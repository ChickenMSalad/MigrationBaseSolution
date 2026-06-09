namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunControlStateResponse
{
    public Guid RunId { get; init; }
    public string CurrentStatus { get; init; } = string.Empty;
    public bool CancelRequested { get; init; }
    public bool Aborted { get; init; }
    public int ActiveLeaseCount { get; init; }
    public int OutstandingWorkItemCount { get; init; }
    public int CompletedWorkItemCount { get; init; }
    public int FailedWorkItemCount { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string Message { get; init; } = string.Empty;
}


