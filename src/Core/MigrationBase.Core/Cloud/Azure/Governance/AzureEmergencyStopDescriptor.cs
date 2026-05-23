namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed class AzureEmergencyStopDescriptor
{
    public string Name { get; init; } = string.Empty;
    public AzureEmergencyStopMode Mode { get; init; } = AzureEmergencyStopMode.None;
    public string Reason { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool BlocksNewWorkAdmission { get; init; } = true;
    public bool AllowsActiveWorkToDrain { get; init; } = true;
    public bool RequiresOperatorAcknowledgement { get; init; } = true;
}
