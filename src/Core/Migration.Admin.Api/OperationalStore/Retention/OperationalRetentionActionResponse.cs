namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRetentionActionResponse
{
    public bool Enabled { get; init; }
    public bool Executed { get; init; }
    public string Action { get; init; } = string.Empty;
    public int AffectedRunCount { get; init; }
    public DateTimeOffset Threshold { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyCollection<Guid> RunIds { get; init; } = Array.Empty<Guid>();
}


