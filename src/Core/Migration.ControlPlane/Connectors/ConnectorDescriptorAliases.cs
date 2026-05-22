namespace Migration.ControlPlane.Connectors;

/// <summary>
/// Shared connector alias helpers used by Admin/API/frontend-facing descriptor work.
///
/// This intentionally does not register services yet. It is a small, safe
/// consolidation point for source/target aliases that currently tend to drift
/// between Manifest Builder, credential matching, and backend service names.
/// </summary>
public static class ConnectorDescriptorAliases
{
    private static readonly IReadOnlyDictionary<string, string[]> AliasMap =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["aem"] = ["adobe experience manager", "adobeexperiencemanager"],
            ["aprimo"] = ["aprimo dam"],
            ["azureblob"] = ["azure blob", "blob", "azure storage", "azurestorage"],
            ["bynder"] = ["bynder dam"],
            ["cloudinary"] = ["cloudinary dam"],
            ["contenthub"] = ["content hub", "sitecore", "sitecore content hub", "sitecorecontenthub"],
            ["s3"] = ["aws s3", "amazon s3", "amazons3"],
            ["sharepoint"] = ["share point", "microsoft sharepoint"],
            ["webdam"] = ["web dam", "widen"]
        };

    public static string Normalize(string? connectorType)
    {
        if (string.IsNullOrWhiteSpace(connectorType))
        {
            return string.Empty;
        }

        var value = connectorType.Trim();

        foreach (var pair in AliasMap)
        {
            if (string.Equals(pair.Key, value, StringComparison.OrdinalIgnoreCase) ||
                pair.Value.Any(alias => string.Equals(alias, value, StringComparison.OrdinalIgnoreCase)))
            {
                return pair.Key;
            }
        }

        return value;
    }

    public static bool Matches(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> GetAliases(string connectorType)
    {
        var normalized = Normalize(connectorType);
        return AliasMap.TryGetValue(normalized, out var aliases)
            ? aliases
            : Array.Empty<string>();
    }
}
