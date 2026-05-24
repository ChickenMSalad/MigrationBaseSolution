namespace Migration.Application.Operational.Health;

public interface IOperationalHealthProbeService
{
    Task<OperationalHealthProbeResponse> GetLivenessAsync(CancellationToken cancellationToken = default);

    Task<OperationalHealthProbeResponse> GetReadinessAsync(CancellationToken cancellationToken = default);
}

public static class OperationalHealthProbeStatuses
{
    public const string Healthy = "Healthy";
    public const string Degraded = "Degraded";
    public const string Unhealthy = "Unhealthy";
}

public sealed class OperationalHealthProbeResponse
{
    public string Status { get; init; } = OperationalHealthProbeStatuses.Unhealthy;

    public DateTimeOffset EvaluatedUtc { get; init; } = DateTimeOffset.UtcNow;

    public string Component { get; init; } = "operational-runtime";

    public string? Message { get; init; }

    public IReadOnlyList<string> BlockingIssues { get; init; } = Array.Empty<string>();

    public IReadOnlyList<OperationalHealthProbeDependencyStatus> Dependencies { get; init; } = Array.Empty<OperationalHealthProbeDependencyStatus>();
}

public sealed class OperationalHealthProbeDependencyStatus
{
    public string Name { get; init; } = string.Empty;

    public string Status { get; init; } = OperationalHealthProbeStatuses.Unhealthy;

    public string? Message { get; init; }
}
