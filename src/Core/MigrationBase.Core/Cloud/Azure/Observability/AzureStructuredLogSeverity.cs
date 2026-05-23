namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Describes the normalized severity of an Azure operational log event.
/// </summary>
public enum AzureStructuredLogSeverity
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}
