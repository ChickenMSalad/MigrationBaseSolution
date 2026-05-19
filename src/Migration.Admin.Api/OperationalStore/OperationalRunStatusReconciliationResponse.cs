namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunStatusReconciliationResponse
{
    public Guid RunId { get; init; }
    public string CurrentStatus { get; init; } = string.Empty;
    public string RecommendedStatus { get; init; } = string.Empty;
    public bool WouldChange { get; init; }
    public bool Applied { get; init; }
    public int TotalWorkItemCount { get; init; }
    public int CreatedWorkItemCount { get; init; }
    public int LockedWorkItemCount { get; init; }
    public int ProcessingWorkItemCount { get; init; }
    public int CompletedWorkItemCount { get; init; }
    public int FailedWorkItemCount { get; init; }
    public int OutstandingWorkItemCount { get; init; }
    public string Reason { get; init; } = string.Empty;
}
