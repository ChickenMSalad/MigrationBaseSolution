namespace MigrationBase.Core.Cloud.Azure.Operations;

/// <summary>
/// Describes the coarse operational state of an Azure-hosted migration runtime or host role.
/// </summary>
public enum AzureOperationalStateKind
{
    Unknown = 0,
    Provisioning = 1,
    Ready = 2,
    Running = 3,
    Draining = 4,
    Degraded = 5,
    Blocked = 6,
    Maintenance = 7,
    Stopped = 8
}
