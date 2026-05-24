using System;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndDryRunStepResult
{
    public required string StepId { get; init; }

    public required string Name { get; init; }

    public AzureEndToEndDryRunStepStatus Status { get; init; } =
        AzureEndToEndDryRunStepStatus.NotRun;

    public string? Message { get; init; }

    public DateTimeOffset CompletedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
