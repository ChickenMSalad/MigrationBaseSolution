namespace Migration.ControlPlane.Artifacts;

public enum ArtifactKind
{
    Unknown = 0,
    Manifest = 1,
    Mapping = 2,
    Binary = 3,
    Report = 4,
    Taxonomy = 5,
    Other = 99
}
