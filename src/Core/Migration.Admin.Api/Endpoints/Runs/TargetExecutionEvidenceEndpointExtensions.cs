using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Migration.Admin.Api.Contracts;
using Migration.ControlPlane.Services;
using Migration.Orchestration.Abstractions;

namespace Migration.Admin.Api.Endpoints;

public static class TargetExecutionEvidenceEndpointExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RouteGroupBuilder MapTargetExecutionEvidenceEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/runs/{runId}/target-evidence", async (
                string runId,
                [FromQuery] string? status,
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

                var allStates = await state.ListWorkItemsAsync(run.JobName, cancellationToken).ConfigureAwait(false);
                var matchingStates = allStates
                    .Where(x => string.Equals(x.RunId, run.RunId, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(x.JobName, run.JobName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var rows = matchingStates
                    .Select(ToEvidenceRow)
                    .Where(x => MatchesStatus(x, status))
                    .OrderByDescending(x => x.UpdatedUtc)
                    .ThenBy(x => x.WorkItemId, StringComparer.OrdinalIgnoreCase)
                    .Take(Math.Clamp(take ?? 500, 1, 5000))
                    .ToList();

                var response = new TargetExecutionEvidenceResponse(
                    run.RunId,
                    run.JobName,
                    matchingStates.Count,
                    matchingStates.Count(IsSuccessState),
                    matchingStates.Count(IsFailedState),
                    matchingStates.Count(IsRetryState),
                    rows);

                return Results.Ok(response);
            })
            .WithName("GetRunTargetExecutionEvidence")
            .WithTags("Runs")
            .WithSummary("Lists row-level target upsert evidence for a migration run.")
            .Produces<TargetExecutionEvidenceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return api;
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
            return IsSuccessStatus(row.Status);
        }

        if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "retry", StringComparison.OrdinalIgnoreCase))
        {
            return IsFailedStatus(row.Status) || !string.IsNullOrWhiteSpace(row.Error);
        }

        return string.Equals(row.Status, status, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSuccessState(MigrationWorkItemState state) => IsSuccessStatus(state.Status);
    private static bool IsFailedState(MigrationWorkItemState state) => IsFailedStatus(state.Status) || !string.IsNullOrWhiteSpace(state.LastError);
    private static bool IsRetryState(MigrationWorkItemState state) => IsFailedState(state);

    private static bool IsSuccessStatus(string? status) =>
        string.Equals(status, MigrationWorkItemStatuses.Succeeded, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, MigrationWorkItemStatuses.DryRunSucceeded, StringComparison.OrdinalIgnoreCase);

    private static bool IsFailedStatus(string? status) =>
        string.Equals(status, MigrationWorkItemStatuses.ValidationFailed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, MigrationWorkItemStatuses.SourceFailed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, MigrationWorkItemStatuses.TargetFailed, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase);

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
}
