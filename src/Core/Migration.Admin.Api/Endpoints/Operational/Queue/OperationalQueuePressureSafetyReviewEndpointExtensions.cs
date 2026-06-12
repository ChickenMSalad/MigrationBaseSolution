namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure safety review endpoints.
/// This endpoint is intentionally read-only and compile-safe: it documents operator safety checks before mitigation changes.
/// </summary>
public static class OperationalQueuePressureSafetyReviewEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureSafetyReviewApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/safety-review",
                (string? pressureLevel) =>
                {
                    var selectedPressureLevel = NormalizePressureLevel(pressureLevel);
                    var checks = BuildChecks(selectedPressureLevel);

                    return Results.Ok(new
                    {
                        safetyReview = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/safety-review",
                            filters = new
                            {
                                pressureLevel = selectedPressureLevel
                            },
                            mode = "GuidanceOnly",
                            isReadOnly = true,
                            isAutomaticMutationEnabled = false,
                            purpose = "Operator-facing safety review for queue pressure mitigation decisions.",
                            summary = new
                            {
                                selectedPressureLevel,
                                requiresHumanApproval = true,
                                requiresRollbackPlan = RequiresRollbackPlan(selectedPressureLevel),
                                minimumReviewState = ResolveMinimumReviewState(selectedPressureLevel)
                            },
                            checks,
                            stopConditions = new[]
                            {
                                "Do not proceed when dashboard, trend, and stability index disagree on pressure direction.",
                                "Do not proceed without a named operator and rollback path for High or Critical pressure.",
                                "Do not treat this endpoint as authorization to mutate queue, dispatcher, or run state."
                            },
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/stability-index",
                                "/api/operational/queue-pressure/auto-mitigation",
                                "/api/operational/queue-pressure/throttle-policy",
                                "/api/operational/queue-pressure/post-recovery-review"
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureSafetyReview")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure mitigation safety review guidance.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/safety-review/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/safety-review",
                        mode = "GuidanceOnly",
                        isReadOnly = true,
                        isAutomaticMutationEnabled = false,
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" }
                    }
                }))
            .WithName("GetOperationalQueuePressureSafetyReviewReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure safety review readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string NormalizePressureLevel(string? pressureLevel)
    {
        return string.IsNullOrWhiteSpace(pressureLevel) ? "Elevated" : pressureLevel.Trim();
    }

    private static bool RequiresRollbackPlan(string pressureLevel)
    {
        return pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveMinimumReviewState(string pressureLevel)
    {
        return pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            ? "IncidentCommanderApproval"
            : pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
                ? "OperatorLeadApproval"
                : pressureLevel.Equals("Normal", StringComparison.OrdinalIgnoreCase)
                    ? "ObservationOnly"
                    : "OperatorAcknowledgement";
    }

    private static QueuePressureSafetyCheck[] BuildChecks(string pressureLevel)
    {
        return new[]
        {
            new QueuePressureSafetyCheck(
                Sequence: 1,
                Name: "Confirm pressure evidence",
                Requirement: "Review dashboard, trend, incident summary, and stability index before acting.",
                RequiredForLevel: "All"),
            new QueuePressureSafetyCheck(
                Sequence: 2,
                Name: "Confirm mitigation boundary",
                Requirement: "Validate that the selected response is guidance-only here and any mutation uses approved operational tooling.",
                RequiredForLevel: "All"),
            new QueuePressureSafetyCheck(
                Sequence: 3,
                Name: "Confirm rollback path",
                Requirement: $"Ensure rollback or recovery workflow is documented before applying changes for '{pressureLevel}'.",
                RequiredForLevel: "HighOrCritical"),
            new QueuePressureSafetyCheck(
                Sequence: 4,
                Name: "Confirm post-change review",
                Requirement: "Schedule post-recovery review after pressure normalizes.",
                RequiredForLevel: "ElevatedOrHigher")
        };
    }

    private sealed record QueuePressureSafetyCheck(
        int Sequence,
        string Name,
        string Requirement,
        string RequiredForLevel);
}


