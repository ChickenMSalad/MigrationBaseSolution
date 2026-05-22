namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Operational health summary intended for App Service/Container Apps probes and operator diagnostics.
/// This response intentionally contains no secrets.
/// </summary>
public sealed record OperationalHealthDescriptor(
    string Status,
    string EnvironmentName,
    DateTimeOffset CheckedUtc,
    IReadOnlyList<OperationalHealthCheckDescriptor> Checks,
    IReadOnlyList<string> Warnings);

public sealed record OperationalHealthCheckDescriptor(
    string Name,
    string Status,
    string Description,
    IReadOnlyList<string> Warnings);

public static class OperationalHealthStatuses
{
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Unhealthy = "unhealthy";
}
