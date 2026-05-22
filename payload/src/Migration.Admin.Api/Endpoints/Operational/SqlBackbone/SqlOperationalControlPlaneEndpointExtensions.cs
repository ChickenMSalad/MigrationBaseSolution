using System.Text.Json;
using Migration.Infrastructure.Sql.Records;
using Migration.Infrastructure.Sql.Stores;

namespace Migration.Admin.Api.Endpoints;

public static class SqlOperationalControlPlaneEndpointExtensions
{
    public static RouteGroupBuilder MapSqlOperationalControlPlaneEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        var group = api.MapGroup("/sql-operational")
            .WithTags("SQL Operational Control Plane");

        group.MapPost("/projects", CreateProjectAsync)
            .WithName("CreateSqlOperationalProject");

        group.MapPost("/runs", CreateRunAsync)
            .WithName("CreateSqlOperationalRun");

        group.MapGet("/runs/{runId:guid}", GetRunAsync)
            .WithName("GetSqlOperationalRun");

        group.MapPost("/runs/{runId:guid}/manifest-rows", UpsertManifestRowsAsync)
            .WithName("UpsertSqlOperationalManifestRows");

        group.MapPost("/runs/{runId:guid}/work-items", EnqueueWorkItemsAsync)
            .WithName("EnqueueSqlOperationalWorkItems");

        group.MapPost("/work-items/{workItemId:guid}/complete", CompleteWorkItemAsync)
            .WithName("CompleteSqlOperationalWorkItem");

        group.MapPost("/work-items/{workItemId:guid}/fail", FailWorkItemAsync)
            .WithName("FailSqlOperationalWorkItem");

        group.MapPost("/runs/{runId:guid}/checkpoints", SaveCheckpointAsync)
            .WithName("SaveSqlOperationalCheckpoint");

        group.MapPost("/runs/{runId:guid}/asset-mappings", UpsertAssetMappingAsync)
            .WithName("UpsertSqlOperationalAssetMapping");

