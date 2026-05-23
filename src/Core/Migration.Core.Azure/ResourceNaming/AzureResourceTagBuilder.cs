using Microsoft.Extensions.Options;

namespace Migration.Core.Azure.ResourceNaming;

public sealed class AzureResourceTagBuilder : IAzureResourceTagBuilder
{
    private readonly AzureResourceTagOptions _options;

    public AzureResourceTagBuilder(IOptions<AzureResourceTagOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyDictionary<string, string> Build(string environmentName, string workloadName)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["application"] = "MigrationBaseSolution",
            ["environment"] = NormalizeTagValue(environmentName),
            ["workload"] = NormalizeTagValue(workloadName),
            ["owner"] = NormalizeTagValue(_options.Owner),
            ["costCenter"] = NormalizeTagValue(_options.CostCenter),
            ["dataClassification"] = NormalizeTagValue(_options.DataClassification),
            ["operationalStore"] = NormalizeTagValue(_options.OperationalStore)
        };

        foreach (var additionalTag in _options.AdditionalTags)
        {
            if (!string.IsNullOrWhiteSpace(additionalTag.Key) && !string.IsNullOrWhiteSpace(additionalTag.Value))
            {
                tags[additionalTag.Key.Trim()] = NormalizeTagValue(additionalTag.Value);
            }
        }

        return tags;
    }

    private static string NormalizeTagValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unspecified" : value.Trim();
    }
}
