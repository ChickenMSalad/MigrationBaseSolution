using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationResult
{
    public required string ScenarioId { get; init; }

    public AzureEndToEndValidationStatus Status { get; init; } =
        AzureEndToEndValidationStatus.NotRun;

    public DateTimeOffset CompletedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureEndToEndValidationIssue> Issues { get; init; } =
        new List<AzureEndToEndValidationIssue>();

    public bool Passed =>
        Status is AzureEndToEndValidationStatus.Passed or
            AzureEndToEndValidationStatus.PassedWithWarnings;

    public bool HasWarnings => Issues.Any(issue => issue.IsWarning);

    public bool HasErrors => Issues.Any(issue => !issue.IsWarning);
}
