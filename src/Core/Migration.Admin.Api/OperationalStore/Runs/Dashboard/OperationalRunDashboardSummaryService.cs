namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunDashboardSummaryService
    : IOperationalRunDashboardSummaryService
{
    private readonly IOperationalRunStatusProjectionService _projectionService;
    private readonly IOperationalRunControlService _controlService;
    private readonly IOperationalRunCompletionFinalizationService _completionService;
    private readonly IOperationalRunFailureFinalizationService _failureService;

    public OperationalRunDashboardSummaryService(
        IOperationalRunStatusProjectionService projectionService,
        IOperationalRunControlService controlService,
        IOperationalRunCompletionFinalizationService completionService,
        IOperationalRunFailureFinalizationService failureService)
    {
        _projectionService = projectionService;
        _controlService = controlService;
        _completionService = completionService;
        _failureService = failureService;
    }

    public async Task<OperationalRunDashboardSummaryResponse?> GetSummaryAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            throw new ArgumentException("RunId is required.", nameof(runId));
        }

        var projection = await _projectionService.GetAsync(
            runId,
            cancellationToken);

        if (projection is null)
        {
            return null;
        }

        var controlState = await _controlService.GetControlStateAsync(
            runId,
            cancellationToken);

        var completionReadiness = await _completionService.GetReadinessAsync(
            runId,
            cancellationToken);

        var failureReadiness = await _failureService.GetReadinessAsync(
            runId,
            cancellationToken);

        return new OperationalRunDashboardSummaryResponse
        {
            RunId = runId,
            Projection = projection,
            ControlState = controlState,
            CompletionReadiness = completionReadiness,
            FailureReadiness = failureReadiness,
            GeneratedAt = DateTimeOffset.UtcNow,
            Messages = BuildMessages(
                projection,
                controlState,
                completionReadiness,
                failureReadiness)
        };
    }

    private static IReadOnlyCollection<string> BuildMessages(
        dynamic projection,
        OperationalRunControlStateResponse controlState,
        OperationalRunCompletionReadinessResponse completionReadiness,
        OperationalRunFailureReadinessResponse failureReadiness)
    {
        var messages = new List<string>();

        try
        {
            messages.Add($"Run status is {projection.RunStatus}.");
            messages.Add($"Projection status is {projection.ProjectionStatus}.");
            messages.Add($"Completion is {projection.CompletionPercent}%.");

            if (projection.WorkItemFailedCount > 0)
            {
                messages.Add($"{projection.WorkItemFailedCount} work item(s) have failed.");
            }

            if (projection.WorkItemLockedCount > 0)
            {
                messages.Add($"{projection.WorkItemLockedCount} work item(s) are currently locked.");
            }
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            messages.Add("Run projection loaded.");
        }

        if (controlState.CancelRequested)
        {
            messages.Add("Run cancellation has been requested.");
        }

        if (controlState.Aborted)
        {
            messages.Add("Run has been aborted.");
        }

        if (completionReadiness.CanFinalize)
        {
            messages.Add("Run is ready for completion finalization.");
        }

        if (failureReadiness.CanFinalizeFailure)
        {
            messages.Add("Run is ready for failure finalization.");
        }

        return messages;
    }
}


