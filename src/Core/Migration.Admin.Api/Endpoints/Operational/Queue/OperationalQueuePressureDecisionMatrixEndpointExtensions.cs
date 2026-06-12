namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure decision matrix endpoints.
/// This endpoint is intentionally read-only and compile-safe: it exposes deterministic operator decision support guidance.
/// </summary>
public static class OperationalQueuePressureDecisionMatrixEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureDecisionMatrixApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/decision-matrix",
                (string? pressureLevel, string? recoveryState) =>
                {
                    var selectedPressureLevel = NormalizePressureLevel(pressureLevel);
                    var selectedRecoveryState = NormalizeRecoveryState(recoveryState);
                    var rows = BuildDecisionRows(selectedPressureLevel, selectedRecoveryState);

                    return Results.Ok(new
                    {
                        decisionMatrix = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/decision-matrix",
                            filters = new
                            {
                                pressureLevel = selectedPressureLevel,
                                recoveryState = selectedRecoveryState
                            },
                            isReadOnly = true,
                            purpose = "Decision-support matrix for choosing conservative operator action during queue pressure events.",
                            summary = new
                            {
                                selectedPressureLevel,
                                selectedRecoveryState,
                                rowCount = rows.Length,
                                recommendedDecision = ResolveRecommendedDecision(selectedPressureLevel, selectedRecoveryState),
                                requiresLeadApproval = RequiresLeadApproval(selectedPressureLevel),
                                allowAutomatedMitigation = AllowsAutomatedMitigation(selectedPressureLevel, selectedRecoveryState)
                            },
                            rows,
                            guardrails = new[]
                            {
                                "Prefer read-only diagnosis before changing queue throughput.",
                                "Do not run concurrent manual mitigations without a named operator owner.",
                                "Use throttle-policy and safety-review endpoints before applying capacity-impacting action."
                            },
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/operator-advisory",
                                "/api/operational/queue-pressure/throttle-policy",
                                "/api/operational/queue-pressure/auto-mitigation",
                                "/api/operational/queue-pressure/safety-review"
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureDecisionMatrix")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure decision matrix guidance.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/decision-matrix/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/decision-matrix",
                        mode = "GuidanceOnly",
                        isReadOnly = true,
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" },
                        supportedRecoveryStates = new[] { "Active", "Stabilizing", "Recovered", "Review" }
                    }
                }))
            .WithName("GetOperationalQueuePressureDecisionMatrixReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure decision matrix endpoint readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string NormalizePressureLevel(string? pressureLevel)
    {
        return string.IsNullOrWhiteSpace(pressureLevel) ? "Elevated" : pressureLevel.Trim();
    }

    private static string NormalizeRecoveryState(string? recoveryState)
    {
        return string.IsNullOrWhiteSpace(recoveryState) ? "Active" : recoveryState.Trim();
    }

    private static bool RequiresLeadApproval(string pressureLevel)
    {
        return pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase);
    }

    private static bool AllowsAutomatedMitigation(string pressureLevel, string recoveryState)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !recoveryState.Equals("Review", StringComparison.OrdinalIgnoreCase)
            && !recoveryState.Equals("Recovered", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRecommendedDecision(string pressureLevel, string recoveryState)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase))
        {
            return "EscalateBeforeChange";
        }

        if (pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase))
        {
            return recoveryState.Equals("Stabilizing", StringComparison.OrdinalIgnoreCase)
                ? "HoldAndMonitor"
                : "LeadReviewedMitigation";
        }

        if (pressureLevel.Equals("Normal", StringComparison.OrdinalIgnoreCase))
        {
            return "MonitorOnly";
        }

        return "ConservativeMitigationReview";
    }

    private static QueuePressureDecisionRow[] BuildDecisionRows(string pressureLevel, string recoveryState)
    {
        var leadApproval = RequiresLeadApproval(pressureLevel);
        var automatedMitigation = AllowsAutomatedMitigation(pressureLevel, recoveryState);

        return new[]
        {
            new QueuePressureDecisionRow(
                Sequence: 1,
                Signal: "Pressure Level",
                ObservedValue: pressureLevel,
                Decision: ResolveRecommendedDecision(pressureLevel, recoveryState),
                RequiredAction: leadApproval ? "Confirm lead approval before action." : "Continue operator-owned review."),
            new QueuePressureDecisionRow(
                Sequence: 2,
                Signal: "Recovery State",
                ObservedValue: recoveryState,
                Decision: recoveryState.Equals("Recovered", StringComparison.OrdinalIgnoreCase) ? "PostRecoveryReview" : "ActiveMonitoring",
                RequiredAction: recoveryState.Equals("Recovered", StringComparison.OrdinalIgnoreCase) ? "Use post-recovery review before closing." : "Keep monitoring until stability is confirmed."),
            new QueuePressureDecisionRow(
                Sequence: 3,
                Signal: "Mitigation Mode",
                ObservedValue: automatedMitigation ? "AllowedWithGuardrails" : "ManualReviewRequired",
                Decision: automatedMitigation ? "UseSafetyReviewFirst" : "EscalateOrHold",
                RequiredAction: automatedMitigation ? "Check safety review before mitigation." : "Do not apply automation without explicit approval."),
            new QueuePressureDecisionRow(
                Sequence: 4,
                Signal: "Documentation",
                ObservedValue: leadApproval ? "Required" : "Recommended",
                Decision: "RecordDecision",
                RequiredAction: "Capture timestamp, owner, observed state, and next review point.")
        };
    }

    private sealed record QueuePressureDecisionRow(
        int Sequence,
        string Signal,
        string ObservedValue,
        string Decision,
        string RequiredAction);
}


