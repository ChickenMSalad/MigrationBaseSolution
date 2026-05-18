namespace Migration.Application.Models.OperationalStore.Statuses;

public static class MigrationFailureTypes
{
    public const string Unknown = "Unknown";
    public const string Validation = "Validation";
    public const string Manifest = "Manifest";
    public const string Mapping = "Mapping";
    public const string SourceConnector = "SourceConnector";
    public const string TargetConnector = "TargetConnector";
    public const string ArtifactStorage = "ArtifactStorage";
    public const string QueueDispatch = "QueueDispatch";
    public const string Timeout = "Timeout";
    public const string Cancellation = "Cancellation";
}
