namespace MigrationBase.Core.Cloud.Azure.Deployment.HealthChecks;

public sealed record AzureDeploymentHealthCheckResult(
    string Name,
    AzureDeploymentHealthCheckStatus Status,
    string Message,
    DateTimeOffset ObservedAtUtc,
    IReadOnlyDictionary<string, string>? Evidence = null);
