namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure auto-mitigation guidance endpoints.
/// This endpoint is intentionally metadata-only and compile-safe: it does not mutate migrations, schedules, queues, or dispatcher state.
/// </summary>
public static class OperationalQueuePressureAutoMitigationEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureAutoMitigationApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/auto-mitigation",
                (string? pressureLevel) =>
                {
                    var selectedPressureLevel = NormalizePressureLevel(pressureLevel);
                    var steps = BuildSteps(selectedPressureLevel);

                    return Results.Ok(new
                    {
                        autoMitigation = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/auto-mitigation",
                            filters = new
                            {
                                pressureLevel = selectedPressureLevel
                            },
                            mode = "GuidanceOnly",
                            isAutomaticMutationEnabled = false,
                            purpose = "Operator-facing auto-mitigation plan describing safe staged responses without changing queue, dispatcher, or run state.",
                            summary = new
                            {
                                selectedPressureLevel,
                                recommendedPosture = ResolvePosture(selectedPressureLevel),
                                requiresHumanApproval = true,
                                rollbackRequired = true
                            },
                            steps,
                            safeguards = new[]
                            {
                                "Does not start, pause, cancel, retry, or reprioritize migration work.",
                                "Use throttle policy and capacity forecast before approving throughput changes.",
                                "Record operator decision and review post-recovery endpoint after mitigation."
                            },
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/throttle-policy",
                                "/api/operational/queue-pressure/capacity-forecast",
                                "/api/operational/queue-pressure/recovery-workflow",
                                "/api/operational/queue-pressure/post-recovery-review"
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureAutoMitigation")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure auto-mitigation guidance.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/auto-mitigation/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/auto-mitigation",
                        mode = "GuidanceOnly",
                        isAutomaticMutationEnabled = false,
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" }
                    }
                }))
            .WithName("GetOperationalQueuePressureAutoMitigationReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure auto-mitigation readiness.")
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

    private static string ResolvePosture(string pressureLevel)
    {
        return pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            ? "PauseNonEssentialStarts"
            : pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
                ? "ThrottleAndDrain"
                : pressureLevel.Equals("Normal", StringComparison.OrdinalIgnoreCase)
                    ? "Observe"
                    : "GuardThroughput";
    }

    private static QueuePressureAutoMitigationStep[] BuildSteps(string pressureLevel)
    {
        var posture = ResolvePosture(pressureLevel);

        return new[]
        {
            new QueuePressureAutoMitigationStep(
                Sequence: 1,
                Name: "Confirm pressure state",
                Action: "Review dashboard, trend, and stability index before any throughput decision.",
                AutomationBoundary: "Read-only validation"),
            new QueuePressureAutoMitigationStep(
                Sequence: 2,
                Name: "Select mitigation posture",
                Action: $"Use posture '{posture}' as the proposed operator response for pressure level '{pressureLevel}'.",
                AutomationBoundary: "Recommendation only"),
            new QueuePressureAutoMitigationStep(
                Sequence: 3,
                Name: "Apply human-approved throttle",
                Action: "If approved, use existing operational processes to slow starts, pause non-essential batches, or hold new work.",
                AutomationBoundary: "No mutation from this endpoint"),
            new QueuePressureAutoMitigationStep(
                Sequence: 4,
                Name: "Verify recovery",
                Action: "Re-check stability, recovery workflow, and post-recovery review after pressure normalizes.",
                AutomationBoundary: "Read-only follow-up")
        };
    }

    private sealed record QueuePressureAutoMitigationStep(
        int Sequence,
        string Name,
        string Action,
        string AutomationBoundary);
}


