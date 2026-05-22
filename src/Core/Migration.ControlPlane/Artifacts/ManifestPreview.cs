namespace Migration.ControlPlane.Artifacts;

public sealed class ManifestPreview
{
    public string ArtifactId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IReadOnlyDictionary<string, string>> SampleRows { get; init; } = Array.Empty<IReadOnlyDictionary<string, string>>();
}
