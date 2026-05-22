namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure execution readiness endpoints.
/// This endpoint is intentionally read-only and compile-safe: it summarizes operator readiness before executing mitigations.
/// </summary>
public static class OperationalQueuePressureExecutionReadinessEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureExecutionReadinessApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/execution-readiness",
                (string? pressureLevel) =>
                {
                    var selectedPressureLevel = NormalizePressureLevel(pressureLevel);
                    var gates = BuildReadinessGates(selectedPressureLevel);
                    var blockingGateCount = gates.Count(gate => gate.IsBlocking);

                    return Results.Ok(new
                    {
                        executionReadiness = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/execution-readiness",
                            filters = new
                            {
                                pressureLevel = selectedPressureLevel
                            },
                            mode = "GuidanceOnly",
                            isReadOnly = true,
                            purpose = "Operator-facing readiness checklist before executing queue pressure response steps.",
                            summary = new
                            {
                                selectedPressureLevel,
                                isExecutionAuthorizedByEndpoint = false,
                                requiresHumanApproval = RequiresHumanApproval(selectedPressureLevel),
                                requiresRollbackPlan = RequiresRollbackPlan(selectedPressureLevel),
                                blockingGateCount,
                                readinessState = ResolveReadinessState(selectedPressureLevel, blockingGateCount)
                            },
                            gates,
                            requiredBeforeExecution = new[]
                            {
                                "Confirm current dashboard and trend data before acting.",
                                "Confirm the selected throttle or mitigation policy is still appropriate.",
                                "Confirm named owner, rollback path, and observation window.",
                                "Use approved operational tooling for any mutation; this endpoint is guidance-only."
                            },
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/throttle-policy",
                                "/api/operational/queue-pressure/auto-mitigation",
                                "/api/operational/queue-pressure/safety-review",
                                "/api/operational/queue-pressure/recovery-workflow"
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureExecutionReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure execution readiness guidance.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/execution-readiness/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/execution-readiness",
                        mode = "GuidanceOnly",
                        isReadOnly = true,
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" }
                    }
                }))
            .WithName("GetOperationalQueuePressureExecutionReadinessReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure execution readiness endpoint readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string NormalizePressureLevel(string? pressureLevel)
    {
        return string.IsNullOrWhiteSpace(pressureLevel) ? "Elevated" : pressureLevel.Trim();
    }

    private static bool RequiresHumanApproval(string pressureLevel)
    {
        return !pressureLevel.Equals("Normal", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RequiresRollbackPlan(string pressureLevel)
    {
        return pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveReadinessState(string pressureLevel, int blockingGateCount)
    {
        if (blockingGateCount > 0)
        {
            return "BlockedUntilReviewed";
        }

        return pressureLevel.Equals("Normal", StringComparison.OrdinalIgnoreCase)
            ? "ObservationReady"
            : "ReadyForHumanApprovedGuidance";
    }

    private static QueuePressureExecutionReadinessGate[] BuildReadinessGates(string pressureLevel)
    {
        return new[]
        {
            new QueuePressureExecutionReadinessGate(
                Sequence: 1,
                Name: "Evidence current",
                Status: "Required",
                IsBlocking: false,
                Description: "Dashboard, trend, stability index, and incident summary should be checked before execution."),
            new QueuePressureExecutionReadinessGate(
                Sequence: 2,
                Name: "Human owner assigned",
                Status: RequiresHumanApproval(pressureLevel) ? "Required" : "Optional",
                IsBlocking: false,
                Description: "Assign a named operator for Elevated, High, or Critical pressure response."),
            new QueuePressureExecutionReadinessGate(
                Sequence: 3,
                Name: "Rollback path documented",
                Status: RequiresRollbackPlan(pressureLevel) ? "Required" : "Recommended",
                IsBlocking: false,
                Description: "High and Critical pressure responses should have an explicit rollback or recovery workflow."),
            new QueuePressureExecutionReadinessGate(
                Sequence: 4,
                Name: "Mutation boundary confirmed",
                Status: "Required",
                IsBlocking: false,
                Description: "This endpoint does not authorize mutation; use approved operational tooling for changes.")
        };
    }

    private sealed record QueuePressureExecutionReadinessGate(
        int Sequence,
        string Name,
        string Status,
        bool IsBlocking,
        string Description);
}
