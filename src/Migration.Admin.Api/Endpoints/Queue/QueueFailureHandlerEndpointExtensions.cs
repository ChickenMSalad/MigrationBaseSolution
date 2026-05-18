using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueueFailureHandlerEndpointExtensions
{
    public static RouteGroupBuilder MapQueueFailureHandlerEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapPost("/cloud/queue/failure-handler/probe", async (
                IConfiguration configuration,
                IQueueReceiveProvider receiveProvider,
                IQueueFailureHandler failureHandler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var request = BuildSampleRequest(workspaceId);
                var options = QueuePoisonHandlingPlanner.BuildOptions(configuration);
                var plan = QueuePoisonHandlingPlanner.BuildPlan(options, receiveProvider.Descriptor);

                var result = await failureHandler.HandleFailureAsync(
                    request,
                    plan,
                    cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    request,
                    plan,
                    result
                });
            })
            .WithName("ProbeQueueFailureHandler")
            .WithTags("Cloud")
            .WithSummary("Runs the queue failure handler against a synthetic failure.");

        return api;
    }

    private static QueueFailureArtifactRequest BuildSampleRequest(string workspaceId)
    {
        var runId = "sample-run";
        var projectId = "sample-project";
        var messageType = "migration.run.execute";
        var idempotencyKey = QueueIdempotencyKeyBuilder.Build(workspaceId, projectId, runId, messageType);

        return new QueueFailureArtifactRequest(
            WorkspaceId: workspaceId,
            ProjectId: projectId,
            RunId: runId,
            MessageType: messageType,
            IdempotencyKey: idempotencyKey,
            FailureReason: "sample-handler-failure",
            ExceptionType: "SampleException",
            ExceptionMessage: "This is a synthetic queue failure handler probe.",
            Attempt: 5,
            FailedUtc: DateTimeOffset.UtcNow);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
