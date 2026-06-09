namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionReplayPolicyOverrideService
{
    Task<ExecutionReplayPolicyOverrideResult> OverrideAsync(
        OverrideExecutionReplayPolicyRequest request,
        CancellationToken cancellationToken);

    Task<ExecutionReplayPolicyOverrideRecord?> FindActiveOverrideAsync(
        Guid sourceExecutionSessionId,
        string scope,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecutionReplayPolicyOverrideRecord>> ReadHistoryAsync(
        Guid sourceExecutionSessionId,
        int take,
        CancellationToken cancellationToken);

    Task ConsumeAsync(
        Guid replayPolicyOverrideId,
        Guid replayExecutionSessionId,
        CancellationToken cancellationToken);
}


