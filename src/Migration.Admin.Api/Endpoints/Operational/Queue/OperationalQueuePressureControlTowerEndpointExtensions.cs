namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure control tower endpoints.
/// This endpoint is intentionally read-only and compile-safe: it consolidates operator navigation and decision context.
/// </summary>
public static class OperationalQueuePressureControlTowerEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureControlTowerApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/control-tower",
                (string? pressureLevel, string? queueTrend, string? dispatcherState) =>
                {
                    var selectedPressureLevel = Normalize(pressureLevel, "Elevated");
                    var selectedQueueTrend = Normalize(queueTrend, "Stable");
                    var selectedDispatcherState = Normalize(dispatcherState, "Nominal");
                    var severity = ResolveSeverity(selectedPressureLevel, selectedQueueTrend, selectedDispatcherState);

                    return Results.Ok(new
                    {
                        controlTower = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/control-tower",
                            isReadOnly = true,
                            purpose = "Single operator navigation surface for queue pressure review, readiness, mitigation, recovery, and post-review.",
                            inputs = new
                            {
                                pressureLevel = selectedPressureLevel,
                                queueTrend = selectedQueueTrend,
                                dispatcherState = selectedDispatcherState
                            },
                            summary = new
                            {
                                severity,
                                posture = ResolvePosture(severity),
                                requiresLeadReview = severity is "High" or "Critical",
                                requiresIncidentOwner = severity == "Critical",
                                recommendedFirstStep = ResolveFirstStep(severity)
                            },
                            lanes = BuildLanes(severity),
                            operatorSequence = BuildOperatorSequence(severity),
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/trend",
                                "/api/operational/queue-pressure/risk-banding",
                                "/api/operational/queue-pressure/decision-matrix",
                                "/api/operational/queue-pressure/safety-review",
                                "/api/operational/queue-pressure/execution-readiness",
                                "/api/operational/queue-pressure/auto-mitigation",
                                "/api/operational/queue-pressure/recovery-readiness",
                                "/api/operational/queue-pressure/post-recovery-review"
                            },
                            guardrails = new[]
                            {
                                "Control tower is navigation and guidance only; it does not mutate queue state.",
                                "Use safety-review and execution-readiness before any throttle or mitigation action.",
                                "Use post-recovery-review after any recovery workflow or operational incident."
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureControlTower")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure control tower guidance surface.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/control-tower/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/control-tower",
                        mode = "GuidanceOnly",
                        isReadOnly = true,
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" },
                        supportedQueueTrends = new[] { "Improving", "Stable", "Worsening", "Surging" },
                        supportedDispatcherStates = new[] { "Nominal", "Constrained", "Degraded", "Blocked" }
                    }
                }))
            .WithName("GetOperationalQueuePressureControlTowerReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure control tower endpoint readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string ResolveSeverity(string pressureLevel, string queueTrend, string dispatcherState)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            || queueTrend.Equals("Surging", StringComparison.OrdinalIgnoreCase)
            || dispatcherState.Equals("Blocked", StringComparison.OrdinalIgnoreCase))
        {
            return "Critical";
        }

        if (pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || queueTrend.Equals("Worsening", StringComparison.OrdinalIgnoreCase)
            || dispatcherState.Equals("Degraded", StringComparison.OrdinalIgnoreCase))
        {
            return "High";
        }

        if (pressureLevel.Equals("Elevated", StringComparison.OrdinalIgnoreCase)
            || dispatcherState.Equals("Constrained", StringComparison.OrdinalIgnoreCase))
        {
            return "Elevated";
        }

        return "Normal";
    }

    private static string ResolvePosture(string severity)
    {
        return severity switch
        {
            "Critical" => "Incident-led response with named owner and explicit rollback path.",
            "High" => "Lead-reviewed mitigation readiness with frequent reassessment.",
            "Elevated" => "Active monitoring with conservative action preparation.",
            _ => "Monitor only."
        };
    }

    private static string ResolveFirstStep(string severity)
    {
        return severity switch
        {
            "Critical" => "Open escalation guide and assign an incident owner before applying changes.",
            "High" => "Review risk banding, safety review, and execution readiness.",
            "Elevated" => "Review dashboard, trend, and operator advisory before changing throughput.",
            _ => "Continue dashboard monitoring."
        };
    }

    private static QueuePressureControlLane[] BuildLanes(string severity)
    {
        return new[]
        {
            new QueuePressureControlLane("Observe", "Dashboard and trend review", "/api/operational/queue-pressure/dashboard", true),
            new QueuePressureControlLane("Assess", "Risk banding and decision matrix", "/api/operational/queue-pressure/risk-banding", true),
            new QueuePressureControlLane("Prepare", "Safety review and execution readiness", "/api/operational/queue-pressure/safety-review", severity is "Elevated" or "High" or "Critical"),
            new QueuePressureControlLane("Mitigate", "Throttle policy and auto-mitigation guidance", "/api/operational/queue-pressure/throttle-policy", severity is "High" or "Critical"),
            new QueuePressureControlLane("Recover", "Recovery readiness and workflow", "/api/operational/queue-pressure/recovery-readiness", severity is "High" or "Critical"),
            new QueuePressureControlLane("Review", "Post-recovery review", "/api/operational/queue-pressure/post-recovery-review", true)
        };
    }

    private static string[] BuildOperatorSequence(string severity)
    {
        if (severity == "Critical")
        {
            return new[]
            {
                "Assign incident owner.",
                "Open escalation guide.",
                "Review safety-review and execution-readiness.",
                "Apply only approved mitigation guidance.",
                "Run recovery workflow and post-recovery review."
            };
        }

        if (severity == "High")
        {
            return new[]
            {
                "Review risk banding.",
                "Confirm safety guardrails.",
                "Prepare conservative mitigation.",
                "Reassess trend after mitigation."
            };
        }

        if (severity == "Elevated")
        {
            return new[]
            {
                "Monitor dashboard and trend.",
                "Review operator advisory.",
                "Prepare but do not apply mitigation unless pressure worsens."
            };
        }

        return new[] { "Continue normal monitoring." };
    }

    private sealed record QueuePressureControlLane(string Name, string Description, string PrimaryEndpoint, bool IsRecommended);
}
