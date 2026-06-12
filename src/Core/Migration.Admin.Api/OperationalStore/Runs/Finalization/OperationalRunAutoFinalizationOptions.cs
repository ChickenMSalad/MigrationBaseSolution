namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunAutoFinalizationOptions
{
    public const string SectionName = "OperationalRunAutoFinalization";

    public bool Enabled { get; init; }

    public int IntervalSeconds { get; init; } = 30;

    public int BatchSize { get; init; } = 100;
}


