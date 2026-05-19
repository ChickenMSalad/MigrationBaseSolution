namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalWorkItemFailRequest
{
    public string WorkerId { get; init; } = string.Empty;

    public string FailureReason { get; init; } = string.Empty;
}
