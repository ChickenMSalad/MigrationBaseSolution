namespace MigrationBase.Core.Cloud.Azure.Deployment;

public enum AzureDeploymentApprovalDecision
{
    NotRequired = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4,
    Superseded = 5
}
