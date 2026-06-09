namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsPresetResponse
{
    public OperationalGlobalFailureAnalyticsPreset Preset { get; init; } = default!;
    public OperationalGlobalFailureFilteredAnalyticsResponse Analytics { get; init; } = default!;
    public DateTimeOffset GeneratedAt { get; init; }
}


