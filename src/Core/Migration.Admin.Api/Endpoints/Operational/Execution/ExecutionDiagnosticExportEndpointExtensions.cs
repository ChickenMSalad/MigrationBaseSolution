using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Endpoints.Operational.Execution;

public static class ExecutionDiagnosticExportEndpointExtensions
{
    public static IEndpointRouteBuilder MapExecutionDiagnosticExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/execution-diagnostics")
            .WithTags("Operational Execution Diagnostics");

        group.MapGet("/{executionSessionId:guid}/bundle", async (
            IExecutionDiagnosticExportService exportService,
            Guid executionSessionId,
            CancellationToken cancellationToken) =>
        {
            var bundle = await exportService.BuildBundleAsync(
                executionSessionId,
                cancellationToken);

            if (bundle.Session is null)
            {
                return Results.NotFound(new
                {
                    message = $"Execution session was not found: {executionSessionId}"
                });
            }

            return Results.Ok(bundle);
        })
        .WithName("GetExecutionDiagnosticBundle");

        group.MapGet("/{executionSessionId:guid}/bundle.json", async (
            IExecutionDiagnosticExportService exportService,
            Guid executionSessionId,
            CancellationToken cancellationToken) =>
        {
            var bundle = await exportService.BuildBundleAsync(
                executionSessionId,
                cancellationToken);

            if (bundle.Session is null)
            {
                return Results.NotFound(new
                {
                    message = $"Execution session was not found: {executionSessionId}"
                });
            }

            var json = JsonSerializer.Serialize(
                bundle,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            var fileName = $"execution-session-{executionSessionId:D}-diagnostics-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";

            return Results.File(
                Encoding.UTF8.GetBytes(json),
                "application/json",
                fileName);
        })
        .WithName("DownloadExecutionDiagnosticBundleJson");

        return endpoints;
    }
}
