namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineCatalogResponse
{
    public Guid RunId { get; init; }

    public IReadOnlyCollection<string> EventTypes { get; init; } =
        Array.Empty<string>();

    public IReadOnlyCollection<string> Sources { get; init; } =
        Array.Empty<string>();

    public int EventTypeCount { get; init; }

    public int SourceCount { get; init; }
}
