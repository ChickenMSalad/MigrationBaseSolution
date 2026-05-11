using System.Text.RegularExpressions;

using Migration.Connectors.Targets.Bynder.Clients;
using Migration.Connectors.Targets.Bynder.Models;
using Bynder.Sdk.Model;
using Bynder.Sdk.Service;
using Bynder.Sdk.Service.Asset;
using Bynder.Sdk.Settings;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using RestSharp;

namespace Migration.Connectors.Targets.Bynder.Clients;

public class MetapropertyOptionBuilderFactoryApi(IBynderClient bynderClient, IMemoryCache memoryCache)
{
    public async Task<Dictionary<string, BynderMetaProperty>> CreateMetapropertyLookupApi(global::Bynder.Sdk.Settings.Configuration configuration)
    {
        return await memoryCache.GetOrCreateAsync(nameof(CreateMetapropertyLookupApi), async cacheEntry =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            try
            {
                var restClient = new BynderRestClient(configuration);
                var token = await restClient.GetAccessTokenAsync();
                var apiClient = await restClient.GetAuthenticatedClientAsync();

                var request = new RestRequest("api/v4/metaproperties/", Method.Get);
                var response = await apiClient.ExecuteAsync(request);

                if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
                {
                    // Log or throw an exception ?
                    return new Dictionary<string, BynderMetaProperty>();
                }

                var metaPropsToken = JsonConvert.DeserializeObject<JToken>(response.Content);
                if (metaPropsToken == null)
                {
                    return new Dictionary<string, BynderMetaProperty>();
                }

                var metaPropertyDict = metaPropsToken.Children()
                    .Select(child => child.First?.ToObject<BynderMetaProperty>())
                    .Where(meta => meta != null)
                    .OrderBy(meta => meta!.ZIndex)
                    .ToDictionary(meta => meta!.Name, meta => meta!);

                return metaPropertyDict;
            }
            catch (Exception ex)
            {
                // Log error if desired
                return new Dictionary<string, BynderMetaProperty>();
            }

        }).ConfigureAwait(false) ?? new Dictionary<string, BynderMetaProperty>();
    }

    public async Task<BynderMetaProperty?> CreateMetapropertyApi(
        global::Bynder.Sdk.Settings.Configuration configuration,
        BynderMetaProperty metaproperty)
    {
        try
        {
            var restClient = new BynderRestClient(configuration);
            var apiClient = await restClient.GetAuthenticatedClientAsync();

            var payload = new JObject
            {
                ["name"] = metaproperty.Name,
                ["label"] = metaproperty.Label,
                ["type"] = metaproperty.Type,
                ["isSearchable"] = metaproperty.IsSearchable,
                ["zindex"] = metaproperty.ZIndex,
                ["isApiField"] = true,
                ["isEditable"] = true,
                ["isRequired"] = false,
                ["isFilterable"] = false,
                ["isMainfilter"] = false,
                ["isDisplayField"] = false,
                ["isMultifilter"] = false,
                ["showInGridView"] = false,
                ["showInListView"] = false,
                ["showInDuplicateView"] = false,
                ["isDrilldown"] = false,
                ["useDependencies"] = false,
                ["isMultiSelect"] = metaproperty.IsMultiSelect
            };

            var request = new RestRequest("api/v4/metaproperties/", Method.Post);
            request.AddParameter("data", payload.ToString(Formatting.None));

            var response = await apiClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                return new BynderMetaProperty
                {
                    Name = metaproperty.Name,
                    Label = metaproperty.Label,
                    Type = metaproperty.Type,
                    IsSearchable = metaproperty.IsSearchable,
                    IsMultiSelect = metaproperty.IsMultiSelect,
                    ZIndex = metaproperty.ZIndex
                };
            }

            var token = JsonConvert.DeserializeObject<JToken>(response.Content);
            if (token == null)
            {
                return new BynderMetaProperty
                {
                    Name = metaproperty.Name,
                    Label = metaproperty.Label,
                    Type = metaproperty.Type,
                    IsSearchable = metaproperty.IsSearchable,
                    IsMultiSelect = metaproperty.IsMultiSelect,
                    ZIndex = metaproperty.ZIndex
                };
            }

            var parsed = TryParseBynderMetaProperty(token);

            memoryCache.Remove(nameof(CreateMetapropertyLookupApi));

            return new BynderMetaProperty
            {
                Id = parsed?.Id,
                Name = !string.IsNullOrWhiteSpace(parsed?.Name) ? parsed.Name : metaproperty.Name,
                Label = !string.IsNullOrWhiteSpace(parsed?.Label) ? parsed.Label : metaproperty.Label,
                Type = !string.IsNullOrWhiteSpace(parsed?.Type) ? parsed.Type : metaproperty.Type,
                IsSearchable = parsed?.IsSearchable ?? metaproperty.IsSearchable,
                IsMultiSelect = parsed?.IsMultiSelect ?? metaproperty.IsMultiSelect,
                ZIndex = parsed?.ZIndex ?? metaproperty.ZIndex,
                Options = parsed?.Options ?? new List<MetapropertyOption>()
            };
        }
        catch
        {
            return null;
        }
    }
    private static BynderMetaProperty? TryParseBynderMetaProperty(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;

            if (obj["id"] != null || obj["name"] != null || obj["label"] != null || obj["type"] != null)
            {
                return obj.ToObject<BynderMetaProperty>();
            }

            foreach (var propertyName in new[] { "data", "metaproperty", "result" })
            {
                if (obj[propertyName] is JObject nested)
                {
                    var nestedParsed = nested.ToObject<BynderMetaProperty>();
                    if (nestedParsed != null)
                    {
                        return nestedParsed;
                    }
                }
            }
        }

        if (token.Type == JTokenType.Array)
        {
            var first = token.First;
            if (first != null)
            {
                return TryParseBynderMetaProperty(first);
            }
        }

        return null;
    }
    public async Task<MetapropertyOption?> CreateMetapropertyOptionApi(
        global::Bynder.Sdk.Settings.Configuration configuration,
        string metapropertyId,
        string optionValue)
    {
        try
        {
            var restClient = new BynderRestClient(configuration);
            var apiClient = await restClient.GetAuthenticatedClientAsync();

            var payload = new JObject
            {
                ["label"] = optionValue,
                ["name"] = BuildOptionSafeName(optionValue)
            };

            var request = new RestRequest($"api/v4/metaproperties/{metapropertyId}/options/", Method.Post);
            request.AddParameter("data", payload.ToString(Formatting.None));

            var response = await apiClient.ExecuteAsync(request);

            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
            {
                return null;
            }

            var token = JsonConvert.DeserializeObject<JToken>(response.Content);
            if (token == null)
            {
                return null;
            }

            var created = token.Type == JTokenType.Object
                ? token.ToObject<MetapropertyOption>()
                : token.First?.ToObject<MetapropertyOption>();

            memoryCache.Remove(nameof(CreateMetapropertyLookupApi));

            return created;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildOptionSafeName(string value)
    {
        var cleaned = value.Trim().ToLowerInvariant();
        cleaned = Regex.Replace(cleaned, @"[^a-z0-9_]+", "_");
        cleaned = Regex.Replace(cleaned, @"_+", "_").Trim('_');

        return string.IsNullOrWhiteSpace(cleaned) ? "option" : cleaned;
    }
}