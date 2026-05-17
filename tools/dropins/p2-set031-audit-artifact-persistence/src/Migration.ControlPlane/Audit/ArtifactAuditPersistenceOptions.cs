namespace Migration.ControlPlane.Audit;

public sealed class ArtifactAuditPersistenceOptions
{
    public const string SectionName = "Audit";

    public string ArtifactKind { get; init; } = "audit";

    public string ArtifactId { get; init; } = "events";

    public string FileNamePrefix { get; init; } = "audit-event";

    public int RecentQueryLimit { get; init; } = 100;
}
