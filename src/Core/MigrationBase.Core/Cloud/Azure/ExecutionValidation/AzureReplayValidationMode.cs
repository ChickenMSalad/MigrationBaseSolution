namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public enum AzureReplayValidationMode
{
    DryRun = 0,
    ControlledReplay = 1,
    ApprovedReplay = 2,
    EmergencyReplay = 3
}
