namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure capacity guardrails endpoints.
/// This endpoint is intentionally metadata-only and compile-safe: it does not introduce new services, repositories, or DTO dependencies.
/// </summary>
public static class OperationalQueuePressureCapacityGuardrailsEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureCapacityGuardrailsApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/capacity-guardrails",
                (string? mode) =>
                {
                    var selectedMode = string.IsNullOrWhiteSpace(mode) ? "standard" : mode.Trim();
                    var guardrails = BuildGuardrails();

                    return Results.Ok(new
                    {
                        capacityGuardrails = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/capacity-guardrails",
                            filters = new
                            {
                                mode = selectedMode
                            },
                            purpose = "Operator-facing guardrails for deciding when to hold, reduce, resume, or increase migration throughput under queue pressure.",
                            guardrails,
                            decisionFlow = new[]
                            {
                                "Check the dashboard endpoint for current pressure level.",
                                "Check the trend endpoint before increasing migration volume.",
                                "Use the stability index to confirm whether recovery is sustained.",
                                "Follow recovery workflow before resuming normal volume after an unstable period."
                            },
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/trend",
                                "/api/operational/queue-pressure/stability-index",
                                "/api/operational/queue-pressure/recovery-workflow",
                                "/api/operational/queue-pressure/post-recovery-review"
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureCapacityGuardrails")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure capacity guardrails.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/capacity-guardrails/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/capacity-guardrails",
                        guardrailCount = BuildGuardrails().Length
                    }
                }))
            .WithName("GetOperationalQueuePressureCapacityGuardrailsReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure capacity guardrail readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static QueuePressureCapacityGuardrail[] BuildGuardrails()
    {
        return new[]
        {
            new QueuePressureCapacityGuardrail(
                State: "Normal",
                OperatorDecision: "Proceed",
                Guidance: "Migration volume may continue when dashboard, trend, and stability checks are all healthy.",
                SuggestedEndpoint: "/api/operational/queue-pressure/dashboard"),
            new QueuePressureCapacityGuardrail(
                State: "Watch",
                OperatorDecision: "Hold increases",
                Guidance: "Do not increase throughput until trend evidence shows pressure is stable or improving.",
                SuggestedEndpoint: "/api/operational/queue-pressure/trend"),
            new QueuePressureCapacityGuardrail(
                State: "Constrained",
                OperatorDecision: "Reduce or pause increases",
                Guidance: "Use the action plan and operator checklist before allowing additional migration volume.",
                SuggestedEndpoint: "/api/operational/queue-pressure/action-plan"),
            new QueuePressureCapacityGuardrail(
                State: "Recovery",
                OperatorDecision: "Recover first",
                Guidance: "Follow recovery workflow and complete post-recovery review before returning to normal throughput.",
                SuggestedEndpoint: "/api/operational/queue-pressure/recovery-workflow")
        };
    }

    private sealed record QueuePressureCapacityGuardrail(
        string State,
        string OperatorDecision,
        string Guidance,
        string SuggestedEndpoint);
}


