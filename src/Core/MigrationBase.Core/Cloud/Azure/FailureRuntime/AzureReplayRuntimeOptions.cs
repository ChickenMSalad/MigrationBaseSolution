namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureReplayRuntimeOptions
{
    public const string SectionName = "AzureRuntime:ReplayRuntime";

    public bool Enabled { get; set; } = true;

    public bool RequireApproval { get; set; } = true;

    public bool AllowOperatorOverride { get; set; } = true;

    public bool BlockPoisonReplay { get; set; } = true;
}
