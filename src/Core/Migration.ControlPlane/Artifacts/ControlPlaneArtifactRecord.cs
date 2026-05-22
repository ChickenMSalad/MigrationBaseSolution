namespace Migration.ControlPlane.Artifacts;

public sealed class ControlPlaneArtifactRecord
{
    public string ArtifactId { get; init; } = string.Empty;
    public ArtifactKind Kind { get; init; } = ArtifactKind.Unknown;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public long Length { get; init; }
    public string RelativePath { get; init; } = string.Empty;
    public string AbsolutePath { get; init; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public string? ProjectId { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
