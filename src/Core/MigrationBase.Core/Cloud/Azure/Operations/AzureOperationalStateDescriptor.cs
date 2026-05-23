namespace MigrationBase.Core.Cloud.Azure.Operations;

/// <summary>
/// Captures the current operational state for an Azure runtime boundary.
/// </summary>
public sealed class AzureOperationalStateDescriptor
{
    public string EnvironmentName { get; set; } = string.Empty;

    public string DeploymentRing { get; set; } = string.Empty;

    public string ScopeName { get; set; } = string.Empty;

    public string ScopeKind { get; set; } = string.Empty;

    public AzureOperationalStateKind StateKind { get; set; } = AzureOperationalStateKind.Unknown;

    public bool AllowsNewWork { get; set; }

    public bool AllowsReplay { get; set; }

    public bool AllowsOperatorActions { get; set; } = true;

    public string Summary { get; set; } = string.Empty;

    public DateTimeOffset CapturedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<AzureOperationalStateSignal> Signals { get; set; } = new();
}
