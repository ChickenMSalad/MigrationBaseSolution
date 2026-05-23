namespace MigrationBase.Core.Cloud.Azure.Deployment.Rollback;

public enum AzureDeploymentRollbackStrategy
{
    Manual = 0,
    RedeployPreviousArtifact = 1,
    RestoreConfigurationSnapshot = 2,
    RestoreDatabasePointInTime = 3,
    FullEnvironmentRecovery = 4
}
