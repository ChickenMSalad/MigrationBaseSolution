using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndEvidenceReport
{
    public required string ReportId { get; init; }

    public required string ScenarioId { get; init; }

    public AzureEndToEndEvidenceReportStatus Status { get; init; } =
        AzureEndToEndEvidenceReportStatus.Incomplete;

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureEndToEndEvidenceEntry> Evidence { get; init; } =
        new List<AzureEndToEndEvidenceEntry>();

    public IReadOnlyList<AzureEndToEndValidationIssue> Issues { get; init; } =
        new List<AzureEndToEndValidationIssue>();
}
