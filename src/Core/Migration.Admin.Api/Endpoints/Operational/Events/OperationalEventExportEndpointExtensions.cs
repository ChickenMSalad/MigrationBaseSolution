using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Endpoints.Operational.Events;

public static class OperationalEventExportEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalEventExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/events/export")
            .WithTags("Operational Event Export");

        group.MapGet("/csv", async (
            IOperationalEventQueryService queryService,
            string? severity,
            string? category,
            string? eventType,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            Guid? executionSessionId,
            Guid? migrationRunId,
            int? take,
            CancellationToken cancellationToken) =>
        {
            var request = new OperationalEventQueryRequest(
                Severity: Normalize(severity),
                Category: Normalize(category),
                EventType: Normalize(eventType),
                FromUtc: fromUtc,
                ToUtc: toUtc,
                ExecutionSessionId: executionSessionId,
                MigrationRunId: migrationRunId,
                Skip: 0,
                Take: Math.Clamp(take.GetValueOrDefault(250), 1, 1000));

            var events = await queryService.QueryAsync(request, cancellationToken);
            var csv = BuildCsv(events);
            var fileName = $"operational-events-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";

            return Results.File(
                Encoding.UTF8.GetBytes(csv),
                "text/csv",
                fileName);
        })
        .WithName("ExportOperationalEventsCsv");

        return endpoints;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string BuildCsv(IReadOnlyList<OperationalEventRecord> events)
    {
        var builder = new StringBuilder();

        builder.AppendLine("OperationalEventId,CreatedUtc,Severity,Category,EventType,Source,ExecutionSessionId,MigrationRunId,Message");

        foreach (var item in events)
        {
            builder.Append(Escape(item.OperationalEventId.ToString("D"))).Append(',');
            builder.Append(Escape(item.CreatedUtc.ToString("O"))).Append(',');
            builder.Append(Escape(item.Severity)).Append(',');
            builder.Append(Escape(item.Category)).Append(',');
            builder.Append(Escape(item.EventType)).Append(',');
            builder.Append(Escape(item.Source)).Append(',');
            builder.Append(Escape(item.ExecutionSessionId?.ToString("D"))).Append(',');
            builder.Append(Escape(item.MigrationRunId?.ToString("D"))).Append(',');
            builder.AppendLine(Escape(item.Message));
        }

        return builder.ToString();
    }

    private static string Escape(string? value)
    {
        var safeValue = value ?? string.Empty;

        if (safeValue.Contains('"') || safeValue.Contains(',') || safeValue.Contains('\n') || safeValue.Contains('\r'))
        {
            return $"\"{safeValue.Replace("\"", "\"\"")}\"";
        }

        return safeValue;
    }
}


