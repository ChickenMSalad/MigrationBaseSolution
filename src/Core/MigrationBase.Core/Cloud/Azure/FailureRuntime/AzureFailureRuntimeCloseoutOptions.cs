namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureRuntimeCloseoutOptions
{
    public const string SectionName = "AzureRuntime:FailureRuntimeCloseout";

    public bool Enabled { get; set; } = true;

    public bool RequireReadinessEvaluation { get; set; } = true;

    public bool RequireIncidentRecording { get; set; } = true;

    public bool RequireReplayAdmissionControl { get; set; } = true;
}
