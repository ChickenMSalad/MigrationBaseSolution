namespace Migration.Application.Models.OperationalStore.Statuses;

public static class MigrationWorkItemStatuses
{
    public const string Pending = "Pending";
    public const string Queued = "Queued";
    public const string Locked = "Locked";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string RetryPending = "RetryPending";
    public const string DeadLettered = "DeadLettered";
}
