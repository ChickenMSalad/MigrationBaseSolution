using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Migration.Application.Taxonomy;
using Microsoft.Extensions.Options;

namespace Migration.Infrastructure.Taxonomy;

public sealed class CloudinaryTaxonomyProvider : ITaxonomyProvider
{
    private readonly HttpClient _httpClient;
    private readonly CloudinaryTaxonomyOptions _options;

    public CloudinaryTaxonomyProvider(HttpClient httpClient, IOptions<TaxonomyBuilderOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.Cloudinary;
    }

    public string TargetType => "Cloudinary";

    public async Task<TaxonomyWorkbook> GetTaxonomyAsync(TaxonomyExportRequest request, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var url = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/metadata_fields";
        using var http = new HttpRequestMessage(HttpMethod.Get, url);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ApiKey}:{_options.ApiSecret}"));
        http.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var response = await _httpClient.SendAsync(http, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var fields = new List<TaxonomyField>();
        var options = new List<TaxonomyOption>();

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var array = JsonTaxonomyHelpers.TryGetArray(root, out var fieldsArray, "metadata_fields", "fields")
            ? fieldsArray
            : root;

        if (array.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in array.EnumerateArray())
            {
                var id = JsonTaxonomyHelpers.StringProp(item, "external_id", "id");
                var name = JsonTaxonomyHelpers.StringProp(item, "external_id", "id");
                var label = JsonTaxonomyHelpers.StringProp(item, "label", "display_name");
                var type = JsonTaxonomyHelpers.StringProp(item, "type", "datasource_type");

                fields.Add(new TaxonomyField
                {
                    TargetType = TargetType,
                    Id = id,
                    Name = name,
                    Label = string.IsNullOrWhiteSpace(label) ? name : label,
                    Type = type,
                    Required = JsonTaxonomyHelpers.BoolProp(item, "mandatory", "required"),
                    Searchable = true,
                    MultiValue = string.Equals(type, "set", StringComparison.OrdinalIgnoreCase),
                    Status = JsonTaxonomyHelpers.StringProp(item, "state", "status"),
                    SortOrder = JsonTaxonomyHelpers.IntProp(item, "position", "sortOrder")
                });

                if (request.IncludeOptions && JsonTaxonomyHelpers.TryGetArray(item, out var datasource, "datasource", "values", "options"))
                {
                    foreach (var option in datasource.EnumerateArray())
                    {
                        options.Add(new TaxonomyOption
                        {
                            TargetType = TargetType,
                            FieldId = id,
                            FieldName = name,
                            Id = JsonTaxonomyHelpers.StringProp(option, "external_id", "id", "value"),
                            Name = JsonTaxonomyHelpers.StringProp(option, "external_id", "id", "value"),
                            Label = JsonTaxonomyHelpers.StringProp(option, "value", "label", "display_name"),
                            Selectable = !JsonTaxonomyHelpers.BoolProp(option, "disabled"),
                            SortOrder = JsonTaxonomyHelpers.IntProp(option, "position", "sortOrder")
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
        if (string.IsNullOrWhiteSpace(_options.CloudName) || string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            throw new InvalidOperationException("TaxonomyBuilder:Cloudinary:CloudName, ApiKey, and ApiSecret are required.");
        }
    }
}
