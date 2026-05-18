namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunControlActionRequest
{
    public string Reason { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = "local-admin";
}
