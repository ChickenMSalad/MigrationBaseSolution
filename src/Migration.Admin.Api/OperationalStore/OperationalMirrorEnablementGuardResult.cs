namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorEnablementGuardResult
{
    public bool CanMirror { get; init; }

    public bool MirrorEnabled { get; init; }

    public bool ReadinessPassed { get; init; }

    public bool SqlSchemaPassed { get; init; }

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}
