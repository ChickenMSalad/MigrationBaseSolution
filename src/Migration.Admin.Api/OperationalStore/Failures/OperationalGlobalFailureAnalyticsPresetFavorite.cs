namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureAnalyticsPresetFavorite
{
    public string FavoriteKey { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyCollection<string> PresetKeys { get; init; } =
        Array.Empty<string>();
}
