using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Connectors.Targets.Cloudinary.Configuration;
using Migration.Connectors.Targets.Cloudinary.Models;

namespace Migration.Connectors.Targets.Cloudinary.Clients;

public sealed class CloudinaryAdminClient(
    HttpClient httpClient,
    IOptions<CloudinaryOptions> options,
    ILogger<CloudinaryAdminClient> logger) : ICloudinaryAdminClient
{
    private readonly CloudinaryOptions _options = options.Value;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<CloudinaryMetadataFieldSchema>> GetMetadataFieldsAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "metadata_fields");
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var results = new List<CloudinaryMetadataFieldSchema>();
        if (!document.RootElement.TryGetProperty("metadata_fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var field in fields.EnumerateArray())
        {
            var schema = new CloudinaryMetadataFieldSchema
            {
                ExternalId = field.TryGetProperty("external_id", out var externalId) ? externalId.GetString() ?? string.Empty : string.Empty,
                Type = field.TryGetProperty("type", out var type) ? type.GetString() ?? string.Empty : string.Empty
            };

            if (field.TryGetProperty("datasource", out var datasource) && datasource.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in datasource.EnumerateArray())
                {
                    schema.DatasourceValues.Add(new CloudinaryMetadataDatasourceValue
                    {
                        ExternalId = item.TryGetProperty("external_id", out var dsExternalId) ? dsExternalId.GetString() ?? string.Empty : string.Empty,
                        Value = item.TryGetProperty("value", out var value) ? value.GetString() ?? string.Empty : string.Empty
                    });
                }
            }

            results.Add(schema);
        }

        return results;
    }

    public async Task<bool> AssetExistsAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"resources/image/upload/{Uri.EscapeDataString(publicId)}";
        using var request = CreateRequest(HttpMethod.Get, endpoint);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        if ((int)response.StatusCode == 400)
        {
            // some Cloudinary accounts return 400 for mismatched resource type. Fallback to auto via search.
            return await SearchAssetExistsAsync(publicId, cancellationToken).ConfigureAwait(false);
        }

        response.EnsureSuccessStatusCode();
        return false;
    }

    public async Task<IReadOnlyList<string>> FindDuplicatePublicIdsAsync(IEnumerable<string> publicIds, CancellationToken cancellationToken = default)
    {
        var normalized = publicIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var duplicates = normalized
            .GroupBy(x => x, StringComparer.Ordinal)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var publicId in normalized)
        {
            if (await SearchAssetExistsAsync(publicId, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }
        }

        return duplicates.OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    public async Task DeleteAsync(string publicId, string resourceType = "image", string type = "upload", CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, string?>
        {
            ["public_id"] = publicId,
            ["resource_type"] = resourceType,
            ["type"] = type
        };

        using var request = CreateSignedFormRequest("resources/destroy", payload);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        logger.LogInformation("Deleted Cloudinary asset '{PublicId}' ({ResourceType}/{Type}).", publicId, resourceType, type);
    }

    private async Task<bool> SearchAssetExistsAsync(string publicId, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, string?>
        {
            ["expression"] = $"public_id=\"{publicId.Replace("\"", "\\\"")}\"",
            ["max_results"] = "1"
        };

        using var request = CreateSignedFormRequest("resources/search", payload);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return document.RootElement.TryGetProperty("resources", out var resources)
               && resources.ValueKind == JsonValueKind.Array
               && resources.GetArrayLength() > 0;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        _options.Validate();
        var request = new HttpRequestMessage(method, relativePath);
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ApiKey}:{_options.ApiSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        return request;
    }

    private HttpRequestMessage CreateSignedFormRequest(string relativePath, IDictionary<string, string?> values)
    {
        _options.Validate();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var payload = values
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(x => x.Key, x => x.Value!, StringComparer.Ordinal);

        payload["timestamp"] = timestamp;
        payload["api_key"] = _options.ApiKey;
        payload["signature"] = ComputeSignature(payload);

        return new HttpRequestMessage(HttpMethod.Post, relativePath)
        {
            Content = new FormUrlEncodedContent(payload)
        };
    }

    private string ComputeSignature(IDictionary<string, string> payload)
    {
        var source = string.Join("&",
            payload
                .Where(x => !string.Equals(x.Key, "file", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(x.Key, "api_key", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(x.Key, "resource_type", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(x.Key, "signature", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}={x.Value}")) + _options.ApiSecret;

        using var sha = System.Security.Cryptography.SHA1.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
