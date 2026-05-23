namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Defines the severity of an observability closeout gate.
/// </summary>
public enum AzureObservabilityCloseoutGateSeverity
{
    Informational = 0,
    Warning = 1,
    Blocking = 2
}
