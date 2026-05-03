using System.Net.Http.Headers;
using System.Text.Json;
using Migration.Application.Taxonomy;
using Microsoft.Extensions.Options;

namespace Migration.Infrastructure.Taxonomy;

public sealed class BynderTaxonomyProvider : ITaxonomyProvider
{
    private readonly HttpClient _httpClient;
    private readonly BynderTaxonomyOptions _options;

    public BynderTaxonomyProvider(HttpClient httpClient, IOptions<TaxonomyBuilderOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.Bynder;
    }

    public string TargetType => "Bynder";

    public async Task<TaxonomyWorkbook> GetTaxonomyAsync(TaxonomyExportRequest request, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var url = new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), "api/v4/metaproperties/");
        using var http = new HttpRequestMessage(HttpMethod.Get, url);
        http.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BearerToken);

        using var response = await _httpClient.SendAsync(http, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var fields = new List<TaxonomyField>();
        var options = new List<TaxonomyOption>();

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var items = root.ValueKind == JsonValueKind.Array
            ? root.EnumerateArray()
            : root.ValueKind == JsonValueKind.Object
                ? root.EnumerateObject().Select(x => x.Value)
                : Enumerable.Empty<JsonElement>();

        foreach (var item in items)
        {
            var id = JsonTaxonomyHelpers.StringProp(item, "id", "Id");
            var name = JsonTaxonomyHelpers.StringProp(item, "name", "Name");
            var label = JsonTaxonomyHelpers.StringProp(item, "label", "Label");
            var type = JsonTaxonomyHelpers.StringProp(item, "type", "Type");

            fields.Add(new TaxonomyField
            {
                TargetType = TargetType,
                Id = id,
                Name = name,
                Label = string.IsNullOrWhiteSpace(label) ? name : label,
                Type = type,
                Required = JsonTaxonomyHelpers.BoolProp(item, "isRequired", "required"),
                Searchable = JsonTaxonomyHelpers.BoolProp(item, "isSearchable", "searchable"),
                MultiValue = JsonTaxonomyHelpers.BoolProp(item, "isMultiSelect", "multiSelect", "multi_value"),
                Status = JsonTaxonomyHelpers.StringProp(item, "status"),
                SortOrder = JsonTaxonomyHelpers.IntProp(item, "zIndex", "position", "sortOrder")
            });

            if (request.IncludeOptions && JsonTaxonomyHelpers.TryGetArray(item, out var optionArray, "options", "Options"))
            {
                foreach (var option in optionArray.EnumerateArray())
                {
                    options.Add(new TaxonomyOption
                    {
                        TargetType = TargetType,
                        FieldId = id,
                        FieldName = name,
                        Id = JsonTaxonomyHelpers.StringProp(option, "id", "Id"),
                        Name = JsonTaxonomyHelpers.StringProp(option, "name", "Name"),
                        Label = JsonTaxonomyHelpers.StringProp(option, "label", "Label", "name"),
                        Selectable = !JsonTaxonomyHelpers.BoolProp(option, "disabled") && JsonTaxonomyHelpers.BoolProp(option, "isSelectable", "selectable"),
                        SortOrder = JsonTaxonomyHelpers.IntProp(option, "zIndex", "position", "sortOrder"),
                        LinkedOptionIds = ReadLinkedOptionIds(option)
                    });
                }
            }
        }

        return new TaxonomyWorkbook
        {
            TargetType = TargetType,
            Fields = fields.OrderBy(x => x.SortOrder ?? int.MaxValue).ThenBy(x => x.Name).ToList(),
            Options = options.OrderBy(x => x.FieldName).ThenBy(x => x.SortOrder ?? int.MaxValue).ThenBy(x => x.Name).ToList(),
            RawJson = request.IncludeRaw ? raw : null
        };
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.BearerToken))
        {
            throw new InvalidOperationException("TaxonomyBuilder:Bynder:BaseUrl and TaxonomyBuilder:Bynder:BearerToken are required.");
        }
    }

    private static string? ReadLinkedOptionIds(JsonElement option)
    {
        if (!JsonTaxonomyHelpers.TryGetArray(option, out var linked, "linkedOptionIds", "LinkedOptionIds")) return null;
        return string.Join("|", linked.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.GetRawText()));
    }
}
