namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed class AzureDrainRequestDescriptor
{
    public string Name { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public string TargetRole { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public TimeSpan? MaximumDrainDuration { get; init; }
    public bool StopNewAssignments { get; init; } = true;
    public bool WaitForActiveLeases { get; init; } = true;
}
