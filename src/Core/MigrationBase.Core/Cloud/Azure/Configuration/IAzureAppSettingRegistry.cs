namespace MigrationBase.Core.Cloud.Azure.Configuration;

public interface IAzureAppSettingRegistry
{
    IReadOnlyList<AzureAppSettingDescriptor> Settings { get; }

    IReadOnlyList<AzureAppSettingDescriptor> GetRequiredSettings(string? role = null, string? environment = null);

    IReadOnlyList<AzureAppSettingDescriptor> GetMissingRequiredSettings(
        IReadOnlyDictionary<string, string?> values,
        string? role = null,
        string? environment = null);
}
