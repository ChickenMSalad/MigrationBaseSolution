namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure recovery readiness endpoints.
/// This endpoint is intentionally read-only and compile-safe: it summarizes recovery readiness after queue pressure intervention.
/// </summary>
public static class OperationalQueuePressureRecoveryReadinessEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureRecoveryReadinessApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/recovery-readiness",
                (string? pressureLevel) =>
                {
                    var selectedPressureLevel = NormalizePressureLevel(pressureLevel);
                    var checkpoints = BuildRecoveryReadinessCheckpoints(selectedPressureLevel);
                    var requiredCheckpointCount = checkpoints.Count(checkpoint => checkpoint.IsRequired);

                    return Results.Ok(new
                    {
                        recoveryReadiness = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/recovery-readiness",
                            filters = new
                            {
                                pressureLevel = selectedPressureLevel
                            },
                            mode = "GuidanceOnly",
                            isReadOnly = true,
                            purpose = "Operator-facing recovery readiness checklist after queue pressure response activity.",
                            summary = new
                            {
                                selectedPressureLevel,
                                isRecoveryAuthorizedByEndpoint = false,
                                requiresHumanReview = RequiresHumanReview(selectedPressureLevel),
                                requiresPostRecoveryObservation = RequiresPostRecoveryObservation(selectedPressureLevel),
                                requiredCheckpointCount,
                                readinessState = ResolveReadinessState(selectedPressureLevel)
                            },
                            checkpoints,
                            requiredBeforeRecoveryCloseout = new[]
                            {
                                "Confirm queue pressure has stabilized or is trending down.",
                                "Confirm throttles, mitigations, or operator actions are either reverted or intentionally retained.",
                                "Confirm failure and retry rates are not rising after recovery action.",
                                "Record owner, observation window, and next review point."
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
            .WithName("GetOperationalQueuePressureRecoveryReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure recovery readiness guidance.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/recovery-readiness/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/recovery-readiness",
                        mode = "GuidanceOnly",
                        isReadOnly = true,
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" }
                    }
                }))
            .WithName("GetOperationalQueuePressureRecoveryReadinessReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure recovery readiness endpoint readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string NormalizePressureLevel(string? pressureLevel)
    {
        return string.IsNullOrWhiteSpace(pressureLevel) ? "Elevated" : pressureLevel.Trim();
    }

    private static bool RequiresHumanReview(string pressureLevel)
    {
        return !pressureLevel.Equals("Normal", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RequiresPostRecoveryObservation(string pressureLevel)
    {
        return pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveReadinessState(string pressureLevel)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase))
        {
            return "RecoveryReviewRequired";
        }

        if (pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase))
        {
            return "ObservationRequiredBeforeCloseout";
        }

        return pressureLevel.Equals("Normal", StringComparison.OrdinalIgnoreCase)
            ? "CloseoutReady"
            : "OperatorReviewRecommended";
    }

    private static QueuePressureRecoveryReadinessCheckpoint[] BuildRecoveryReadinessCheckpoints(string pressureLevel)
    {
        return new[]
        {
            new QueuePressureRecoveryReadinessCheckpoint(
                Sequence: 1,
                Name: "Pressure state reviewed",
                Status: "Required",
                IsRequired: true,
                Description: "Review dashboard, trend, and stability data before closing recovery."),
            new QueuePressureRecoveryReadinessCheckpoint(
                Sequence: 2,
                Name: "Mitigation state reviewed",
                Status: RequiresHumanReview(pressureLevel) ? "Required" : "Recommended",
                IsRequired: RequiresHumanReview(pressureLevel),
                Description: "Confirm any throttles, mitigations, or operational changes are intentionally retained or reverted."),
            new QueuePressureRecoveryReadinessCheckpoint(
                Sequence: 3,
                Name: "Post-recovery observation window",
                Status: RequiresPostRecoveryObservation(pressureLevel) ? "Required" : "Recommended",
                IsRequired: RequiresPostRecoveryObservation(pressureLevel),
                Description: "High and Critical recovery paths should include explicit post-recovery observation."),
            new QueuePressureRecoveryReadinessCheckpoint(
                Sequence: 4,
                Name: "Closeout notes prepared",
                Status: "Recommended",
                IsRequired: false,
                Description: "Capture owner, timeline, action summary, current state, and next review point.")
        };
    }

    private sealed record QueuePressureRecoveryReadinessCheckpoint(
        int Sequence,
        string Name,
        string Status,
        bool IsRequired,
        string Description);
}


