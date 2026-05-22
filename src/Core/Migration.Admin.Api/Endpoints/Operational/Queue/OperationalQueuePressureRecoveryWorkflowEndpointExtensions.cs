namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure recovery workflow endpoints.
/// This endpoint is intentionally metadata-only and compile-safe: it does not introduce new services, repositories, or DTO dependencies.
/// </summary>
public static class OperationalQueuePressureRecoveryWorkflowEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureRecoveryWorkflowApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/recovery-workflow",
                (string? stage) =>
                {
                    var stages = BuildRecoveryStages();

                    if (!string.IsNullOrWhiteSpace(stage))
                    {
                        stages = stages
                            .Where(x => string.Equals(x.Stage, stage, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                    }

                    return Results.Ok(new
                    {
                        recoveryWorkflow = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            filters = new
                            {
                                stage
                            },
                            endpoint = "/api/operational/queue-pressure/recovery-workflow",
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/trend",
                                "/api/operational/queue-pressure/action-plan",
                                "/api/operational/queue-pressure/recommendation-catalog",
                                "/api/operational/queue-pressure/operator-checklist",
                                "/api/operational/queue-pressure/escalation-guide",
                                "/api/operational/queue-pressure/incident-summary",
                                "/api/operational/queue-pressure/runbook"
                            },
                            totalStageCount = stages.Length,
                            stages
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureRecoveryWorkflow")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure recovery workflow.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/recovery-workflow/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        stageCount = BuildRecoveryStages().Length,
                        endpoint = "/api/operational/queue-pressure/recovery-workflow"
                    }
                }))
            .WithName("GetOperationalQueuePressureRecoveryWorkflowReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure recovery workflow readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static QueuePressureRecoveryStage[] BuildRecoveryStages()
    {
        return new[]
        {
            new QueuePressureRecoveryStage(
                Stage: "Stabilize",
                Objective: "Stop queue pressure from worsening before changing migration throughput.",
                OperatorActions: new[]
                {
                    "Confirm the incident summary and runbook agree on the current disposition.",
                    "Pause new high-volume migration starts when pressure is elevated.",
                    "Capture dashboard and trend output before mitigation begins."
                },
                ValidationEndpoint: "/api/operational/queue-pressure/incident-summary",
                CompletionSignal: "Current pressure state is known and no new pressure source is being introduced."),
            new QueuePressureRecoveryStage(
                Stage: "Reduce",
                Objective: "Apply the least disruptive pressure-reduction action available.",
                OperatorActions: new[]
                {
                    "Follow the operator checklist for the active severity.",
                    "Use the action plan to choose the next operational step.",
                    "Escalate only when local operator action is insufficient or telemetry is unreliable."
                },
                ValidationEndpoint: "/api/operational/queue-pressure/action-plan",
                CompletionSignal: "The selected mitigation has been applied or escalation has been initiated."),
            new QueuePressureRecoveryStage(
                Stage: "Verify",
                Objective: "Confirm pressure is stable or decreasing after recovery action.",
                OperatorActions: new[]
                {
                    "Run the queue pressure full smoke validation.",
                    "Review a fresh trend sample after mitigation.",
                    "Compare current dashboard output against the captured pre-mitigation state."
                },
                ValidationEndpoint: "/api/operational/queue-pressure/trend",
                CompletionSignal: "Trend and dashboard output show stable or improving pressure."),
            new QueuePressureRecoveryStage(
                Stage: "Resume",
                Objective: "Return migration activity gradually once pressure is under control.",
                OperatorActions: new[]
                {
                    "Resume work in small batches instead of immediately restoring full load.",
                    "Monitor dashboard and incident summary after each resume step.",
                    "Document the recovery outcome when escalation or manual intervention occurred."
                },
                ValidationEndpoint: "/api/operational/queue-pressure/dashboard",
                CompletionSignal: "Migration activity is resumed and queue pressure remains observable and acceptable.")
        };
    }

    private sealed record QueuePressureRecoveryStage(
        string Stage,
        string Objective,
        string[] OperatorActions,
        string ValidationEndpoint,
        string CompletionSignal);
}
