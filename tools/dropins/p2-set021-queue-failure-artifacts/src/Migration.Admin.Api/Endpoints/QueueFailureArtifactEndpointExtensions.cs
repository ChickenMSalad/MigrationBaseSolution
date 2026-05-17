using System.Text;
using Migration.ControlPlane.Queues;
using Migration.ControlPlane.Storage;

namespace Migration.Admin.Api.Endpoints;

public static class QueueFailureArtifactEndpointExtensions
{
    public static RouteGroupBuilder MapQueueFailureArtifactEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue/failure-artifact/plan", (
                IConfiguration configuration,
                IQueueReceiveProvider receiveProvider,
                HttpContext httpContext) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var request = BuildSampleRequest(workspaceId);
                var options = QueuePoisonHandlingPlanner.BuildOptions(configuration);
                var poisonPlan = QueuePoisonHandlingPlanner.BuildPlan(options, receiveProvider.Descriptor);
                var descriptor = QueueFailureArtifactPlanner.BuildDescriptor(request, poisonPlan);

                return Results.Ok(new
                {
                    request,
                    poisonPlan,
                    descriptor
                });
            })
            .WithName("GetQueueFailureArtifactPlan")
            .WithTags("Cloud")
            .WithSummary("Gets the planned queue failure artifact descriptor.");

        api.MapPost("/cloud/queue/failure-artifact/probe", async (
                IConfiguration configuration,
                IQueueReceiveProvider receiveProvider,
                IArtifactStorageService artifactStorage,
                IArtifactManifestIndexService manifestIndex,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var request = BuildSampleRequest(workspaceId);
                var options = QueuePoisonHandlingPlanner.BuildOptions(configuration);
                var poisonPlan = QueuePoisonHandlingPlanner.BuildPlan(options, receiveProvider.Descriptor);
                var descriptor = QueueFailureArtifactPlanner.BuildDescriptor(request, poisonPlan);

                var payload = QueueFailureArtifactPlanner.ToJsonPayload(request);
                var storageRequest = QueueFailureArtifactPlanner.ToArtifactStorageRequest(descriptor);

                await using var content = new MemoryStream(Encoding.UTF8.GetBytes(payload));
                var artifact = await artifactStorage.WriteAsync(storageRequest, content, cancellationToken).ConfigureAwait(false);
                var index = await manifestIndex.AddAsync(artifact, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    descriptor,
                    artifact,
                    index
                });
            })
            .WithName("ProbeQueueFailureArtifact")
            .WithTags("Cloud")
            .WithSummary("Writes a sample queue failure artifact through artifact storage.");

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
            FailureReason: "sample-failure",
            ExceptionType: "SampleException",
            ExceptionMessage: "This is a synthetic queue failure artifact probe.",
            Attempt: 1,
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
