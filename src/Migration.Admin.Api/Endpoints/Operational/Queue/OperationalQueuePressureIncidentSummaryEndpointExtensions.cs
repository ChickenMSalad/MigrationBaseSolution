namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure incident summary endpoints.
/// This endpoint is intentionally metadata-only and compile-safe: it does not introduce new services, repositories, or DTO dependencies.
/// </summary>
public static class OperationalQueuePressureIncidentSummaryEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureIncidentSummaryApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/incident-summary",
                (string? severity) =>
                {
                    var states = BuildIncidentStates();

                    if (!string.IsNullOrWhiteSpace(severity))
                    {
                        states = states
                            .Where(x => string.Equals(x.Severity, severity, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                    }

                    return Results.Ok(new
                    {
                        incidentSummary = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            filters = new
                            {
                                severity
                            },
                            endpoint = "/api/operational/queue-pressure/incident-summary",
                            totalIncidentStateCount = states.Length,
                            severities = states.Select(x => x.Severity).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                            states
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureIncidentSummary")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure incident summary reference.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/incident-summary/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        incidentStateCount = BuildIncidentStates().Length,
                        endpoint = "/api/operational/queue-pressure/incident-summary"
                    }
                }))
            .WithName("GetOperationalQueuePressureIncidentSummaryReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure incident summary readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static QueuePressureIncidentState[] BuildIncidentStates()
    {
        return new[]
        {
            new QueuePressureIncidentState(
                Severity: "Info",
                Summary: "Queue pressure is observable and current checks are passing.",
                OperatorSignal: "Dashboard, trend, action-plan, recommendation catalog, checklist, and escalation guide routes are available.",
                SuggestedDisposition: "Monitor only.",
                EvidenceToCapture: "Current dashboard and trend output if operators want a baseline."),
            new QueuePressureIncidentState(
                Severity: "Warning",
                Summary: "Queue pressure or dispatcher pressure is elevated enough to require operator attention.",
                OperatorSignal: "Trend output is rising, checklist contains warning actions, or action-plan response recommends slowing new run starts.",
                SuggestedDisposition: "Review active run volume and avoid adding additional high-volume migration work until pressure stabilizes.",
                EvidenceToCapture: "Dashboard, trend, action-plan, and checklist output."),
            new QueuePressureIncidentState(
                Severity: "Critical",
                Summary: "Queue pressure appears sustained or severe and may impact migration throughput or reliability.",
                OperatorSignal: "Repeated high-pressure samples, constrained dispatcher pressure, failed validation, or action-plan critical recommendations.",
                SuggestedDisposition: "Escalate to implementation lead and decide whether to pause, isolate, or scale the affected work.",
                EvidenceToCapture: "Dashboard, trend, action-plan, checklist, escalation guide, and failed smoke output."),
            new QueuePressureIncidentState(
                Severity: "Blocked",
                Summary: "Operational queue pressure telemetry cannot be trusted for incident decisions.",
                OperatorSignal: "Endpoint discovery fails, route checks fail, duplicate /api/api route appears, or smoke tests fail.",
                SuggestedDisposition: "Stop relying on this operational surface until routing and validation are fixed.",
                EvidenceToCapture: "Route-check failure, full-smoke failure, and relevant application logs.")
        };
    }

    private sealed record QueuePressureIncidentState(
        string Severity,
        string Summary,
        string OperatorSignal,
        string SuggestedDisposition,
        string EvidenceToCapture);
}
