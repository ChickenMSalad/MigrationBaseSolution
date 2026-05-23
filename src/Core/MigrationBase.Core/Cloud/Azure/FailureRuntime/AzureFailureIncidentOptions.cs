namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureIncidentOptions
{
    public const string SectionName = "AzureRuntime:FailureIncident";

    public bool Enabled { get; set; } = true;

    public bool RecordRetryDecisions { get; set; } = true;

    public bool RecordReplayDecisions { get; set; } = true;

    public bool UseInMemoryIncidentStore { get; set; } = true;
}
