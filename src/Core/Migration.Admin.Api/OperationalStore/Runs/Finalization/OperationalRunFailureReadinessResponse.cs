namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunFailureReadinessResponse
{
    public Guid RunId { get; init; }
    public string CurrentStatus { get; init; } = string.Empty;
    public bool CanFinalizeFailure { get; init; }
    public bool Finalized { get; init; }
    public int TotalWorkItemCount { get; init; }
    public int OutstandingWorkItemCount { get; init; }
    public int CompletedWorkItemCount { get; init; }
    public int FailedWorkItemCount { get; init; }
    public string? FailureReason { get; init; }
    public string Message { get; init; } = string.Empty;
}


