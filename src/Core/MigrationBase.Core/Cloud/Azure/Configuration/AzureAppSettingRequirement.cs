namespace MigrationBase.Core.Cloud.Azure.Configuration;

/// <summary>
/// Describes how strongly an Azure application setting is required for a hosted migration role.
/// </summary>
public enum AzureAppSettingRequirement
{
    Optional = 0,
    Required = 1,
    SecretRequired = 2
}
