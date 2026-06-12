namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorStatusResponse
{
    public bool Enabled { get; init; }

    public bool MirrorServiceRegistered { get; init; }
}


