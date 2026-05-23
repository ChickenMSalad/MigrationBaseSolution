namespace MigrationBase.Core.Cloud.Azure.Workers.Poison;

/// <summary>
/// Options for conservative poison-work handling before provider-specific persistence is wired.
/// </summary>
public sealed class AzureWorkerPoisonWorkOptions
{
    public const string SectionName = "AzureRuntime:Workers:PoisonWork";

    public int MaxAttemptsBeforeDeadLetter { get; init; } = 5;
    public int AbandonDelaySeconds { get; init; } = 30;
    public bool RequireOperatorActionForDeadLetter { get; init; } = true;
    public bool AllowReplayForDeadLetteredWork { get; init; } = true;
}
