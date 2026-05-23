namespace MigrationBase.Core.Cloud.Azure.Deployment;

/// <summary>
/// Defines how a deployment parameter is expected to be supplied for an Azure deployment target.
/// </summary>
public enum AzureDeploymentParameterRequirement
{
    Optional = 0,
    Required = 1,
    RequiredForProduction = 2,
    Generated = 3,
    SecretReference = 4
}
