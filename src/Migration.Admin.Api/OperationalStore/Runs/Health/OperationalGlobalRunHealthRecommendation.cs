namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthRecommendation
{
    public string RecommendationKey { get; init; } = string.Empty;

    public string Priority { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string SuggestedAction { get; init; } = string.Empty;

    public string RelatedSignal { get; init; } = string.Empty;
}
