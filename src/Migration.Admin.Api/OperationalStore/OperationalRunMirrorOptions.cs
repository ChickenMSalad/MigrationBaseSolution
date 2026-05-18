namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunMirrorOptions
{
    public const string SectionName = "OperationalRunMirror";

    public bool Enabled { get; init; }
}