        return api;
    }

    private static async Task<IResult> CreateProjectAsync(
        CreateSqlOperationalProjectRequest request,
        ISqlOperationalBackboneStore store,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var projectId = request.ProjectId ?? Guid.NewGuid();

        var record = new SqlMigrationProjectRecord(
            projectId,
            RequireNonEmpty(request.ProjectKey, nameof(request.ProjectKey)),
            RequireNonEmpty(request.DisplayName, nameof(request.DisplayName)),
            NormalizeStatus(request.Status, "Created"),
            request.CreatedAtUtc ?? now,
            now);

        await store.CreateProjectAsync(record, cancellationToken);

        return Results.Created($"/api/sql-operational/projects/{projectId}", new SqlOperationalProjectResponse(
            record.ProjectId,
            record.ProjectKey,
            record.DisplayName,
            record.Status,
            record.CreatedAtUtc,
            record.UpdatedAtUtc));
    }

    private static async Task<IResult> CreateRunAsync(
        CreateSqlOperationalRunRequest request,
        ISqlOperationalBackboneStore store,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var runId = request.RunId ?? Guid.NewGuid();

        var record = new SqlMigrationRunRecord(
            runId,
            request.ProjectId,
            RequireNonEmpty(request.RunKey, nameof(request.RunKey)),
            NormalizeStatus(request.Status, "Created"),
            request.CreatedAtUtc ?? now,
            now,
            request.StartedAtUtc,
            request.CompletedAtUtc);

        await store.CreateRunAsync(record, cancellationToken);

        return Results.Created($"/api/sql-operational/runs/{runId}", ToRunResponse(record));
    }

    private static async Task<IResult> GetRunAsync(
        Guid runId,
        ISqlOperationalBackboneStore store,
        CancellationToken cancellationToken)
    {
        var run = await store.GetRunAsync(runId, cancellationToken);

        return run is null
            ? Results.NotFound(new { message = $"SQL operational run '{runId}' was not found." })
            : Results.Ok(ToRunResponse(run));
    }

    private static async Task<IResult> UpsertManifestRowsAsync(
        Guid runId,
        UpsertSqlOperationalManifestRowsRequest request,
        ISqlOperationalBackboneStore store,
        CancellationToken cancellationToken)
    {
        if (request.Rows.Count == 0)
        {
            return Results.BadRequest(new { message = "At least one manifest row is required." });
        }

        var now = DateTimeOffset.UtcNow;
        var rows = request.Rows.Select(row => new SqlMigrationManifestRowRecord(
            row.ManifestRowId ?? Guid.NewGuid(),
            runId,
            row.RowNumber,
            RequireNonEmpty(row.SourceIdentifier, nameof(row.SourceIdentifier)),
            row.SourceUri,
            ToJson(row.Payload),
            NormalizeStatus(row.Status, "Pending"),
            row.CreatedAtUtc ?? now,
            now)).ToArray();

        await store.UpsertManifestRowsAsync(rows, cancellationToken);

        return Results.Accepted($"/api/sql-operational/runs/{runId}/manifest-rows", new
        {
            runId,
            upserted = rows.Length
        });
    }

    private static async Task<IResult> EnqueueWorkItemsAsync(
        Guid runId,
        EnqueueSqlOperationalWorkItemsRequest request,
        ISqlOperationalBackboneStore store,
        CancellationToken cancellationToken)
    {
        if (request.WorkItems.Count == 0)
        {
            return Results.BadRequest(new { message = "At least one work item is required." });
        }

        var now = DateTimeOffset.UtcNow;
        var workItems = request.WorkItems.Select(item => new SqlMigrationWorkItemRecord(
            item.WorkItemId ?? Guid.NewGuid(),
            runId,
            item.ManifestRowId,
            RequireNonEmpty(item.WorkItemType, nameof(item.WorkItemType)),
            NormalizeStatus(item.Status, "Pending"),
            item.AttemptCount ?? 0,
            item.AvailableAtUtc,
            item.LeasedUntilUtc,
            item.LeaseOwner,
            ToJson(item.Payload),
            item.CreatedAtUtc ?? now,
            now)).ToArray();

        await store.EnqueueWorkItemsAsync(workItems, cancellationToken);

        return Results.Accepted($"/api/sql-operational/runs/{runId}/work-items", new
        {
            runId,
            enqueued = workItems.Length
        });
    }

    private static async Task<IResult> CompleteWorkItemAsync(
        Guid workItemId,
        ISqlOperationalBackboneStore store,
        CancellationToken cancellationToken)
    {
        await store.CompleteWorkItemAsync(workItemId, cancellationToken);
        return Results.Accepted($"/api/sql-operational/work-items/{workItemId}", new { workItemId, status = "Completed" });
    }

    private static async Task<IResult> FailWorkItemAsync(
        Guid workItemId,
        FailSqlOperationalWorkItemRequest request,
        ISqlOperationalBackboneStore store,
        CancellationToken cancellationToken)
    {
        var failure = new SqlMigrationFailureRecord(
            request.FailureId ?? Guid.NewGuid(),
            request.RunId,
            workItemId,
            request.ManifestRowId,
            RequireNonEmpty(request.FailureType, nameof(request.FailureType)),
            RequireNonEmpty(request.FailureCode, nameof(request.FailureCode)),
            RequireNonEmpty(request.Message, nameof(request.Message)),
            ToJson(request.Details),
            request.CreatedAtUtc ?? DateTimeOffset.UtcNow);

        await store.FailWorkItemAsync(workItemId, failure, cancellationToken);

        return Results.Accepted($"/api/sql-operational/work-items/{workItemId}", new
        {
            workItemId,
            failureId = failure.FailureId,
            status = "Failed"
        });
    }

    private static async Task<IResult> SaveCheckpointAsync(
        Guid runId,
        SaveSqlOperationalCheckpointRequest request,
        ISqlOperationalBackboneStore store,
        CancellationToken cancellationToken)
    {
        var checkpoint = new SqlMigrationCheckpointRecord(
            request.CheckpointId ?? Guid.NewGuid(),
            runId,
            RequireNonEmpty(request.CheckpointName, nameof(request.CheckpointName)),
            RequireNonEmpty(request.CheckpointValue, nameof(request.CheckpointValue)),
            ToJson(request.Payload),
            request.CreatedAtUtc ?? DateTimeOffset.UtcNow);

        await store.SaveCheckpointAsync(checkpoint, cancellationToken);

        return Results.Accepted($"/api/sql-operational/runs/{runId}/checkpoints", new
        {
            runId,
            checkpointId = checkpoint.CheckpointId
        });
    }

    private static async Task<IResult> UpsertAssetMappingAsync(
        Guid runId,
        UpsertSqlOperationalAssetMappingRequest request,
        ISqlOperationalBackboneStore store,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var mapping = new SqlMigrationAssetMappingRecord(
            request.AssetMappingId ?? Guid.NewGuid(),
            runId,
            RequireNonEmpty(request.SourceSystem, nameof(request.SourceSystem)),
            RequireNonEmpty(request.SourceIdentifier, nameof(request.SourceIdentifier)),
            RequireNonEmpty(request.TargetSystem, nameof(request.TargetSystem)),
            RequireNonEmpty(request.TargetIdentifier, nameof(request.TargetIdentifier)),
            ToJson(request.Payload),
            request.CreatedAtUtc ?? now,
            now);

        await store.UpsertAssetMappingAsync(mapping, cancellationToken);

        return Results.Accepted($"/api/sql-operational/runs/{runId}/asset-mappings", new
        {
            runId,
            assetMappingId = mapping.AssetMappingId
        });
    }

    private static SqlOperationalRunResponse ToRunResponse(SqlMigrationRunRecord record)
    {
        return new SqlOperationalRunResponse(
            record.RunId,
            record.ProjectId,
            record.RunKey,
            record.Status,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.StartedAtUtc,
            record.CompletedAtUtc);
    }

    private static string NormalizeStatus(string? status, string fallback)
    {
        return string.IsNullOrWhiteSpace(status) ? fallback : status.Trim();
    }

    private static string RequireNonEmpty(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BadHttpRequestException($"'{parameterName}' is required.");
        }

        return value.Trim();
    }

    private static string ToJson(JsonElement? value)
    {
        if (value is null || value.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return "{}";
        }

        return value.Value.GetRawText();
    }
}

