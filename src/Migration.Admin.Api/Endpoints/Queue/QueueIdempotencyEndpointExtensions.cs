using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueueIdempotencyEndpointExtensions
{
    public static RouteGroupBuilder MapQueueIdempotencyEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue/idempotency", (
                HttpContext httpContext,
                IConfiguration configuration) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var projectId = FirstNonEmpty(
                    httpContext.Request.Query["projectId"].FirstOrDefault(),
                    "sample-project");

                var runId = FirstNonEmpty(
                    httpContext.Request.Query["runId"].FirstOrDefault(),
                    "sample-run");

                var messageType = FirstNonEmpty(
                    httpContext.Request.Query["messageType"].FirstOrDefault(),
                    "migration.run.execute");

                return Results.Ok(new
                {
                    workspaceId,
                    projectId,
                    runId,
                    messageType,
                    idempotencyKey = QueueIdempotencyKeyBuilder.Build(workspaceId, projectId, runId, messageType),
                    hashedIdempotencyKey = QueueIdempotencyKeyBuilder.BuildHashed(workspaceId, projectId, runId, messageType),
                    leaseResource = QueueIdempotencyKeyBuilder.LeaseResourceForRun(workspaceId, projectId, runId)
                });
            })
            .WithName("GetQueueIdempotencyPlan")
            .WithTags("Cloud")
            .WithSummary("Gets deterministic queue idempotency and lease resource values.");

        api.MapPost("/cloud/queue/envelope/serialize", () =>
            {
                var envelope = QueueMessageEnvelopeFactory.CreateMigrationRunEnvelope(
                    workspaceId: "default",
                    projectId: "sample-project",
                    runId: "sample-run",
                    messageType: "migration.run.execute");

                var json = QueueMessageSerialization.ToJson(envelope);
                var base64 = QueueMessageSerialization.ToBase64Json(envelope);
                var roundTrip = QueueMessageSerialization.FromBase64Json(base64);

                return Results.Ok(new
                {
                    envelope,
                    json,
                    base64,
                    roundTrip,
                    roundTripMatches = envelope.IdempotencyKey == roundTrip.IdempotencyKey
                });
            })
            .WithName("SerializeQueueEnvelope")
            .WithTags("Cloud")
            .WithSummary("Serializes and round-trips a sample queue envelope.");

        return api;
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
