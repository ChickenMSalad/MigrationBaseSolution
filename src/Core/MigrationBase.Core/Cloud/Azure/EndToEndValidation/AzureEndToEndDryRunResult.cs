using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndDryRunResult
{
    public required string ScenarioId { get; init; }

    public DateTimeOffset CompletedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureEndToEndDryRunStepResult> Steps { get; init; } =
        new List<AzureEndToEndDryRunStepResult>();

    public bool Passed => Steps.All(step => step.Status != AzureEndToEndDryRunStepStatus.Failed);
}
