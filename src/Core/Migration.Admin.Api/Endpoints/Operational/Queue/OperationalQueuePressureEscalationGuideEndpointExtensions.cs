namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure escalation guide endpoints.
/// This endpoint is intentionally metadata-only and compile-safe: it does not introduce new services, repositories, or DTO dependencies.
/// </summary>
public static class OperationalQueuePressureEscalationGuideEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureEscalationGuideApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/escalation-guide",
                (string? level) =>
                {
                    var levels = BuildEscalationLevels();

                    if (!string.IsNullOrWhiteSpace(level))
                    {
                        levels = levels
                            .Where(x => string.Equals(x.Level, level, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                    }

                    return Results.Ok(new
                    {
                        escalationGuide = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            filters = new
                            {
                                level
                            },
                            endpoint = "/api/operational/queue-pressure/escalation-guide",
                            totalEscalationLevelCount = levels.Length,
                            levels = levels.Select(x => x.Level).ToArray(),
                            items = levels
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureEscalationGuide")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure escalation guide.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/escalation-guide/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        escalationLevelCount = BuildEscalationLevels().Length,
                        endpoint = "/api/operational/queue-pressure/escalation-guide"
                    }
                }))
            .WithName("GetOperationalQueuePressureEscalationGuideReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure escalation guide readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static QueuePressureEscalationLevel[] BuildEscalationLevels()
    {
        return new[]
        {
            new QueuePressureEscalationLevel(
                Level: "Info",
                Trigger: "Queue pressure is present but stable and operational smoke tests are passing.",
                Owner: "Migration operator",
                Response: "Continue monitoring dashboard and trend output. No capacity or run-control change is required.",
                Validation: "Rerun the full queue pressure smoke test after the next operational interval."),
            new QueuePressureEscalationLevel(
                Level: "Warning",
                Trigger: "Queue pressure trend is rising, dispatcher pressure is elevated, or operator-checklist warning items fail.",
                Owner: "Migration operator with implementation lead awareness",
                Response: "Review the action plan, confirm worker/run settings, and avoid starting additional high-volume runs until trend stabilizes.",
                Validation: "Dashboard and trend endpoints should show flat or falling pressure after the response window."),
            new QueuePressureEscalationLevel(
                Level: "Critical",
                Trigger: "Queue pressure remains high across repeated samples, dispatcher pressure remains constrained, or smoke validation fails.",
                Owner: "Implementation lead",
                Response: "Pause nonessential migration activity, capture dashboard/trend/action-plan output, and decide whether to scale workers, isolate failures, or pause affected runs.",
                Validation: "Full smoke validation must pass before resuming normal run volume."),
            new QueuePressureEscalationLevel(
                Level: "Blocked",
                Trigger: "Endpoint discovery fails, /api/api duplicate routes appear, or queue pressure data cannot be trusted.",
                Owner: "Engineering",
                Response: "Stop operational decision-making from this dashboard path until routing and smoke validation are repaired.",
                Validation: "Endpoint discovery, route check, smoke test, consistency test, and full smoke test all pass.")
        };
    }

    private sealed record QueuePressureEscalationLevel(
        string Level,
        string Trigger,
        string Owner,
        string Response,
        string Validation);
}


