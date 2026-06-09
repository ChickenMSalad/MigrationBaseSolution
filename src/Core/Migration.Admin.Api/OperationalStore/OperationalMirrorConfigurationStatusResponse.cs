namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorConfigurationStatusResponse
{
    public bool Enabled { get; init; }

    public bool MirrorServiceRegistered { get; init; }

    public bool OptionsValidatorRegistered { get; init; }

    public string Mode =>
        Enabled
            ? "Enabled"
            : "Disabled";
}


