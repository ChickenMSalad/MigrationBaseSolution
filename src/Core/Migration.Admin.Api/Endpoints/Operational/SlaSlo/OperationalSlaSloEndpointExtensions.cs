using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.SqlMetrics;

namespace Migration.Admin.Api.Endpoints.Operational.SlaSlo;

public static class OperationalSlaSloEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalSlaSloEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/sla-slo")
            .WithTags("Operational SLA/SLO");

        group.MapGet("/summary", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var warningBreaches = snapshot.QueueDepth > 0 ? 1 : 0;
            var criticalBreaches = snapshot.FailureCount > 0 ? 1 : 0;

            var response = new OperationalSlaSloSummaryResponse(
                TotalPolicies: 2,
                ActivePolicies: 2,
                WarningBreaches: warningBreaches,
                CriticalBreaches: criticalBreaches,
                Status: snapshot.Status);

            return Results.Ok(response);
        })
        .WithName("GetOperationalSlaSloSummary");

        group.MapGet("/policies", () =>
        {
            var response = new OperationalSlaSloPolicyCatalogResponse(
                Policies:
                [
                    new OperationalSlaSloPolicyResponse(
                        PolicyId: "queue-depth-warning",
                        Name: "Queue depth warning",
                        Metric: "WorkItems",
                        Threshold: "> 0",
                        Severity: "warning",
                        Enabled: true,
                        Description: "Flags pending operational work items."),
                    new OperationalSlaSloPolicyResponse(
                        PolicyId: "failure-critical",
                        Name: "Failure critical",
                        Metric: "MigrationFailures",
                        Threshold: "> 0",
                        Severity: "critical",
                        Enabled: true,
                        Description: "Flags persisted migration failures.")
                ]);

            return Results.Ok(response);
        })
        .WithName("GetOperationalSlaSloPolicies");

        group.MapGet("/breach-preview", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var breaches = new List<OperationalSlaSloBreachPreviewItemResponse>();

            if (snapshot.QueueDepth > 0)
            {
                breaches.Add(new OperationalSlaSloBreachPreviewItemResponse(
                    BreachId: "queue-depth-warning",
                    DetectedUtc: DateTimeOffset.UtcNow,
                    Severity: "warning",
                    Metric: "WorkItems",
                    Threshold: "> 0",
                    ObservedValue: snapshot.QueueDepth.ToString(),
                    Scope: "OperationalSql",
                    Message: "Operational queue contains pending work items."));
            }

            if (snapshot.FailureCount > 0)
            {
                breaches.Add(new OperationalSlaSloBreachPreviewItemResponse(
                    BreachId: "failure-critical",
                    DetectedUtc: DateTimeOffset.UtcNow,
                    Severity: "critical",
                    Metric: "MigrationFailures",
                    Threshold: "> 0",
                    ObservedValue: snapshot.FailureCount.ToString(),
                    Scope: "OperationalSql",
                    Message: "Operational store contains migration failures."));
            }

            if (snapshot.Status is "not-configured" or "unhealthy")
            {
                breaches.Add(new OperationalSlaSloBreachPreviewItemResponse(
                    BreachId: "operational-sql-health-critical",
                    DetectedUtc: DateTimeOffset.UtcNow,
                    Severity: "critical",
                    Metric: "OperationalSqlHealth",
                    Threshold: "healthy",
                    ObservedValue: snapshot.Status,
                    Scope: "OperationalSql",
                    Message: snapshot.Message ?? "Operational SQL metrics reader is not healthy."));
            }

            var response = new OperationalSlaSloBreachPreviewResponse(Breaches: breaches);
            return Results.Ok(response);
        })
        .WithName("GetOperationalSlaSloBreachPreview");

        return endpoints;
    }
}

public sealed record OperationalSlaSloSummaryResponse(
    int TotalPolicies,
    int ActivePolicies,
    int WarningBreaches,
    int CriticalBreaches,
    string Status);

public sealed record OperationalSlaSloPolicyCatalogResponse(
    IReadOnlyList<OperationalSlaSloPolicyResponse> Policies);

public sealed record OperationalSlaSloPolicyResponse(
    string PolicyId,
    string Name,
    string Metric,
    string Threshold,
    string Severity,
    bool Enabled,
    string? Description);

public sealed record OperationalSlaSloBreachPreviewResponse(
    IReadOnlyList<OperationalSlaSloBreachPreviewItemResponse> Breaches);

public sealed record OperationalSlaSloBreachPreviewItemResponse(
    string BreachId,
    DateTimeOffset DetectedUtc,
    string Severity,
    string Metric,
    string Threshold,
    string ObservedValue,
    string Scope,
    string Message);


