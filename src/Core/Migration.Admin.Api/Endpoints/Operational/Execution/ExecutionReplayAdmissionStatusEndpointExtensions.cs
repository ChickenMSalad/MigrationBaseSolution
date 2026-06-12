using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionReplayAdmissionStatusEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionReplayAdmissionStatusEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-replay")
            .WithTags("Operational Execution Replay");

        group.MapGet("/admission/background/status", (
            IOptionsMonitor<ExecutionReplayAdmissionBackgroundOptions> backgroundOptions,
            IOptionsMonitor<ExecutionReplayAdmissionOptions> admissionOptions) =>
        {
            var background = backgroundOptions.CurrentValue;
            var admission = admissionOptions.CurrentValue;

            return Results.Ok(new ExecutionReplayAdmissionBackgroundStatus(
                Enabled: background.Enabled,
                IntervalSeconds: Math.Clamp(background.IntervalSeconds, 15, 3600),
                Take: Math.Clamp(background.Take, 1, 250),
                AdmissionEnabled: admission.Enabled,
                MaxConcurrentReplays: Math.Clamp(admission.MaxConcurrentReplays, 0, 100),
                AllowedStartHourUtc: Math.Clamp(admission.AllowedStartHourUtc, 0, 23),
                AllowedEndHourUtc: Math.Clamp(admission.AllowedEndHourUtc, 1, 24),
                GeneratedUtc: DateTimeOffset.UtcNow));
        })
        .WithName("GetExecutionReplayAdmissionBackgroundStatus");

        return endpoints;
    }
}


