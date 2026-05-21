namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure operational posture endpoints.
/// This endpoint is intentionally read-only and compile-safe: it summarizes operational posture without executing mitigation actions.
/// </summary>
public static class OperationalQueuePressureOperationalPostureEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureOperationalPostureApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/operational-posture",
                (string? pressureLevel, string? trend, string? readiness, string? mitigationState) =>
                {
                    var selectedPressureLevel = Normalize(pressureLevel, "Elevated");
                    var selectedTrend = Normalize(trend, "Stable");
                    var selectedReadiness = Normalize(readiness, "Ready");
                    var selectedMitigationState = Normalize(mitigationState, "Prepared");
                    var posture = ResolvePosture(selectedPressureLevel, selectedTrend, selectedReadiness, selectedMitigationState);

                    return Results.Ok(new
                    {
                        operationalPosture = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/operational-posture",
                            isReadOnly = true,
                            purpose = "Operator-facing posture summary for queue pressure monitoring, mitigation readiness, and recovery coordination.",
                            inputs = new
                            {
                                pressureLevel = selectedPressureLevel,
                                trend = selectedTrend,
                                readiness = selectedReadiness,
                                mitigationState = selectedMitigationState
                            },
                            status = new
                            {
                                posture,
                                requiresOperatorAttention = posture is "Guarded" or "Degraded" or "Critical",
                                operatingMode = ResolveOperatingMode(posture),
                                summary = BuildSummary(posture)
                            },
                            postureSignals = BuildPostureSignals(selectedPressureLevel, selectedTrend, selectedReadiness, selectedMitigationState),
                            recommendedChecks = BuildRecommendedChecks(posture),
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/executive-summary",
                                "/api/operational/queue-pressure/command-center",
                                "/api/operational/queue-pressure/control-tower",
                                "/api/operational/queue-pressure/execution-readiness",
                                "/api/operational/queue-pressure/recovery-readiness",
                                "/api/operational/queue-pressure/safety-review"
                            },
                            guardrails = new[]
                            {
                                "Operational posture is advisory and read-only.",
                                "Use execution readiness and safety review before throughput or throttle changes.",
                                "Use command center when posture is degraded or critical."
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureOperationalPosture")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure operational posture.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/operational-posture/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/operational-posture",
                        mode = "ReadOnlyOperationalPosture",
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" },
                        supportedTrends = new[] { "Improving", "Stable", "Worsening", "Surging" },
                        supportedReadinessStates = new[] { "Ready", "Watching", "Limited", "Blocked" },
                        supportedMitigationStates = new[] { "None", "Prepared", "Active", "Blocked" }
                    }
                }))
            .WithName("GetOperationalQueuePressureOperationalPostureReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure operational posture endpoint readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string ResolvePosture(string pressureLevel, string trend, string readiness, string mitigationState)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            || trend.Equals("Surging", StringComparison.OrdinalIgnoreCase)
            || readiness.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
            || mitigationState.Equals("Blocked", StringComparison.OrdinalIgnoreCase))
        {
            return "Critical";
        }

        if (pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || trend.Equals("Worsening", StringComparison.OrdinalIgnoreCase)
            || readiness.Equals("Limited", StringComparison.OrdinalIgnoreCase)
            || mitigationState.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            return "Degraded";
        }

        if (pressureLevel.Equals("Elevated", StringComparison.OrdinalIgnoreCase)
            || readiness.Equals("Watching", StringComparison.OrdinalIgnoreCase)
            || mitigationState.Equals("Prepared", StringComparison.OrdinalIgnoreCase))
        {
            return "Guarded";
        }

        return "Healthy";
    }

    private static string ResolveOperatingMode(string posture)
    {
        return posture switch
        {
            "Critical" => "IncidentCommand",
            "Degraded" => "ActiveOperationalControl",
            "Guarded" => "EnhancedMonitoring",
            _ => "NormalOperations"
        };
    }

    private static string BuildSummary(string posture)
    {
        return posture switch
        {
            "Critical" => "Operational posture is critical; use command center, escalation, and recovery readiness before additional load.",
            "Degraded" => "Operational posture is degraded; operators should actively monitor and prepare mitigation decisions.",
            "Guarded" => "Operational posture is guarded; pressure is manageable but needs enhanced monitoring.",
            _ => "Operational posture is healthy and suitable for normal monitoring."
        };
    }

    private static object[] BuildPostureSignals(string pressureLevel, string trend, string readiness, string mitigationState)
    {
        return new object[]
        {
            new { name = "Pressure Level", value = pressureLevel },
            new { name = "Trend", value = trend },
            new { name = "Readiness", value = readiness },
            new { name = "Mitigation State", value = mitigationState }
        };
    }

    private static string[] BuildRecommendedChecks(string posture)
    {
        return posture switch
        {
            "Critical" => new[]
            {
                "Open command center.",
                "Confirm escalation guide and named owner.",
                "Confirm recovery readiness before further execution.",
                "Prepare post-recovery review checkpoint."
            },
            "Degraded" => new[]
            {
                "Review decision matrix.",
                "Review throttle policy and safety review.",
                "Confirm operator advisory is current."
            },
            "Guarded" => new[]
            {
                "Review dashboard and trend.",
                "Confirm capacity guardrails and forecast.",
                "Keep mitigation plan prepared."
            },
            _ => new[] { "Continue normal dashboard monitoring." }
        };
    }
}
