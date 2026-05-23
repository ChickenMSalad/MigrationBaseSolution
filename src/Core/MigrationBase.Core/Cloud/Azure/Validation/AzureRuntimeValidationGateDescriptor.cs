namespace MigrationBase.Core.Cloud.Azure.Validation;

public sealed class AzureRuntimeValidationGateDescriptor
{
    public string GateKey { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string AppliesToEnvironment { get; init; } = "all";

    public string AppliesToHostRole { get; init; } = "all";

    public AzureRuntimeValidationGateSeverity Severity { get; init; } = AzureRuntimeValidationGateSeverity.Blocking;

    public bool RequiredForDeployment { get; init; } = true;

    public string Description { get; init; } = string.Empty;

    public string RemediationHint { get; init; } = string.Empty;
}
