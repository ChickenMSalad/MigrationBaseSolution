using Bynder.Sdk.Model;

namespace Migration.Connectors.Targets.Bynder.Extensions;

public static class MediaExtensions
{
    public static string? GetPropertyValue(this Media media, string propertyName)
    {
        if (!media.PropertyOptionsDictionary.TryGetValue(propertyName, out var value) &&
            !propertyName.StartsWith("property_", StringComparison.OrdinalIgnoreCase))
        {
            // Add fallback check when property prefix is not specified
            media.PropertyOptionsDictionary.TryGetValue($"property_{propertyName}", out value);
        }

        return value?.FirstOrDefault()?.ToString();
    }
}
