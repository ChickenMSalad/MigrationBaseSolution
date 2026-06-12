namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure command center endpoints.
/// This endpoint is intentionally read-only and compile-safe: it provides an executive/operator command view over existing queue-pressure surfaces.
/// </summary>
public static class OperationalQueuePressureCommandCenterEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureCommandCenterApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/command-center",
                (string? pressureLevel, string? queueTrend, string? dispatcherState, string? incidentState) =>
                {
                    var selectedPressureLevel = Normalize(pressureLevel, "Elevated");
                    var selectedQueueTrend = Normalize(queueTrend, "Stable");
                    var selectedDispatcherState = Normalize(dispatcherState, "Nominal");
                    var selectedIncidentState = Normalize(incidentState, "None");
                    var severity = ResolveSeverity(selectedPressureLevel, selectedQueueTrend, selectedDispatcherState, selectedIncidentState);

                    return Results.Ok(new
                    {
                        commandCenter = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/command-center",
                            isReadOnly = true,
                            purpose = "Command-level queue pressure view for coordinating observation, readiness, mitigation, recovery, and review.",
                            inputs = new
                            {
                                pressureLevel = selectedPressureLevel,
                                queueTrend = selectedQueueTrend,
                                dispatcherState = selectedDispatcherState,
                                incidentState = selectedIncidentState
                            },
                            commandSummary = new
                            {
                                severity,
                                operatingMode = ResolveOperatingMode(severity),
                                needsCommandReview = severity is "High" or "Critical",
                                needsIncidentCadence = severity == "Critical" || selectedIncidentState.Equals("Active", StringComparison.OrdinalIgnoreCase),
                                recommendedCadence = ResolveCadence(severity)
                            },
                            commandPriorities = BuildPriorities(severity),
                            commandChecklist = BuildChecklist(severity),
                            communicationPlan = BuildCommunicationPlan(severity),
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/control-tower",
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/risk-banding",
                                "/api/operational/queue-pressure/decision-matrix",
                                "/api/operational/queue-pressure/escalation-guide",
                                "/api/operational/queue-pressure/recovery-workflow",
                                "/api/operational/queue-pressure/post-recovery-review"
                            },
                            guardrails = new[]
                            {
                                "Command center is advisory and read-only; it does not execute mitigation.",
                                "Critical command mode requires a named incident owner and explicit rollback path.",
                                "Any throughput change should be reviewed against safety-review and execution-readiness outputs."
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureCommandCenter")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure command center view.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/command-center/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/command-center",
                        mode = "ReadOnlyCommandView",
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" },
                        supportedQueueTrends = new[] { "Improving", "Stable", "Worsening", "Surging" },
                        supportedDispatcherStates = new[] { "Nominal", "Constrained", "Degraded", "Blocked" },
                        supportedIncidentStates = new[] { "None", "Watching", "Active", "Recovering" }
                    }
                }))
            .WithName("GetOperationalQueuePressureCommandCenterReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure command center endpoint readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string ResolveSeverity(string pressureLevel, string queueTrend, string dispatcherState, string incidentState)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            || queueTrend.Equals("Surging", StringComparison.OrdinalIgnoreCase)
            || dispatcherState.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
            || incidentState.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            return "Critical";
        }

        if (pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || queueTrend.Equals("Worsening", StringComparison.OrdinalIgnoreCase)
            || dispatcherState.Equals("Degraded", StringComparison.OrdinalIgnoreCase)
            || incidentState.Equals("Recovering", StringComparison.OrdinalIgnoreCase))
        {
            return "High";
        }

        if (pressureLevel.Equals("Elevated", StringComparison.OrdinalIgnoreCase)
            || dispatcherState.Equals("Constrained", StringComparison.OrdinalIgnoreCase)
            || incidentState.Equals("Watching", StringComparison.OrdinalIgnoreCase))
        {
            return "Elevated";
        }

        return "Normal";
    }

    private static string ResolveOperatingMode(string severity)
    {
        return severity switch
        {
            "Critical" => "IncidentCommand",
            "High" => "MitigationCommand",
            "Elevated" => "WatchCommand",
            _ => "NormalMonitoring"
        };
    }

    private static string ResolveCadence(string severity)
    {
        return severity switch
        {
            "Critical" => "Every 15 minutes until pressure exits critical state.",
            "High" => "Every 30 minutes while mitigation or recovery is active.",
            "Elevated" => "Hourly review while pressure remains elevated.",
            _ => "Normal operational cadence."
        };
    }

    private static string[] BuildPriorities(string severity)
    {
        return severity switch
        {
            "Critical" => new[]
            {
                "Protect run integrity before increasing throughput.",
                "Assign a named incident owner.",
                "Confirm rollback path and recovery workflow.",
                "Communicate status cadence to operators."
            },
            "High" => new[]
            {
                "Confirm risk band and decision matrix recommendation.",
                "Prepare conservative throttle or mitigation guidance.",
                "Track recovery readiness before making additional changes."
            },
            "Elevated" => new[]
            {
                "Watch trend direction.",
                "Review operator advisory.",
                "Prepare mitigation only if pressure worsens."
            },
            _ => new[] { "Continue normal monitoring." }
        };
    }

    private static string[] BuildChecklist(string severity)
    {
        if (severity == "Critical")
        {
            return new[]
            {
                "Open control tower.",
                "Open escalation guide.",
                "Confirm execution readiness.",
                "Confirm safety review.",
                "Run recovery workflow after mitigation."
            };
        }

        if (severity == "High")
        {
            return new[]
            {
                "Open risk banding.",
                "Open decision matrix.",
                "Review throttle policy.",
                "Confirm recovery readiness."
            };
        }

        if (severity == "Elevated")
        {
            return new[]
            {
                "Open dashboard.",
                "Open trend.",
                "Open operator advisory."
            };
        }

        return new[] { "Open dashboard as needed." };
    }

    private static object BuildCommunicationPlan(string severity)
    {
        return new
        {
            audience = severity is "Critical" or "High" ? "Operations lead and migration owner" : "Operations monitor",
            updateCadence = ResolveCadence(severity),
            requiredMessage = severity == "Critical"
                ? "Current pressure state, owner, mitigation decision, rollback readiness, and next review time."
                : "Current pressure state, trend, and next review time."
        };
    }
}


