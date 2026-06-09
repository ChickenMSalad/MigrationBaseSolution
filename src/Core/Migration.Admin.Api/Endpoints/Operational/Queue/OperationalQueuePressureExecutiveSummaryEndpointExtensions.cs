namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure executive summary endpoints.
/// This endpoint is intentionally read-only and compile-safe: it summarizes the queue-pressure command surfaces without executing actions.
/// </summary>
public static class OperationalQueuePressureExecutiveSummaryEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureExecutiveSummaryApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/executive-summary",
                (string? pressureLevel, string? trend, string? mitigationState, string? recoveryState) =>
                {
                    var selectedPressureLevel = Normalize(pressureLevel, "Elevated");
                    var selectedTrend = Normalize(trend, "Stable");
                    var selectedMitigationState = Normalize(mitigationState, "Prepared");
                    var selectedRecoveryState = Normalize(recoveryState, "Ready");
                    var executiveStatus = ResolveExecutiveStatus(selectedPressureLevel, selectedTrend, selectedMitigationState, selectedRecoveryState);

                    return Results.Ok(new
                    {
                        executiveSummary = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/executive-summary",
                            isReadOnly = true,
                            purpose = "Executive-level summary of queue pressure posture, risk, mitigation posture, and recovery readiness.",
                            inputs = new
                            {
                                pressureLevel = selectedPressureLevel,
                                trend = selectedTrend,
                                mitigationState = selectedMitigationState,
                                recoveryState = selectedRecoveryState
                            },
                            status = new
                            {
                                executiveStatus,
                                attentionRequired = executiveStatus is "InterventionRequired" or "CriticalAttention",
                                operatorMode = ResolveOperatorMode(executiveStatus),
                                summary = BuildSummary(executiveStatus)
                            },
                            keySignals = BuildKeySignals(selectedPressureLevel, selectedTrend, selectedMitigationState, selectedRecoveryState),
                            recommendedBriefing = BuildBriefing(executiveStatus),
                            nextActions = BuildNextActions(executiveStatus),
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/command-center",
                                "/api/operational/queue-pressure/control-tower",
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/risk-banding",
                                "/api/operational/queue-pressure/recovery-readiness",
                                "/api/operational/queue-pressure/post-recovery-review"
                            },
                            guardrails = new[]
                            {
                                "Executive summary is advisory and read-only.",
                                "Use command center and control tower outputs for operator-level decisions.",
                                "Use safety review and execution readiness before approving throughput changes."
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureExecutiveSummary")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure executive summary.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/executive-summary/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/executive-summary",
                        mode = "ReadOnlyExecutiveSummary",
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" },
                        supportedTrends = new[] { "Improving", "Stable", "Worsening", "Surging" },
                        supportedMitigationStates = new[] { "None", "Prepared", "Active", "Blocked" },
                        supportedRecoveryStates = new[] { "Ready", "Watching", "Recovering", "Blocked" }
                    }
                }))
            .WithName("GetOperationalQueuePressureExecutiveSummaryReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure executive summary endpoint readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string ResolveExecutiveStatus(string pressureLevel, string trend, string mitigationState, string recoveryState)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            || trend.Equals("Surging", StringComparison.OrdinalIgnoreCase)
            || mitigationState.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
            || recoveryState.Equals("Blocked", StringComparison.OrdinalIgnoreCase))
        {
            return "CriticalAttention";
        }

        if (pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || trend.Equals("Worsening", StringComparison.OrdinalIgnoreCase)
            || mitigationState.Equals("Active", StringComparison.OrdinalIgnoreCase)
            || recoveryState.Equals("Recovering", StringComparison.OrdinalIgnoreCase))
        {
            return "InterventionRequired";
        }

        if (pressureLevel.Equals("Elevated", StringComparison.OrdinalIgnoreCase)
            || mitigationState.Equals("Prepared", StringComparison.OrdinalIgnoreCase)
            || recoveryState.Equals("Watching", StringComparison.OrdinalIgnoreCase))
        {
            return "Watch";
        }

        return "Stable";
    }

    private static string ResolveOperatorMode(string executiveStatus)
    {
        return executiveStatus switch
        {
            "CriticalAttention" => "IncidentCommand",
            "InterventionRequired" => "ActiveMitigation",
            "Watch" => "EnhancedMonitoring",
            _ => "NormalMonitoring"
        };
    }

    private static string BuildSummary(string executiveStatus)
    {
        return executiveStatus switch
        {
            "CriticalAttention" => "Queue pressure requires immediate command review, named ownership, and confirmed rollback/recovery posture.",
            "InterventionRequired" => "Queue pressure requires operator intervention planning and active monitoring until pressure improves.",
            "Watch" => "Queue pressure is elevated but manageable with enhanced monitoring and prepared mitigation guidance.",
            _ => "Queue pressure posture is stable and suitable for normal monitoring."
        };
    }

    private static object[] BuildKeySignals(string pressureLevel, string trend, string mitigationState, string recoveryState)
    {
        return new object[]
        {
            new { name = "Pressure Level", value = pressureLevel },
            new { name = "Trend", value = trend },
            new { name = "Mitigation State", value = mitigationState },
            new { name = "Recovery State", value = recoveryState }
        };
    }

    private static string[] BuildBriefing(string executiveStatus)
    {
        return executiveStatus switch
        {
            "CriticalAttention" => new[]
            {
                "Current pressure and trend.",
                "Named incident owner.",
                "Mitigation decision and rollback readiness.",
                "Recovery workflow state and next checkpoint."
            },
            "InterventionRequired" => new[]
            {
                "Current pressure and trend.",
                "Recommended mitigation posture.",
                "Recovery readiness and safety review status."
            },
            "Watch" => new[]
            {
                "Current pressure and trend.",
                "Trigger conditions for escalation.",
                "Next review time."
            },
            _ => new[] { "Current pressure posture and normal monitoring status." }
        };
    }

    private static string[] BuildNextActions(string executiveStatus)
    {
        return executiveStatus switch
        {
            "CriticalAttention" => new[]
            {
                "Open command center.",
                "Confirm escalation guide.",
                "Confirm execution readiness before any throughput change.",
                "Prepare post-recovery review once stabilized."
            },
            "InterventionRequired" => new[]
            {
                "Open decision matrix.",
                "Review throttle policy.",
                "Confirm recovery readiness."
            },
            "Watch" => new[]
            {
                "Review dashboard and trend.",
                "Keep operator advisory available."
            },
            _ => new[] { "Continue normal dashboard monitoring." }
        };
    }
}


