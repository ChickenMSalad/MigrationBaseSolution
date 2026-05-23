namespace MigrationBase.Core.Cloud.Azure.Workers.CircuitBreakers;

/// <summary>
/// Defines thresholds and timing rules for protecting worker execution loops.
/// </summary>
public sealed record AzureWorkerCircuitBreakerPolicy
{
    public required string Name { get; init; }

    public bool Enabled { get; init; } = true;

    public int FailureThreshold { get; init; } = 10;

    public int ConsecutiveFailureThreshold { get; init; } = 5;

    public int MinimumSampleSize { get; init; } = 20;

    public double FailureRatioThreshold { get; init; } = 0.5;

    public TimeSpan SamplingWindow { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan OpenDuration { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan HalfOpenProbeInterval { get; init; } = TimeSpan.FromSeconds(30);

    public int HalfOpenProbeLimit { get; init; } = 1;

    public string? Description { get; init; }
}
