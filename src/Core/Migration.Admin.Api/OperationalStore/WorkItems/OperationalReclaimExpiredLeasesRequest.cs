namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalReclaimExpiredLeasesRequest
{
    public int? MaxCount { get; init; }

    public string Reason { get; init; } = string.Empty;
}


