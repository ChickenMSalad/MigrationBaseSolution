namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsPreset
{
    public string PresetKey { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public OperationalGlobalFailureQuery Query { get; init; } = new();
}
