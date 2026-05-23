namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed record AzureMaintenanceModeDecision
{
    public bool IsAllowed { get; init; }

    public bool IsBlocked => !IsAllowed;

    public string DecisionCode { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public AzureMaintenanceModeDescriptor? MaintenanceMode { get; init; }

    public AzureOperationalFreezeDescriptor? OperationalFreeze { get; init; }

    public static AzureMaintenanceModeDecision Allow(string decisionCode, string message) =>
        new()
        {
            IsAllowed = true,
            DecisionCode = decisionCode,
            Message = message
        };

    public static AzureMaintenanceModeDecision Block(
        string decisionCode,
        string message,
        AzureMaintenanceModeDescriptor? maintenanceMode = null,
        AzureOperationalFreezeDescriptor? operationalFreeze = null) =>
        new()
        {
            IsAllowed = false,
            DecisionCode = decisionCode,
            Message = message,
            MaintenanceMode = maintenanceMode,
            OperationalFreeze = operationalFreeze
        };
}
