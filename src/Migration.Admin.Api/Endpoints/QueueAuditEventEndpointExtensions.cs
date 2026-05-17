using Migration.ControlPlane.Audit;
using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueueAuditEventEndpointExtensions
{
    public static RouteGroupBuilder MapQueueAuditEventEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapPost("/cloud/queue/audit/probe", async (
                IAuditEventWriter auditWriter,
                IQueueExecutionPlanner planner,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
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

                var dispatchAudit = QueueAuditEventFactory.DispatchAccepted(
                    envelope,
                    "diagnostic",
                    "migration-runs");

                var planAudit = QueueAuditEventFactory.MessagePlanned(envelope, plan);

                var dispatchResult = await auditWriter.WriteAsync(dispatchAudit, cancellationToken).ConfigureAwait(false);
                var planResult = await auditWriter.WriteAsync(planAudit, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    envelope,
                    plan,
                    auditResults = new[]
                    {
                        dispatchResult,
                        planResult
                    }
                });
            })
            .WithName("ProbeQueueAuditEvents")
            .WithTags("Cloud")
            .WithSummary("Writes synthetic queue audit events through the audit event writer.");

        api.MapGet("/cloud/queue/audit/event-names", () =>
            Results.Ok(new
            {
                category = AuditCategories.Queue,
                eventNames = new[]
                {
                    QueueAuditEventNames.DispatchAccepted,
                    QueueAuditEventNames.ReceivePolled,
                    QueueAuditEventNames.MessagePlanned,
                    QueueAuditEventNames.MessageFailed,
                    QueueAuditEventNames.FailureArtifactWritten,
                    QueueAuditEventNames.CoordinatorPolled
                }
            }))
            .WithName("GetQueueAuditEventNames")
            .WithTags("Cloud")
            .WithSummary("Gets queue audit event names.");

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
