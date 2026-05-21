namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalRunHealthActionPlanItem
{
    public int Sequence { get; init; }

    public string ActionKey { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Priority { get; init; } = string.Empty;

    public string OwnerHint { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string ValidationHint { get; init; } = string.Empty;

    public string SourceRecommendationKey { get; init; } = string.Empty;
}
