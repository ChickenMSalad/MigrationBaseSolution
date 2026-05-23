namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public enum AzureRuntimeCompositionValidationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

/// <summary>
/// Captures a runtime composition validation finding without binding to any particular logger or telemetry provider.
/// </summary>
public sealed class AzureRuntimeCompositionValidationIssue
{
    public AzureRuntimeCompositionValidationIssue()
    {
    }

    public AzureRuntimeCompositionValidationIssue(
        AzureRuntimeCompositionValidationSeverity severity,
        string code,
        string message,
        string? moduleKey = null)
    {
        Severity = severity;
        Code = code;
        Message = message;
        ModuleKey = moduleKey;
    }

    public AzureRuntimeCompositionValidationSeverity Severity { get; set; } = AzureRuntimeCompositionValidationSeverity.Info;

    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? ModuleKey { get; set; }
}
