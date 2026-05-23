namespace MigrationBase.Core.Cloud.Azure.Configuration;

/// <summary>
/// Immutable SDK-free registry of Azure app settings used by deployment and host composition validation.
/// </summary>
public sealed class AzureAppSettingRegistry : IAzureAppSettingRegistry
{
    private readonly IReadOnlyList<AzureAppSettingDescriptor> _settings;

    public AzureAppSettingRegistry(IEnumerable<AzureAppSettingDescriptor> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings
            .Where(setting => !string.IsNullOrWhiteSpace(setting.Key))
            .GroupBy(setting => setting.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(setting => setting.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(setting => setting.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<AzureAppSettingDescriptor> Settings => _settings;

    public IReadOnlyList<AzureAppSettingDescriptor> GetRequiredSettings(string? role = null, string? environment = null)
    {
        return _settings
            .Where(setting => setting.IsRequired)
            .Where(setting => AppliesTo(setting.AppliesToRoles, role))
            .Where(setting => AppliesTo(setting.AppliesToEnvironments, environment))
            .ToArray();
    }

    public IReadOnlyList<AzureAppSettingDescriptor> GetMissingRequiredSettings(
        IReadOnlyDictionary<string, string?> values,
        string? role = null,
        string? environment = null)
    {
        ArgumentNullException.ThrowIfNull(values);

        return GetRequiredSettings(role, environment)
            .Where(setting => !values.TryGetValue(setting.Key, out var value) || string.IsNullOrWhiteSpace(value))
            .ToArray();
    }

    private static bool AppliesTo(IReadOnlyList<string> candidates, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || candidates.Count == 0)
        {
            return true;
        }

        return candidates.Any(candidate => string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase));
    }
}
