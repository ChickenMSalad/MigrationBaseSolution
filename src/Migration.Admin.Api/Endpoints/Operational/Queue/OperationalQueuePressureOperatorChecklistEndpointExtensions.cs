namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure operator checklist endpoints.
/// This is intentionally metadata-only and compile-safe: it does not introduce new services, repositories, or DTO dependencies.
/// </summary>
public static class OperationalQueuePressureOperatorChecklistEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureOperatorChecklistApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/operator-checklist",
                (string? phase, string? severity) =>
                {
                    var checklist = BuildChecklist();

                    if (!string.IsNullOrWhiteSpace(phase))
                    {
                        checklist = checklist
                            .Where(x => string.Equals(x.Phase, phase, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                    }

                    if (!string.IsNullOrWhiteSpace(severity))
                    {
                        checklist = checklist
                            .Where(x => string.Equals(x.Severity, severity, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                    }

                    return Results.Ok(new
                    {
                        operatorChecklist = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            filters = new
                            {
                                phase,
                                severity
                            },
                            totalChecklistItemCount = checklist.Length,
                            phases = checklist
                                .Select(x => x.Phase)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(x => x)
                                .ToArray(),
                            severities = checklist
                                .Select(x => x.Severity)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(x => x)
                                .ToArray(),
                            items = checklist
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureOperatorChecklist")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure operator checklist.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/operator-checklist/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        checklistItemCount = BuildChecklist().Length,
                        endpoint = "/api/operational/queue-pressure/operator-checklist"
                    }
                }))
            .WithName("GetOperationalQueuePressureOperatorChecklistReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure operator checklist readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static QueuePressureChecklistItem[] BuildChecklist()
    {
        return new[]
        {
            new QueuePressureChecklistItem(
                Id: "endpoint-discovery",
                Phase: "Validation",
                Severity: "Critical",
                Check: "Confirm endpoint discovery contains the queue pressure dashboard, trend, action-plan, recommendation-catalog, and operator-checklist routes.",
                ExpectedResult: "All expected /api/operational/queue-pressure routes are discoverable and no /api/api/ duplicates are present."),
            new QueuePressureChecklistItem(
                Id: "dashboard-health",
                Phase: "Triage",
                Severity: "Critical",
                Check: "Open the queue pressure dashboard first to establish the current aggregate pressure state.",
                ExpectedResult: "Dashboard returns a generated timestamp and queue/dispatcher pressure sections."),
            new QueuePressureChecklistItem(
                Id: "trend-confirmation",
                Phase: "Triage",
                Severity: "Warning",
                Check: "Use the trend view to determine whether pressure is rising, falling, or holding steady.",
                ExpectedResult: "Trend output provides enough recent samples to distinguish a burst from sustained pressure."),
            new QueuePressureChecklistItem(
                Id: "action-plan-review",
                Phase: "Response",
                Severity: "Warning",
                Check: "Review the action-plan endpoint before changing worker capacity or migration run controls.",
                ExpectedResult: "Action-plan output gives operator-safe next steps that align with the current pressure level."),
            new QueuePressureChecklistItem(
                Id: "catalog-cross-check",
                Phase: "Response",
                Severity: "Info",
                Check: "Cross-check recommendation-catalog guidance for the matching pressure category and severity.",
                ExpectedResult: "Catalog recommendations explain why the selected action is appropriate."),
            new QueuePressureChecklistItem(
                Id: "post-action-validation",
                Phase: "FollowUp",
                Severity: "Info",
                Check: "After any operator action, rerun dashboard and trend smoke tests to confirm pressure direction changed as expected.",
                ExpectedResult: "Pressure does not continue to rise without a corresponding escalation decision.")
        };
    }

    private sealed record QueuePressureChecklistItem(
        string Id,
        string Phase,
        string Severity,
        string Check,
        string ExpectedResult);
}
