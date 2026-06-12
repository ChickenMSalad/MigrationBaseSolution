namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorInvocationSnapshot
{
    public static OperationalMirrorInvocationSnapshot Empty { get; } = new()
    {
        Invoked = false,
        Mirrored = false,
        Failed = false,
        LegacyRunId = null,
        Message = "Operational mirror has not been invoked since Admin API startup.",
        RecordedAt = null
    };

    public bool Invoked { get; init; }

    public bool Mirrored { get; init; }

    public bool Failed { get; init; }

    public string? LegacyRunId { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTimeOffset? RecordedAt { get; init; }
}


