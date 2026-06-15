using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.SqlMetrics;

namespace Migration.Admin.Api.Endpoints.Operational.SlaSlo;

public static class OperationalSlaSloEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalSlaSloEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/operational/sla-slo")
            .WithTags("Operational SLA/SLO");

        group.MapGet("/summary", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var evaluation = OperationalSlaSloEvaluation.From(snapshot);

            var response = new OperationalSlaSloSummaryResponse(
                TotalPolicies: evaluation.Policies.Count,
                ActivePolicies: evaluation.Policies.Count(policy => policy.Enabled),
                HealthyPolicies: evaluation.HealthyCount,
                WarningBreaches: evaluation.WarningCount,
                CriticalBreaches: evaluation.CriticalCount,
                Status: evaluation.Status,
                OverallObjective: evaluation.Objective,
                Message: evaluation.Message,
                GeneratedUtc: evaluation.GeneratedUtc);

            return Results.Ok(response);
        })
        .WithName("GetOperationalSlaSloSummary");

        group.MapGet("/policies", () =>
        {
            var response = new OperationalSlaSloPolicyCatalogResponse(
                Policies: OperationalSlaSloEvaluation.CreatePolicyCatalog());

            return Results.Ok(response);
        })
        .WithName("GetOperationalSlaSloPolicies");

        group.MapGet("/breach-preview", async (
            ISqlOperationalMetricsReader metricsReader,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await metricsReader.ReadSnapshotAsync(cancellationToken);
            var evaluation = OperationalSlaSloEvaluation.From(snapshot);

            var response = new OperationalSlaSloBreachPreviewResponse(
                GeneratedUtc: evaluation.GeneratedUtc,
                Status: evaluation.Status,
                Breaches: evaluation.Breaches);

            return Results.Ok(response);
        })
        .WithName("GetOperationalSlaSloBreachPreview");

        return endpoints;
    }
}

internal sealed class OperationalSlaSloEvaluation
{
    private OperationalSlaSloEvaluation(
        IReadOnlyList<OperationalSlaSloPolicyResponse> policies,
        IReadOnlyList<OperationalSlaSloBreachPreviewItemResponse> breaches,
        string status,
        string objective,
        string? message,
        DateTimeOffset generatedUtc)
    {
        Policies = policies;
        Breaches = breaches;
        Status = status;
        Objective = objective;
        Message = message;
        GeneratedUtc = generatedUtc;
    }

    public IReadOnlyList<OperationalSlaSloPolicyResponse> Policies { get; }

    public IReadOnlyList<OperationalSlaSloBreachPreviewItemResponse> Breaches { get; }

    public string Status { get; }

    public string Objective { get; }

    public string? Message { get; }

    public DateTimeOffset GeneratedUtc { get; }

    public int WarningCount => Breaches.Count(item => string.Equals(item.Severity, "warning", StringComparison.OrdinalIgnoreCase));

    public int CriticalCount => Breaches.Count(item => string.Equals(item.Severity, "critical", StringComparison.OrdinalIgnoreCase));

    public int HealthyCount => Math.Max(0, Policies.Count - WarningCount - CriticalCount);

