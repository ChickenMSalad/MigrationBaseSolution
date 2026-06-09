namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorReadinessStatus
{
    public bool Ready { get; init; }

    public bool Enabled { get; init; }

    public bool MirrorServiceRegistered { get; init; }

    public bool OptionsValidatorRegistered { get; init; }

    public bool OperationalStoreRegistered { get; init; }

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}