public sealed record CreateSqlOperationalProjectRequest(
    Guid? ProjectId,
    string ProjectKey,
    string DisplayName,
    string? Status,
    DateTimeOffset? CreatedAtUtc);

public sealed record SqlOperationalProjectResponse(
    Guid ProjectId,
    string ProjectKey,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateSqlOperationalRunRequest(
    Guid? RunId,
    Guid ProjectId,
    string RunKey,
    string? Status,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record SqlOperationalRunResponse(
    Guid RunId,
    Guid ProjectId,
    string RunKey,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record UpsertSqlOperationalManifestRowsRequest(
    IReadOnlyList<UpsertSqlOperationalManifestRowRequest> Rows);

public sealed record UpsertSqlOperationalManifestRowRequest(
    Guid? ManifestRowId,
    long RowNumber,
    string SourceIdentifier,
    string? SourceUri,
    JsonElement? Payload,
    string? Status,
    DateTimeOffset? CreatedAtUtc);

public sealed record EnqueueSqlOperationalWorkItemsRequest(
    IReadOnlyList<EnqueueSqlOperationalWorkItemRequest> WorkItems);

public sealed record EnqueueSqlOperationalWorkItemRequest(
    Guid? WorkItemId,
    Guid? ManifestRowId,
    string WorkItemType,
    string? Status,
    int? AttemptCount,
    DateTimeOffset? AvailableAtUtc,
    DateTimeOffset? LeasedUntilUtc,
    string? LeaseOwner,
    JsonElement? Payload,
    DateTimeOffset? CreatedAtUtc);

public sealed record FailSqlOperationalWorkItemRequest(
    Guid? FailureId,
    Guid RunId,
    Guid? ManifestRowId,
    string FailureType,
    string FailureCode,
    string Message,
    JsonElement? Details,
    DateTimeOffset? CreatedAtUtc);

public sealed record SaveSqlOperationalCheckpointRequest(
    Guid? CheckpointId,
    string CheckpointName,
    string CheckpointValue,
    JsonElement? Payload,
    DateTimeOffset? CreatedAtUtc);

public sealed record UpsertSqlOperationalAssetMappingRequest(
    Guid? AssetMappingId,
    string SourceSystem,
    string SourceIdentifier,
    string TargetSystem,
    string TargetIdentifier,
    JsonElement? Payload,
    DateTimeOffset? CreatedAtUtc);
