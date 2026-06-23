using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Migration.Admin.Api.Contracts;
using Migration.ControlPlane.Services;
using Migration.Orchestration.Abstractions;

namespace Migration.Admin.Api.Endpoints;

public static class TargetExecutionEvidenceEndpointExtensions
{
    private const int DefaultTake = 500;
    private const int MaxQueryTake = 5000;
    private const int MaxExportTake = 50000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RouteGroupBuilder MapTargetExecutionEvidenceEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/runs/{runId}/target-evidence", async (
                string runId,
                [FromQuery] string? status,
                [FromQuery] string? search,
                [FromQuery] int? skip,
                [FromQuery] int? take,
                IAdminProjectStore store,
                IMigrationExecutionStateMaintenance state,
                CancellationToken cancellationToken) =>
            {
                var run = await store.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
                if (run is null)
                {
                    return Results.NotFound();
                }

                var matchingStates = await LoadRunStatesAsync(run.RunId, run.JobName, state, cancellationToken).ConfigureAwait(false);
                var rows = matchingStates
                    .Select(ToEvidenceRow)
                    .Where(x => MatchesStatus(x, status))
                    .Where(x => MatchesSearch(x, search))
                    .OrderByDescending(x => x.UpdatedUtc)
                    .ThenBy(x => x.WorkItemId, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var safeSkip = Math.Max(skip ?? 0, 0);
                var safeTake = ClampTake(take, DefaultTake, MaxQueryTake);
                var pageRows = rows.Skip(safeSkip).Take(safeTake).ToList();

                var response = new TargetExecutionEvidenceResponse(
                    run.RunId,
                    run.JobName,
                    rows.Count,
                    rows.Count(IsSuccessRow),
                    rows.Count(IsFailedRow),
                    rows.Count(IsRetryRow),
                    safeSkip,
                    safeTake,
                    pageRows.Count,
                    pageRows);

                return Results.Ok(response);
            })
            .WithName("GetRunTargetExecutionEvidence")
            .WithTags("Runs")
            .WithSummary("Lists row-level target upsert evidence for a migration run.")
            .Produces<TargetExecutionEvidenceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        api.MapGet("/runs/{runId}/target-evidence/export", async (
                string runId,
                [FromQuery] string? status,
                [FromQuery] string? search,
                [FromQuery] int? take,
                IAdminProjectStore store,
                IMigrationExecutionStateMaintenance state,
                CancellationToken cancellationToken) =>
            {
                var run = await store.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
                if (run is null)
                {
                    return Results.NotFound();
                }

                var matchingStates = await LoadRunStatesAsync(run.RunId, run.JobName, state, cancellationToken).ConfigureAwait(false);
                var rows = matchingStates
                    .Select(ToEvidenceRow)
                    .Where(x => MatchesStatus(x, status))
                    .Where(x => MatchesSearch(x, search))
                    .OrderBy(x => x.OriginId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.WorkItemId, StringComparer.OrdinalIgnoreCase)
                    .Take(ClampTake(take, MaxExportTake, MaxExportTake))
                    .ToList();

                var fileKind = NormalizeExportKind(status);
                var fileName = SafeFileName($"{run.JobName}-{fileKind}-target-evidence.csv");
                var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(BuildCsv(rows))).ToArray();

                return Results.File(bytes, "text/csv; charset=utf-8", fileName);
            })
            .WithName("ExportRunTargetExecutionEvidence")
            .WithTags("Runs")
            .WithSummary("Exports row-level target upsert evidence for success/retry/restamping.")
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .Produces(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<IReadOnlyList<MigrationWorkItemState>> LoadRunStatesAsync(
        string runId,
        string jobName,
        IMigrationExecutionStateMaintenance state,
        CancellationToken cancellationToken)
    {
        var allStates = await state.ListWorkItemsAsync(jobName, cancellationToken).ConfigureAwait(false);
        return allStates
            .Where(x => string.Equals(x.RunId, runId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.JobName, jobName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static TargetExecutionEvidenceRow ToEvidenceRow(MigrationWorkItemState state)
    {
        var properties = state.Properties ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var stampedFields = ReadDictionary(properties, "StampedFieldsJson");
        var targetPayloadFields = ReadDictionary(properties, "TargetPayloadFieldsJson");
        var warnings = ReadStringList(properties, "WarningsJson");

        var originId = FirstNonBlank(
            Get(properties, "Origin_Id"),
            Get(properties, "OriginId"),
            Get(properties, "SourceAssetId"),
            state.SourceAssetId,
            state.WorkItemId);

        var targetAssetId = FirstNonBlank(
            Get(properties, "Id"),
            Get(properties, "TargetAssetId"),
            state.TargetAssetId);

        var message = FirstNonBlank(
            Get(properties, "TargetMessage"),
            state.Message);

        var error = FirstNonBlank(
            state.LastError,
            Get(properties, "LastError"),
            Get(properties, "Error"));

        return new TargetExecutionEvidenceRow(
            state.WorkItemId,
            state.Status,
            originId,
            targetAssetId,
            targetAssetId,
            FirstNonBlank(Get(properties, "FileName"), Get(properties, "SourcePath"), Get(properties, "ManifestPath")),
            message,
            error,
            state.StartedUtc,
            state.CompletedUtc,
            state.UpdatedUtc,
            stampedFields,
            targetPayloadFields,
            properties,
            warnings);
    }

    private static bool MatchesStatus(TargetExecutionEvidenceRow row, string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
        {
            return IsSuccessRow(row);
        }

        if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "retry", StringComparison.OrdinalIgnoreCase))
        {
            return IsFailedRow(row);
        }

        return string.Equals(row.Status, status, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesSearch(TargetExecutionEvidenceRow row, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var needle = search.Trim();
        return Contains(row.WorkItemId, needle) ||
               Contains(row.OriginId, needle) ||
               Contains(row.Id, needle) ||
               Contains(row.TargetAssetId, needle) ||
               Contains(row.FileName, needle) ||
               Contains(row.Message, needle) ||
               Contains(row.Error, needle) ||
               row.StampedFields.Any(x => Contains(x.Key, needle) || Contains(x.Value, needle)) ||
               row.TargetPayloadFields.Any(x => Contains(x.Key, needle) || Contains(x.Value, needle));
    }

    private static bool Contains(string? value, string needle) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private static bool IsSuccessRow(TargetExecutionEvidenceRow row) => IsSuccessStatus(row.Status);

    private static bool IsFailedRow(TargetExecutionEvidenceRow row) =>
        IsFailedStatus(row.Status) || !string.IsNullOrWhiteSpace(row.Error);

    private static bool IsRetryRow(TargetExecutionEvidenceRow row) => IsFailedRow(row);

    private static bool IsSuccessStatus(string? status) =>
        string.Equals(status, MigrationWorkItemStatuses.Succeeded, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, MigrationWorkItemStatuses.DryRunSucceeded, StringComparison.OrdinalIgnoreCase);

    private static bool IsFailedStatus(string? status) =>
        string.Equals(status, MigrationWorkItemStatuses.ValidationFailed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, MigrationWorkItemStatuses.SourceFailed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, MigrationWorkItemStatuses.TargetFailed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase);

    private static string BuildCsv(IReadOnlyList<TargetExecutionEvidenceRow> rows)
    {
        var stampedColumns = rows
            .SelectMany(row => row.StampedFields.Keys.Concat(row.TargetPayloadFields.Keys))
            .Where(column => !string.IsNullOrWhiteSpace(column))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(column => column, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var builder = new StringBuilder();
        AppendCsvLine(builder, new[]
        {
            "Status", "Origin_Id", "Id", "TargetAssetId", "WorkItemId", "FileName", "Message", "Error", "StartedUtc", "CompletedUtc", "UpdatedUtc"
        }.Concat(stampedColumns));

        foreach (var row in rows)
        {
            AppendCsvLine(builder, new[]
            {
                row.Status,
                row.OriginId,
                row.Id,
                row.TargetAssetId,
                row.WorkItemId,
                row.FileName,
                row.Message,
                row.Error,
                row.StartedUtc?.ToString("O"),
                row.CompletedUtc?.ToString("O"),
                row.UpdatedUtc.ToString("O")
            }.Concat(stampedColumns.Select(column => FieldValue(row, column))));
        }

        return builder.ToString();
    }

    private static string? FieldValue(TargetExecutionEvidenceRow row, string field)
    {
        if (row.StampedFields.TryGetValue(field, out var stampedValue))
        {
            return stampedValue;
        }

        return row.TargetPayloadFields.TryGetValue(field, out var targetValue) ? targetValue : null;
    }

    private static void AppendCsvLine(StringBuilder builder, IEnumerable<string?> values)
    {
        var first = true;
        foreach (var value in values)
        {
            if (!first)
            {
                builder.Append(',');
            }

            builder.Append(CsvValue(value));
            first = false;
        }

        builder.AppendLine();
    }

    private static string CsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.IndexOfAny(new[] { '"', ',', '\r', '\n' }) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static IReadOnlyDictionary<string, string?> ReadDictionary(IReadOnlyDictionary<string, string?> properties, string key)
    {
        var value = Get(properties, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(value, JsonOptions)
                   ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static IReadOnlyList<string> ReadStringList(IReadOnlyDictionary<string, string?> properties, string key)
    {
        var value = Get(properties, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(value, JsonOptions) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new[] { value };
        }
    }

    private static string? Get(IReadOnlyDictionary<string, string?> properties, string key) =>
        properties.TryGetValue(key, out var value) ? value : null;

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static int ClampTake(int? requested, int defaultTake, int maxTake)
    {
        var value = requested ?? defaultTake;
        return Math.Clamp(value, 1, maxTake);
    }

    private static string NormalizeExportKind(string? status)
    {
        if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
        {
            return "success";
        }

        if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "retry", StringComparison.OrdinalIgnoreCase))
        {
            return "retry";
        }

        return "all";
    }

    private static string SafeFileName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            builder.Append(char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-' ? ch : '-');
        }

        var text = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(text) ? "target-evidence.csv" : text;
    }
}
