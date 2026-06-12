namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure finalization endpoints.
/// This endpoint is intentionally read-only and compile-safe: it summarizes whether queue pressure work is ready to close.
/// </summary>
public static class OperationalQueuePressureFinalizationEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureFinalizationApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/finalization",
                (string? pressureLevel, string? posture, string? readiness, string? recoveryState, int? openActions) =>
                {
                    var selectedPressureLevel = Normalize(pressureLevel, "Normal");
                    var selectedPosture = Normalize(posture, "Normal");
                    var selectedReadiness = Normalize(readiness, "Ready");
                    var selectedRecoveryState = Normalize(recoveryState, "None");
                    var selectedOpenActions = Math.Max(0, openActions ?? 0);
                    var finalizationState = ResolveFinalizationState(selectedPressureLevel, selectedPosture, selectedReadiness, selectedRecoveryState, selectedOpenActions);

                    return Results.Ok(new
                    {
                        queuePressureFinalization = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/finalization",
                            isReadOnly = true,
                            purpose = "Final closeout summary for queue pressure operations after mitigation, recovery, or operator review.",
                            inputs = new
                            {
                                pressureLevel = selectedPressureLevel,
                                posture = selectedPosture,
                                readiness = selectedReadiness,
                                recoveryState = selectedRecoveryState,
                                openActions = selectedOpenActions
                            },
                            status = new
                            {
                                finalizationState,
                                canCloseOperationalReview = finalizationState == "ReadyToClose",
                                requiresFollowUp = finalizationState is "FollowUpRequired" or "HoldOpen" or "EscalationRequired",
                                operatorSummary = BuildOperatorSummary(finalizationState)
                            },
                            closeoutChecklist = BuildCloseoutChecklist(finalizationState, selectedOpenActions),
                            evidenceToCapture = BuildEvidenceToCapture(finalizationState),
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/readout",
                                "/api/operational/queue-pressure/post-recovery-review",
                                "/api/operational/queue-pressure/safety-review",
                                "/api/operational/queue-pressure/recovery-readiness",
                                "/api/operational/queue-pressure/executive-summary"
                            },
                            guardrails = new[]
                            {
                                "Finalization is advisory and read-only.",
                                "Do not close review while pressure is high, critical, blocked, or degraded.",
                                "Capture evidence before committing an operational closeout."
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureFinalization")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure finalization summary.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/finalization/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/finalization",
                        mode = "ReadOnlyQueuePressureFinalization",
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" },
                        supportedPostures = new[] { "Normal", "Guarded", "Degraded", "Critical" },
                        supportedReadinessStates = new[] { "Ready", "Watching", "Limited", "Blocked" },
                        supportedRecoveryStates = new[] { "None", "Prepared", "Active", "Blocked" }
                    }
                }))
            .WithName("GetOperationalQueuePressureFinalizationReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure finalization endpoint readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string ResolveFinalizationState(string pressureLevel, string posture, string readiness, string recoveryState, int openActions)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            || posture.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            || readiness.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
            || recoveryState.Equals("Blocked", StringComparison.OrdinalIgnoreCase))
        {
            return "EscalationRequired";
        }

        if (pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || posture.Equals("Degraded", StringComparison.OrdinalIgnoreCase)
            || readiness.Equals("Limited", StringComparison.OrdinalIgnoreCase)
            || recoveryState.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            return "HoldOpen";
        }

        if (openActions > 0
            || pressureLevel.Equals("Elevated", StringComparison.OrdinalIgnoreCase)
            || posture.Equals("Guarded", StringComparison.OrdinalIgnoreCase)
            || readiness.Equals("Watching", StringComparison.OrdinalIgnoreCase)
            || recoveryState.Equals("Prepared", StringComparison.OrdinalIgnoreCase))
        {
            return "FollowUpRequired";
        }

        return "ReadyToClose";
    }

    private static string BuildOperatorSummary(string finalizationState)
    {
        return finalizationState switch
        {
            "EscalationRequired" => "Do not finalize. Escalation or blocked recovery conditions are still present.",
            "HoldOpen" => "Keep the review open until degraded, high-pressure, or active recovery conditions clear.",
            "FollowUpRequired" => "Resolve remaining follow-up items before closing the operational review.",
            _ => "Queue pressure review is ready for closeout."
        };
    }

    private static object[] BuildCloseoutChecklist(string finalizationState, int openActions)
    {
        return new object[]
        {
            new { name = "Pressure normalized", isRequired = true, isSatisfied = finalizationState == "ReadyToClose", note = "Pressure should be normal before closeout." },
            new { name = "Recovery inactive", isRequired = true, isSatisfied = finalizationState != "HoldOpen" && finalizationState != "EscalationRequired", note = "Active or blocked recovery should keep the review open." },
            new { name = "Open actions resolved", isRequired = true, isSatisfied = openActions == 0, note = $"Open actions supplied: {openActions}." },
            new { name = "Evidence captured", isRequired = true, isSatisfied = finalizationState == "ReadyToClose", note = "Capture dashboard, readout, and post-recovery review evidence." }
        };
    }

    private static string[] BuildEvidenceToCapture(string finalizationState)
    {
        if (finalizationState == "ReadyToClose")
        {
            return new[]
            {
                "Final dashboard or readout snapshot.",
                "Post-recovery review summary.",
                "Confirmation that no operator actions remain open."
            };
        }

        return new[]
        {
            "Current readout or command center state.",
            "Reason the review remains open.",
            "Named follow-up owner and next checkpoint."
        };
    }
}


