namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsPresetCatalogResponse
{
    public int Count { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyCollection<OperationalGlobalFailureAnalyticsPreset> Presets { get; init; } =
        Array.Empty<OperationalGlobalFailureAnalyticsPreset>();
}
