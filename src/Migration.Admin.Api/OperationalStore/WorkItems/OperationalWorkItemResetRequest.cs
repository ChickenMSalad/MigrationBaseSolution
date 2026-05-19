namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalWorkItemResetRequest
{
    public string Reason { get; init; } = string.Empty;
}
