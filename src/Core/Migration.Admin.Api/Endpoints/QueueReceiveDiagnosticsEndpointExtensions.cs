using Migration.ControlPlane.Queues;

namespace Migration.Admin.Api.Endpoints;

public static class QueueReceiveDiagnosticsEndpointExtensions
{
    public static RouteGroupBuilder MapQueueReceiveDiagnosticsEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue/receive/provider", (
                IQueueReceiveProvider provider) =>
            Results.Ok(provider.Descriptor))
            .WithName("GetQueueReceiveProvider")
            .WithTags("Cloud")
            .WithSummary("Gets active queue receive provider diagnostics.");

        api.MapPost("/cloud/queue/receive/probe", async (
                IQueueReceiveProvider provider,
                CancellationToken cancellationToken) =>
            {
                var messages = await provider.ReceiveAsync(
                    maxMessages: 1,
                    visibilityTimeout: TimeSpan.FromSeconds(10),
                    cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    provider = provider.Descriptor,
                    messageCount = messages.Count,
                    messages
                });
            })
            .WithName("ProbeQueueReceive")
            .WithTags("Cloud")
            .WithSummary("Receives up to one message through the active queue receive provider.");

        return api;
    }
}
