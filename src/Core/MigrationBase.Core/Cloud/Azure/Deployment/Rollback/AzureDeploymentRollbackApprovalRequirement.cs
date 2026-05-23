namespace MigrationBase.Core.Cloud.Azure.Deployment.Rollback;

public enum AzureDeploymentRollbackApprovalRequirement
{
    NotRequired = 0,
    Required = 1,
    RequiredForProduction = 2,
    RequiredForDestructiveSteps = 3
}
