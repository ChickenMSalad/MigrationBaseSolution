namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure readout endpoints.
/// This endpoint is intentionally read-only and compile-safe: it produces a compact operator readout without executing mitigation actions.
/// </summary>
public static class OperationalQueuePressureReadoutEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureReadoutApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/readout",
                (string? pressureLevel, string? posture, string? readiness, string? recoveryState) =>
                {
                    var selectedPressureLevel = Normalize(pressureLevel, "Elevated");
                    var selectedPosture = Normalize(posture, "Guarded");
                    var selectedReadiness = Normalize(readiness, "Ready");
                    var selectedRecoveryState = Normalize(recoveryState, "Prepared");
                    var readoutLevel = ResolveReadoutLevel(selectedPressureLevel, selectedPosture, selectedReadiness, selectedRecoveryState);

                    return Results.Ok(new
                    {
                        queuePressureReadout = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            endpoint = "/api/operational/queue-pressure/readout",
                            isReadOnly = true,
                            purpose = "Compact operator readout for queue pressure status, readiness, and recovery coordination.",
                            inputs = new
                            {
                                pressureLevel = selectedPressureLevel,
                                posture = selectedPosture,
                                readiness = selectedReadiness,
                                recoveryState = selectedRecoveryState
                            },
                            status = new
                            {
                                readoutLevel,
                                requiresAttention = readoutLevel is "Watch" or "Act" or "Escalate",
                                operatorSummary = BuildOperatorSummary(readoutLevel),
                                recommendedCadence = ResolveCadence(readoutLevel)
                            },
                            readoutItems = BuildReadoutItems(selectedPressureLevel, selectedPosture, selectedReadiness, selectedRecoveryState),
                            nextOperatorMoves = BuildNextOperatorMoves(readoutLevel),
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/operational-posture",
                                "/api/operational/queue-pressure/executive-summary",
                                "/api/operational/queue-pressure/command-center",
                                "/api/operational/queue-pressure/recovery-readiness",
                                "/api/operational/queue-pressure/safety-review"
                            },
                            guardrails = new[]
                            {
                                "Readout is advisory and read-only.",
                                "Use safety review before any operational mitigation change.",
                                "Use command center for degraded or escalation scenarios."
                            }
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureReadout")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure operator readout.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/readout/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        endpoint = "/api/operational/queue-pressure/readout",
                        mode = "ReadOnlyQueuePressureReadout",
                        supportedPressureLevels = new[] { "Normal", "Elevated", "High", "Critical" },
                        supportedPostures = new[] { "Normal", "Guarded", "Degraded", "Critical" },
                        supportedReadinessStates = new[] { "Ready", "Watching", "Limited", "Blocked" },
                        supportedRecoveryStates = new[] { "None", "Prepared", "Active", "Blocked" }
                    }
                }))
            .WithName("GetOperationalQueuePressureReadoutReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure readout endpoint readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string ResolveReadoutLevel(string pressureLevel, string posture, string readiness, string recoveryState)
    {
        if (pressureLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            || posture.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            || readiness.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
            || recoveryState.Equals("Blocked", StringComparison.OrdinalIgnoreCase))
        {
            return "Escalate";
        }

        if (pressureLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || posture.Equals("Degraded", StringComparison.OrdinalIgnoreCase)
            || readiness.Equals("Limited", StringComparison.OrdinalIgnoreCase)
            || recoveryState.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            return "Act";
        }

        if (pressureLevel.Equals("Elevated", StringComparison.OrdinalIgnoreCase)
            || posture.Equals("Guarded", StringComparison.OrdinalIgnoreCase)
            || readiness.Equals("Watching", StringComparison.OrdinalIgnoreCase)
            || recoveryState.Equals("Prepared", StringComparison.OrdinalIgnoreCase))
        {
            return "Watch";
        }

        return "Observe";
    }

    private static string ResolveCadence(string readoutLevel)
    {
        return readoutLevel switch
        {
            "Escalate" => "Continuous review until pressure clears.",
            "Act" => "Review every active operator cycle.",
            "Watch" => "Review at the next scheduled checkpoint.",
            _ => "Normal monitoring cadence."
        };
    }

    private static string BuildOperatorSummary(string readoutLevel)
    {
        return readoutLevel switch
        {
            "Escalate" => "Queue pressure requires escalation, safety review, and recovery coordination.",
            "Act" => "Queue pressure needs active operator attention and mitigation readiness validation.",
            "Watch" => "Queue pressure is not normal; continue monitoring and keep mitigation prepared.",
            _ => "Queue pressure is within normal observation range."
        };
    }

    private static object[] BuildReadoutItems(string pressureLevel, string posture, string readiness, string recoveryState)
    {
        return new object[]
        {
            new { name = "Pressure", value = pressureLevel, note = "Current queue pressure signal supplied by the caller or upstream dashboard." },
            new { name = "Posture", value = posture, note = "Operational stance to use while reviewing queue conditions." },
            new { name = "Readiness", value = readiness, note = "Operator and execution readiness posture." },
            new { name = "Recovery", value = recoveryState, note = "Recovery coordination state." }
        };
    }

    private static string[] BuildNextOperatorMoves(string readoutLevel)
    {
        return readoutLevel switch
        {
            "Escalate" => new[]
            {
                "Open command center view.",
                "Run safety review before changes.",
                "Confirm recovery readiness and escalation ownership."
            },
            "Act" => new[]
            {
                "Review throttle policy and mitigation guidance.",
                "Confirm capacity guardrails.",
                "Track pressure trend before and after operator action."
            },
            "Watch" => new[]
            {
                "Keep mitigation plan prepared.",
                "Monitor trend and capacity forecast.",
                "Recheck readiness if pressure worsens."
            },
            _ => new[]
            {
                "Continue normal monitoring.",
                "Use dashboard if new queue pressure appears."
            }
        };
    }
}


