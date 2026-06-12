namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineGlobalCatalogResponse
{
    public IReadOnlyCollection<string> EventTypes { get; init; } =
        Array.Empty<string>();

    public IReadOnlyCollection<string> Sources { get; init; } =
        Array.Empty<string>();

    public int EventTypeCount { get; init; }

    public int SourceCount { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }
}


