namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsPresetDashboardResponse
{
    public OperationalGlobalFailureAnalyticsPresetCatalogResponse Catalog { get; init; } = default!;

    public OperationalGlobalFailureAnalyticsPresetResponse SelectedPreset { get; init; } = default!;

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}


