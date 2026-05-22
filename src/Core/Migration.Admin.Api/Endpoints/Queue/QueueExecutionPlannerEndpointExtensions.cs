using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueueExecutionPlannerEndpointExtensions
{
    public static RouteGroupBuilder MapQueueExecutionPlannerEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapPost("/cloud/queue/execution-plan/probe", (
                IQueueExecutionPlanner planner,
                HttpContext httpContext,
                IConfiguration configuration) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var envelope = QueueMessageEnvelopeFactory.CreateMigrationRunEnvelope(
                    workspaceId,
                    "sample-project",
                    "sample-run",
                    QueueMessageTypes.MigrationRunExecute);

                var plan = planner.Plan(envelope);

                return Results.Ok(new
                {
                    envelope,
                    plan
                });
            })
            .WithName("ProbeQueueExecutionPlan")
            .WithTags("Cloud")
            .WithSummary("Creates a sample queue execution plan from an envelope.");

        api.MapGet("/cloud/queue/execution-plan/message-types", () =>
            Results.Ok(new
            {
                supportedMessageTypes = new[]
                {
                    QueueMessageTypes.MigrationRunExecute,
                    QueueMessageTypes.MigrationRunCancel,
                    QueueMessageTypes.MigrationRunResume
                }
            }))
            .WithName("GetQueueExecutionMessageTypes")
            .WithTags("Cloud")
            .WithSummary("Gets queue message types recognized by the execution planner.");

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
