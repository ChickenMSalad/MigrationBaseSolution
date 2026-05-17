namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Safe configuration audit summary. Values are intentionally not included.
/// </summary>
public sealed record CloudConfigurationAuditDescriptor(
    string EnvironmentName,
    string MaturityLevel,
    int ConfiguredCount,
    int MissingCount,
    int WarningCount,
    IReadOnlyList<CloudConfigurationKeyAuditDescriptor> Keys,
    IReadOnlyList<string> Warnings);

public sealed record CloudConfigurationKeyAuditDescriptor(
    string Key,
    string Category,
    bool IsConfigured,
    bool IsRequiredForCloud,
    string? Recommendation);

public static class CloudConfigurationMaturityLevels
{
    public const string LocalDevelopment = "localDevelopment";
    public const string CloudPlanned = "cloudPlanned";
    public const string CloudReady = "cloudReady";
    public const string Unknown = "unknown";
}
