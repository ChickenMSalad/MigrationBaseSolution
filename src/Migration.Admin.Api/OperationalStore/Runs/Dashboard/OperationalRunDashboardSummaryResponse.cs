namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunDashboardSummaryResponse
{
    public Guid RunId { get; init; }

    public OperationalRunStatusProjection Projection { get; init; } = default!;

    public OperationalRunControlStateResponse ControlState { get; init; } = default!;

    public OperationalRunCompletionReadinessResponse CompletionReadiness { get; init; } = default!;

    public OperationalRunFailureReadinessResponse FailureReadiness { get; init; } = default!;

    public DateTimeOffset GeneratedAt { get; init; }

    public IReadOnlyCollection<string> Messages { get; init; } =
        Array.Empty<string>();
}
