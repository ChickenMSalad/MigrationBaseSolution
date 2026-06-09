namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure operator advisory endpoints.
/// This endpoint is intentionally read-only and compile-safe: it gives operators concise next-step advisory guidance.
/// </summary>
public static class OperationalQueuePressureOperatorAdvisoryEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureOperatorAdvisoryApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/operator-advisory",
                (string? pressureLevel, string? mode) =>
                {
                    var selectedPressureLevel = NormalizePressureLevel(pressureLevel);
                    var selectedMode = NormalizeMode(mode);
                    var advisories = BuildOperatorAdvisories(selectedPressureLevel, selectedMode);

                    return Results.Ok(new
                    {
                        operatorAdvisory = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/operator-advisory",
                            filters = new
                            {
                                pressureLevel = selectedPressureLevel,
                                mode = selectedMode
                            },
                            isReadOnly = true,
                            purpose = "Operator-facing advisory guidance for active or recently recovered queue pressure conditions.",
                            summary = new
                            {
                                selectedPressureLevel,
                                selectedMode,
                                advisoryCount = advisories.Length,
                                recommendedTone = ResolveRecommendedTone(selectedPressureLevel),
                                requiresLeadReview = RequiresLeadReview(selectedPressureLevel),
                                requiresIncidentReference = RequiresIncidentReference(selectedPressureLevel)
                            },
                            advisories,
                            escalationReminder = ResolveEscalationReminder(selectedPressureLevel),
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/action-plan",
                                "/api/operational/queue-pressure/escalation-guide",
                                "/api/operational/queue-pressure/recovery-readiness",
                                "/api/operational/queue-pressure/execution-readiness"
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureOperatorAdvisory")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure operator advisory guidance.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/operator-advisory/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/operator-advisory",
                        mode = "GuidanceOnly",
                        isReadOnly = true,
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" },
                        supportedModes = new[] { "Active", "Recovery", "Review" }
                    }
                }))
            .WithName("GetOperationalQueuePressureOperatorAdvisoryReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure operator advisory endpoint readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string NormalizePressureLevel(string? pressureLevel)
    {
        return string.IsNullOrWhiteSpace(pressureLevel) ? "Elevated" : pressureLevel.Trim();
    }

    private static string NormalizeMode(string? mode)
    {
        return string.IsNullOrWhiteSpace(mode) ? "Active" : mode.Trim();
    }

    private static bool RequiresLeadReview(string pressureLevel)
    {
        return pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RequiresIncidentReference(string pressureLevel)
    {
        return pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRecommendedTone(string pressureLevel)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase))
        {
            return "Directive";
        }

        if (pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase))
        {
            return "Urgent";
        }

        return pressureLevel.Equals("Normal", StringComparison.OrdinalIgnoreCase)
            ? "Informational"
            : "Advisory";
    }

    private static string ResolveEscalationReminder(string pressureLevel)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase))
        {
            return "Critical pressure should remain attached to an active incident or lead-owned recovery workflow.";
        }

        if (pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase))
        {
            return "High pressure should have an explicit owner and next review point.";
        }

        return "Continue normal operational monitoring and document any manual intervention.";
    }

    private static QueuePressureOperatorAdvisory[] BuildOperatorAdvisories(string pressureLevel, string mode)
    {
        var leadReview = RequiresLeadReview(pressureLevel);
        var incidentReference = RequiresIncidentReference(pressureLevel);

        return new[]
        {
            new QueuePressureOperatorAdvisory(
                Sequence: 1,
                Category: "State Review",
                Priority: pressureLevel.Equals("Normal", StringComparison.OrdinalIgnoreCase) ? "Normal" : "High",
                Message: "Review dashboard, trend, and stability signals before taking or closing action."),
            new QueuePressureOperatorAdvisory(
                Sequence: 2,
                Category: "Ownership",
                Priority: leadReview ? "High" : "Recommended",
                Message: leadReview ? "Assign or confirm an operator lead before continuing." : "Confirm the operator responsible for monitoring this condition."),
            new QueuePressureOperatorAdvisory(
                Sequence: 3,
                Category: "Mode Guidance",
                Priority: "Recommended",
                Message: $"Use the {mode} workflow guidance and keep action notes concise and timestamped."),
            new QueuePressureOperatorAdvisory(
                Sequence: 4,
                Category: "Incident Linkage",
                Priority: incidentReference ? "Required" : "Optional",
                Message: incidentReference ? "Attach this condition to an incident, escalation, or recovery record." : "Create an incident reference only if pressure worsens or manual mitigation is applied.")
        };
    }

    private sealed record QueuePressureOperatorAdvisory(
        int Sequence,
        string Category,
        string Priority,
        string Message);
}


