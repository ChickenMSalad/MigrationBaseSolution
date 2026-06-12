namespace Migration.Admin.Api.Operational.Execution;

public static class ExecutionPhaseNames
{
    public const string Created = "created";
    public const string Validating = "validating";
    public const string ManifestLoading = "manifest-loading";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Paused = "paused";
    public const string Failed = "failed";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly string[] All =
    [
        Created,
        Validating,
        ManifestLoading,
        Queued,
        Running,
        Paused,
        Failed,
        Completed,
        Cancelled
    ];

    public static bool IsKnown(string? phase)
    {
        return !string.IsNullOrWhiteSpace(phase) &&
            All.Contains(phase.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static string Normalize(string phase)
    {
        return phase.Trim().ToLowerInvariant();
    }
}


