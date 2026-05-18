namespace Migration.Application.Models.OperationalStore.Statuses;

public static class MigrationCheckpointNames
{
    public const string ManifestIngestion = "ManifestIngestion";
    public const string WorkItemDispatch = "WorkItemDispatch";
    public const string RunExecution = "RunExecution";
    public const string IdentifierMapping = "IdentifierMapping";
    public const string FailureRecovery = "FailureRecovery";
}
