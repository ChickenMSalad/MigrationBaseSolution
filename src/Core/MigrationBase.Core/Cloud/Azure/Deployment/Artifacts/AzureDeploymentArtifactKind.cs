namespace MigrationBase.Core.Cloud.Azure.Deployment.Artifacts;

/// <summary>
/// Classifies deployment artifacts without binding the runtime to a particular build or release system.
/// </summary>
public enum AzureDeploymentArtifactKind
{
    Unknown = 0,
    ApplicationPackage = 1,
    WorkerPackage = 2,
    ContainerImage = 3,
    InfrastructureTemplate = 4,
    InfrastructureParameters = 5,
    SqlMigrationBundle = 6,
    ConfigurationBundle = 7,
    ValidationEvidence = 8,
    OperatorUiPackage = 9
}
