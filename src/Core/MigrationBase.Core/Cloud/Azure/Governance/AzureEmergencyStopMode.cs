namespace MigrationBase.Core.Cloud.Azure.Governance;

public enum AzureEmergencyStopMode
{
    None = 0,
    SoftStop = 1,
    DrainOnly = 2,
    HardStop = 3
}
