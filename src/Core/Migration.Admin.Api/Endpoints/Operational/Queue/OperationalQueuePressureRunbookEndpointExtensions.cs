namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure runbook endpoints.
/// This endpoint is intentionally metadata-only and compile-safe: it does not introduce new services, repositories, or DTO dependencies.
/// </summary>
public static class OperationalQueuePressureRunbookEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureRunbookApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/runbook",
                (string? phase) =>
                {
                    var phases = BuildRunbookPhases();

                    if (!string.IsNullOrWhiteSpace(phase))
                    {
                        phases = phases
                            .Where(x => string.Equals(x.Phase, phase, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                    }

                    return Results.Ok(new
                    {
                        runbook = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            filters = new
                            {
                                phase
                            },
                            endpoint = "/api/operational/queue-pressure/runbook",
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/trend",
                                "/api/operational/queue-pressure/action-plan",
                                "/api/operational/queue-pressure/recommendation-catalog",
                                "/api/operational/queue-pressure/operator-checklist",
                                "/api/operational/queue-pressure/escalation-guide",
                                "/api/operational/queue-pressure/incident-summary"
                            },
                            totalPhaseCount = phases.Length,
                            phases
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureRunbook")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure operator runbook.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/runbook/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        phaseCount = BuildRunbookPhases().Length,
                        endpoint = "/api/operational/queue-pressure/runbook"
                    }
                }))
            .WithName("GetOperationalQueuePressureRunbookReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure runbook readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static QueuePressureRunbookPhase[] BuildRunbookPhases()
    {
        return new[]
        {
            new QueuePressureRunbookPhase(
                Phase: "Detect",
                Objective: "Confirm whether queue pressure is present and observable.",
                OperatorSteps: new[]
                {
                    "Run endpoint discovery and confirm no duplicate /api/api route exists.",
                    "Open the queue pressure dashboard and verify the response root is populated.",
                    "Capture the current trend sample before making operational changes."
                },
                PrimaryEndpoint: "/api/operational/queue-pressure/dashboard",
                ExitCriteria: "Dashboard and trend endpoints respond successfully."),
            new QueuePressureRunbookPhase(
                Phase: "Assess",
                Objective: "Determine the likely severity and operational impact.",
                OperatorSteps: new[]
                {
                    "Review trend direction and pressure signals.",
                    "Compare action-plan and recommendation-catalog output.",
                    "Use the incident summary to classify the current state."
                },
                PrimaryEndpoint: "/api/operational/queue-pressure/incident-summary",
                ExitCriteria: "Operator has selected an Info, Warning, Critical, or Blocked disposition."),
            new QueuePressureRunbookPhase(
                Phase: "Act",
                Objective: "Apply the least disruptive operational response that reduces pressure.",
                OperatorSteps: new[]
                {
                    "Follow the operator checklist for the selected severity.",
                    "Avoid starting additional high-volume migration runs while pressure is elevated.",
                    "Escalate when pressure is sustained, severe, or telemetry cannot be trusted."
                },
                PrimaryEndpoint: "/api/operational/queue-pressure/operator-checklist",
                ExitCriteria: "Action-plan guidance has been followed or escalation has been initiated."),
            new QueuePressureRunbookPhase(
                Phase: "Verify",
                Objective: "Confirm whether pressure has stabilized after operator action.",
                OperatorSteps: new[]
                {
                    "Rerun the full smoke validation.",
                    "Capture a fresh dashboard and trend sample.",
                    "Record the before and after state in the incident notes if escalation occurred."
                },
                PrimaryEndpoint: "/api/operational/queue-pressure/trend",
                ExitCriteria: "Pressure is stable, decreasing, or the issue has been handed off with evidence.")
        };
    }

    private sealed record QueuePressureRunbookPhase(
        string Phase,
        string Objective,
        string[] OperatorSteps,
        string PrimaryEndpoint,
        string ExitCriteria);
}
