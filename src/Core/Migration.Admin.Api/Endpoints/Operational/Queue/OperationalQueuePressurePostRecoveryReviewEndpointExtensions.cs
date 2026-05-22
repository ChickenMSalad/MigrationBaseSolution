namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure post-recovery review endpoints.
/// This endpoint is intentionally metadata-only and compile-safe: it does not introduce new services, repositories, or DTO dependencies.
/// </summary>
public static class OperationalQueuePressurePostRecoveryReviewEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressurePostRecoveryReviewApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/post-recovery-review",
                (string? severity) =>
                {
                    var reviewSections = BuildReviewSections();

                    if (!string.IsNullOrWhiteSpace(severity))
                    {
                        reviewSections = reviewSections
                            .Where(x => string.Equals(x.SeverityApplicability, severity, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(x.SeverityApplicability, "All", StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                    }

                    return Results.Ok(new
                    {
                        postRecoveryReview = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            filters = new
                            {
                                severity
                            },
                            endpoint = "/api/operational/queue-pressure/post-recovery-review",
                            relatedEndpoints = new[]
                            {
                                "/api/operational/queue-pressure/dashboard",
                                "/api/operational/queue-pressure/trend",
                                "/api/operational/queue-pressure/action-plan",
                                "/api/operational/queue-pressure/recommendation-catalog",
                                "/api/operational/queue-pressure/operator-checklist",
                                "/api/operational/queue-pressure/escalation-guide",
                                "/api/operational/queue-pressure/incident-summary",
                                "/api/operational/queue-pressure/runbook",
                                "/api/operational/queue-pressure/recovery-workflow"
                            },
                            totalSectionCount = reviewSections.Length,
                            reviewSections
                        }
                    });
                })
            .WithName("GetOperationalQueuePressurePostRecoveryReview")
            .WithTags("Operational Store")
            .WithSummary("Gets the queue pressure post-recovery review checklist.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/post-recovery-review/readiness",
                () => Results.Ok(new
                {
                    readiness = new
                    {
                        generatedAtUtc = DateTimeOffset.UtcNow,
                        isAvailable = true,
                        sectionCount = BuildReviewSections().Length,
                        endpoint = "/api/operational/queue-pressure/post-recovery-review"
                    }
                }))
            .WithName("GetOperationalQueuePressurePostRecoveryReviewReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure post-recovery review readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static QueuePressurePostRecoveryReviewSection[] BuildReviewSections()
    {
        return new[]
        {
            new QueuePressurePostRecoveryReviewSection(
                Section: "Evidence",
                SeverityApplicability: "All",
                Objective: "Capture the operational evidence needed to understand the pressure event.",
                ReviewPrompts: new[]
                {
                    "Was the incident summary captured before mitigation changed the pressure state?",
                    "Were dashboard and trend outputs captured after recovery action completed?",
                    "Were route, smoke, and consistency validations run after the API restart?"
                },
                EvidenceEndpoint: "/api/operational/queue-pressure/incident-summary"),
            new QueuePressurePostRecoveryReviewSection(
                Section: "Mitigation",
                SeverityApplicability: "All",
                Objective: "Confirm the chosen mitigation matched the observed pressure severity.",
                ReviewPrompts: new[]
                {
                    "Which action plan item was used?",
                    "Did the operator checklist require escalation or local action only?",
                    "Was migration activity resumed gradually after pressure stabilized?"
                },
                EvidenceEndpoint: "/api/operational/queue-pressure/action-plan"),
            new QueuePressurePostRecoveryReviewSection(
                Section: "Escalation",
                SeverityApplicability: "Elevated",
                Objective: "Document escalation decisions when pressure exceeded normal operator handling.",
                ReviewPrompts: new[]
                {
                    "Was escalation triggered by severity, telemetry uncertainty, or repeated pressure?",
                    "Was the escalation guide followed before recovery workflow completion?",
                    "Were owners notified with the endpoint evidence needed to act?"
                },
                EvidenceEndpoint: "/api/operational/queue-pressure/escalation-guide"),
            new QueuePressurePostRecoveryReviewSection(
                Section: "Prevention",
                SeverityApplicability: "All",
                Objective: "Identify follow-up work that reduces repeat queue pressure incidents.",
                ReviewPrompts: new[]
                {
                    "Should migration batch sizing or dispatcher throttling be adjusted?",
                    "Did any recommendation catalog item become a permanent runbook change?",
                    "Is additional trend monitoring needed before the next high-volume run?"
                },
                EvidenceEndpoint: "/api/operational/queue-pressure/recommendation-catalog")
        };
    }

    private sealed record QueuePressurePostRecoveryReviewSection(
        string Section,
        string SeverityApplicability,
        string Objective,
        string[] ReviewPrompts,
        string EvidenceEndpoint);
}
