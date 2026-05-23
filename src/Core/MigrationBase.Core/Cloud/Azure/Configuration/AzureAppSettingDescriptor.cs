namespace MigrationBase.Core.Cloud.Azure.Configuration;

/// <summary>
/// SDK-free description of an app setting expected by one or more Azure-hosted migration roles.
/// </summary>
public sealed class AzureAppSettingDescriptor
{
    public string Key { get; init; } = string.Empty;

    public string Section { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public AzureAppSettingRequirement Requirement { get; init; } = AzureAppSettingRequirement.Optional;

    public IReadOnlyList<string> AppliesToRoles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> AppliesToEnvironments { get; init; } = Array.Empty<string>();

    public string? DefaultValueHint { get; init; }

    public string? ExampleValue { get; init; }

    public bool IsRequired => Requirement is AzureAppSettingRequirement.Required or AzureAppSettingRequirement.SecretRequired;

    public bool IsSecret => Requirement == AzureAppSettingRequirement.SecretRequired;
}
