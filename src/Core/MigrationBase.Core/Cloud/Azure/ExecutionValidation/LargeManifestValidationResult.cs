using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public sealed record LargeManifestValidationResult
{
    public string ProfileName { get; init; } = string.Empty;

    public DateTimeOffset EvaluatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyCollection<LargeManifestValidationCheckResult> Checks { get; init; } = Array.Empty<LargeManifestValidationCheckResult>();

    public bool IsProductionReady => Checks.All(check => check.Passed || !check.BlocksProductionReadiness);
}
