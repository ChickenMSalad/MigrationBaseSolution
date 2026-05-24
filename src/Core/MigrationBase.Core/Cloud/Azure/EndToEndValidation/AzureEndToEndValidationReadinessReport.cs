using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationReadinessReport
{
    public AzureEndToEndValidationReadinessStatus Status { get; init; } =
        AzureEndToEndValidationReadinessStatus.Ready;

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureEndToEndValidationReadinessIssue> Issues { get; init; } =
        new List<AzureEndToEndValidationReadinessIssue>();

    public bool IsReady => Status == AzureEndToEndValidationReadinessStatus.Ready;
}