    public static OperationalSlaSloEvaluation From(SqlOperationalMetricsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var policies = CreatePolicyCatalog();
        var breaches = new List<OperationalSlaSloBreachPreviewItemResponse>();
        var generatedUtc = DateTimeOffset.UtcNow;

        if (snapshot.Status is "not-configured" or "unhealthy")
        {
            breaches.Add(new OperationalSlaSloBreachPreviewItemResponse(
                BreachId: "operational-sql-health-critical",
                DetectedUtc: generatedUtc,
                Severity: "critical",
                Metric: "OperationalSqlHealth",
                Threshold: "healthy",
                ObservedValue: snapshot.Status,
                Scope: "OperationalSql",
                Message: snapshot.Message ?? "Operational SQL runtime is not healthy."));
        }

        if (snapshot.FailureCount > 0)
        {
            breaches.Add(new OperationalSlaSloBreachPreviewItemResponse(
                BreachId: "failure-count-critical",
                DetectedUtc: generatedUtc,
                Severity: "critical",
                Metric: "FailedWorkItems",
                Threshold: "0",
                ObservedValue: snapshot.FailureCount.ToString(),
                Scope: "migration.WorkItems",
                Message: "Failed operational work items require review or retry."));
        }

        if (snapshot.QueueDepth >= 500)
        {
            breaches.Add(new OperationalSlaSloBreachPreviewItemResponse(
                BreachId: "queue-depth-critical",
                DetectedUtc: generatedUtc,
                Severity: "critical",
                Metric: "QueuedWorkItems",
                Threshold: "< 500",
                ObservedValue: snapshot.QueueDepth.ToString(),
                Scope: "migration.WorkItems",
                Message: "Operational queue depth is high enough to threaten migration throughput."));
        }
        else if (snapshot.QueueDepth >= 100)
        {
            breaches.Add(new OperationalSlaSloBreachPreviewItemResponse(
                BreachId: "queue-depth-warning",
                DetectedUtc: generatedUtc,
                Severity: "warning",
                Metric: "QueuedWorkItems",
                Threshold: "< 100",
                ObservedValue: snapshot.QueueDepth.ToString(),
                Scope: "migration.WorkItems",
                Message: "Operational queue has elevated backlog."));
        }

        if (snapshot.ActiveRuns > 0 && snapshot.ActiveWorkers == 0)
        {
            breaches.Add(new OperationalSlaSloBreachPreviewItemResponse(
                BreachId: "active-run-no-worker-critical",
                DetectedUtc: generatedUtc,
                Severity: "critical",
                Metric: "ActiveWorkers",
                Threshold: "> 0 when active runs exist",
                ObservedValue: snapshot.ActiveWorkers.ToString(),
                Scope: "migration.Runs",
                Message: "Active operational runs exist but no active workers were observed."));
        }

        var status = breaches.Any(item => string.Equals(item.Severity, "critical", StringComparison.OrdinalIgnoreCase))
            ? "critical"
            : breaches.Count > 0
                ? "warning"
                : snapshot.Status;

        var objective = status switch
        {
            "healthy" => "Operational runtime is inside current SLO guardrails.",
            "warning" => "Operational runtime has warning-level SLO pressure.",
            "critical" => "Operational runtime has critical SLO breaches requiring attention.",
            _ => "Operational runtime SLO status follows SQL readiness state."
        };

        return new OperationalSlaSloEvaluation(
            policies,
            breaches,
            status,
            objective,
            snapshot.Message,
            generatedUtc);
    }

    public static IReadOnlyList<OperationalSlaSloPolicyResponse> CreatePolicyCatalog()
    {
        return new[]
        {
            new OperationalSlaSloPolicyResponse(
                PolicyId: "operational-sql-health-critical",
                Name: "Operational SQL health",
                Metric: "OperationalSqlHealth",
                Threshold: "healthy",
                Severity: "critical",
                Enabled: true,
                Description: "Operational SQL must be reachable and able to read runtime tables."),
            new OperationalSlaSloPolicyResponse(
                PolicyId: "failure-count-critical",
                Name: "Failed work items",
                Metric: "FailedWorkItems",
                Threshold: "0",
                Severity: "critical",
                Enabled: true,
                Description: "Any failed operational work item is a retry or investigation candidate."),
            new OperationalSlaSloPolicyResponse(
                PolicyId: "queue-depth-warning",
                Name: "Queue backlog warning",
                Metric: "QueuedWorkItems",
                Threshold: "< 100",
                Severity: "warning",
                Enabled: true,
                Description: "Queue backlog above this level indicates elevated runtime pressure."),
            new OperationalSlaSloPolicyResponse(
                PolicyId: "queue-depth-critical",
                Name: "Queue backlog critical",
                Metric: "QueuedWorkItems",
                Threshold: "< 500",
                Severity: "critical",
                Enabled: true,
                Description: "Queue backlog above this level threatens migration throughput."),
            new OperationalSlaSloPolicyResponse(
                PolicyId: "active-run-no-worker-critical",
                Name: "Active run worker coverage",
                Metric: "ActiveWorkers",
                Threshold: "> 0 when active runs exist",
                Severity: "critical",
                Enabled: true,
                Description: "Active runs require observable worker activity.")
        };
    }
}

public sealed record OperationalSlaSloSummaryResponse(
    int TotalPolicies,
    int ActivePolicies,
    int HealthyPolicies,
    int WarningBreaches,
    int CriticalBreaches,
    string Status,
    string OverallObjective,
    string? Message,
    DateTimeOffset GeneratedUtc);

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
    DateTimeOffset GeneratedUtc,
    string Status,
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
