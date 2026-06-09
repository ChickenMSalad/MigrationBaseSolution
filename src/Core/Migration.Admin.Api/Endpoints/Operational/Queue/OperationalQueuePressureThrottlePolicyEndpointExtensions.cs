namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure throttle policy endpoints.
/// This endpoint is intentionally metadata-only and compile-safe: it does not introduce new services, repositories, or DTO dependencies.
/// </summary>
public static class OperationalQueuePressureThrottlePolicyEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureThrottlePolicyApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/throttle-policy",
                (string? pressureLevel) =>
                {
                    var selectedPressureLevel = NormalizePressureLevel(pressureLevel);
                    var policies = BuildPolicies();
                    var selectedPolicy = policies.FirstOrDefault(policy => string.Equals(policy.PressureLevel, selectedPressureLevel, StringComparison.OrdinalIgnoreCase))
                        ?? policies[0];

                    return Results.Ok(new
                    {
                        throttlePolicy = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/throttle-policy",
                            filters = new
                            {
                                pressureLevel = selectedPressureLevel
                            },
                            purpose = "Operator-facing throttle guidance for reducing migration pressure without introducing a new scheduling engine.",
                            selectedPolicy,
                            policies,
                            interpretation = new[]
                            {
                                "Use this as operational guidance before increasing or reducing migration throughput.",
                                "Confirm current state through dashboard, trend, stability index, and capacity forecast before changing run cadence.",
                                "Prefer gradual throttling decisions that can be reversed after pressure normalizes."
                            },
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/trend",
                                "/api/operational/queue-pressure/stability-index",
                                "/api/operational/queue-pressure/capacity-forecast",
                                "/api/operational/queue-pressure/operator-checklist"
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureThrottlePolicy")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure throttle policy guidance.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/throttle-policy/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/throttle-policy",
                        policyCount = BuildPolicies().Length
                    }
                }))
            .WithName("GetOperationalQueuePressureThrottlePolicyReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure throttle policy readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string NormalizePressureLevel(string? pressureLevel)
    {
        if (string.IsNullOrWhiteSpace(pressureLevel))
        {
            return "Elevated";
        }

        return pressureLevel.Trim();
    }

    private static QueuePressureThrottlePolicy[] BuildPolicies()
    {
        return new[]
        {
            new QueuePressureThrottlePolicy(
                PressureLevel: "Normal",
                OperatorPosture: "Observe",
                ThroughputGuidance: "Keep current migration cadence and continue standard monitoring.",
                SuggestedAction: "Review dashboard and trend during normal operational checkpoints."),
            new QueuePressureThrottlePolicy(
                PressureLevel: "Elevated",
                OperatorPosture: "Guard",
                ThroughputGuidance: "Avoid adding new large batches until trend and stability index confirm pressure is not worsening.",
                SuggestedAction: "Use capacity guardrails and operator checklist before approving more throughput."),
            new QueuePressureThrottlePolicy(
                PressureLevel: "High",
                OperatorPosture: "Throttle",
                ThroughputGuidance: "Reduce batch starts and prioritize drain-down before additional migration activity.",
                SuggestedAction: "Run the action plan and escalation guide, then reassess after pressure improves."),
            new QueuePressureThrottlePolicy(
                PressureLevel: "Critical",
                OperatorPosture: "Pause and recover",
                ThroughputGuidance: "Pause non-essential migration starts and focus on recovery workflow until pressure is stable.",
                SuggestedAction: "Use incident summary, recovery workflow, and post-recovery review endpoints.")
        };
    }

    private sealed record QueuePressureThrottlePolicy(
        string PressureLevel,
        string OperatorPosture,
        string ThroughputGuidance,
        string SuggestedAction);
}


