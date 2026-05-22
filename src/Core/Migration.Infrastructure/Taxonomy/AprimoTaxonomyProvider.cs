using System.Net.Http.Headers;
using System.Text.Json;
using Migration.Application.Taxonomy;
using Microsoft.Extensions.Options;

namespace Migration.Infrastructure.Taxonomy;

public sealed class AprimoTaxonomyProvider : ITaxonomyProvider
{
    private readonly HttpClient _httpClient;
    private readonly AprimoTaxonomyOptions _options;

    public AprimoTaxonomyProvider(HttpClient httpClient, IOptions<TaxonomyBuilderOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.Aprimo;
    }

    public string TargetType => "Aprimo";

    public async Task<TaxonomyWorkbook> GetTaxonomyAsync(TaxonomyExportRequest request, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var url = new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), "api/core/fielddefinitions");
        using var http = new HttpRequestMessage(HttpMethod.Get, url);
        http.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BearerToken);

        using var response = await _httpClient.SendAsync(http, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var fields = new List<TaxonomyField>();
        var options = new List<TaxonomyOption>();

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var array = JsonTaxonomyHelpers.TryGetArray(root, out var items, "items", "fieldDefinitions", "fields")
            ? items
            : root;

        if (array.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in array.EnumerateArray())
            {
                var id = JsonTaxonomyHelpers.StringProp(item, "id", "Id");
                var name = JsonTaxonomyHelpers.StringProp(item, "fieldName", "name", "Name");
                var label = JsonTaxonomyHelpers.StringProp(item, "label", "Label", "displayName");
                var type = JsonTaxonomyHelpers.StringProp(item, "dataType", "type", "Type");

                fields.Add(new TaxonomyField
                {
                    TargetType = TargetType,
                    Id = id,
                    Name = name,
                    Label = string.IsNullOrWhiteSpace(label) ? name : label,
                    Type = type,
                    Required = JsonTaxonomyHelpers.BoolProp(item, "required", "isRequired"),
                    Searchable = JsonTaxonomyHelpers.BoolProp(item, "searchable", "isSearchable"),
                    MultiValue = type.Contains("List", StringComparison.OrdinalIgnoreCase),
                    GroupName = JsonTaxonomyHelpers.StringProp(item, "fieldGroup", "groupName"),
                    Status = JsonTaxonomyHelpers.StringProp(item, "status"),
                    SortOrder = JsonTaxonomyHelpers.IntProp(item, "sortOrder", "position")
                });

                if (request.IncludeOptions && JsonTaxonomyHelpers.TryGetArray(item, out var values, "values", "options", "classifications"))
                {
                    foreach (var option in values.EnumerateArray())
                    {
                        options.Add(new TaxonomyOption
                        {
                            TargetType = TargetType,
                            FieldId = id,
                            FieldName = name,
                            Id = JsonTaxonomyHelpers.StringProp(option, "id", "Id"),
                            Name = JsonTaxonomyHelpers.StringProp(option, "name", "Name", "value"),
                            Label = JsonTaxonomyHelpers.StringProp(option, "label", "Label", "value", "name"),
                            Selectable = !JsonTaxonomyHelpers.BoolProp(option, "disabled", "inactive"),
                            SortOrder = JsonTaxonomyHelpers.IntProp(option, "sortOrder", "position"),
                            ParentOptionId = JsonTaxonomyHelpers.StringProp(option, "parentId", "parentOptionId")
                        });
                    }
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
            throw new InvalidOperationException("TaxonomyBuilder:Aprimo:BaseUrl and TaxonomyBuilder:Aprimo:BearerToken are required.");
        }
    }
}
