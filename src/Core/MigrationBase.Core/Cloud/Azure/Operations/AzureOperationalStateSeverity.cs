namespace MigrationBase.Core.Cloud.Azure.Operations;

/// <summary>
/// Severity used when reporting operational state findings.
/// </summary>
public enum AzureOperationalStateSeverity
{
    Informational = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}
