namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalWorkItemStateTransitionResponse
{
    public long WorkItemId { get; init; }

    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? Status { get; init; }

    public string? LockedBy { get; init; }

    public DateTimeOffset? LockedAt { get; init; }
}


