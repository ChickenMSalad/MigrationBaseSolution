namespace MigrationBase.Core.Cloud.Azure.Deployment.Validation;

/// <summary>
/// Defines how strongly a deployment validation script is required for a target environment.
/// </summary>
public enum AzureDeploymentValidationScriptRequirement
{
    Optional = 0,
    Recommended = 1,
    Required = 2,
    Blocking = 3
}
