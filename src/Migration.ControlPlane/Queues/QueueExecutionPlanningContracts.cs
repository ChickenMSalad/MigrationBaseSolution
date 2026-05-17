namespace Migration.ControlPlane.Queues;

public sealed record QueueExecutionPlan(
    string MessageType,
    string WorkspaceId,
    string? TenantId,
    string? ProjectId,
    string? RunId,
    string IdempotencyKey,
    string Action,
    bool CanExecute,
    bool RequiresRunId,
    bool RequiresProjectId,
    IReadOnlyList<string> Warnings);

public static class QueueMessageTypes
{
    public const string MigrationRunExecute = "migration.run.execute";
    public const string MigrationRunCancel = "migration.run.cancel";
    public const string MigrationRunResume = "migration.run.resume";
}
