using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueueContractDiagnosticsEndpointExtensions
{
    public static RouteGroupBuilder MapQueueContractDiagnosticsEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue/provider", (IConfiguration configuration) =>
            {
                var provider = configuration["MigrationRunQueue:Provider"] ?? "unknown";

                var descriptor = new QueueProviderDescriptor(
                    ProviderKind: provider,
                    SupportsDeadLettering: provider.Equals("serviceBus", StringComparison.OrdinalIgnoreCase),
                    SupportsSessions: provider.Equals("serviceBus", StringComparison.OrdinalIgnoreCase),
                    SupportsScheduledMessages: provider.Equals("serviceBus", StringComparison.OrdinalIgnoreCase),
                    RecommendedProperties: QueueMessagePropertyNames.Recommended,
                    Warnings: provider.Equals("AzureQueue", StringComparison.OrdinalIgnoreCase)
                        ? ["Azure Queue does not support native dead lettering or sessions."]
                        : []);

                return Results.Ok(descriptor);
            })
            .WithName("GetQueueProviderDescriptor")
            .WithTags("Cloud")
            .WithSummary("Gets queue provider capabilities.");

        api.MapPost("/cloud/queue/envelope/probe", () =>
            {
                var envelope = QueueMessageEnvelopeFactory.CreateMigrationRunEnvelope(
                    workspaceId: "default",
                    projectId: "sample-project",
                    runId: Guid.NewGuid().ToString("N"),
                    messageType: "migration.run.execute");

                return Results.Ok(envelope);
            })
            .WithName("ProbeQueueEnvelope")
            .WithTags("Cloud")
            .WithSummary("Creates a sample queue envelope.");

        return api;
    }
}


