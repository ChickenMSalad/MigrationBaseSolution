using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using Migration.Orchestration.Abstractions;
using Migration.Orchestration.Options;

namespace Migration.Orchestration.State;

public sealed class AzureTableMigrationExecutionStateStore : IMigrationExecutionStateStore, IMigrationExecutionStateMaintenance
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly TableClient _table;

    public AzureTableMigrationExecutionStateStore(IOptions<MigrationExecutionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var tableOptions = options.Value.AzureTableState;
        if (string.IsNullOrWhiteSpace(tableOptions.ConnectionString))
        {
            throw new InvalidOperationException(
                "MigrationExecution:AzureTableState:ConnectionString is required when MigrationExecution:StateStore is AzureTable.");
        }

        _table = new TableClient(tableOptions.ConnectionString, tableOptions.TableName);

        if (tableOptions.CreateTableIfNotExists)
        {
            _table.CreateIfNotExists();
        }
    }

    public Task StartRunAsync(MigrationRunRecord run, CancellationToken cancellationToken = default) => SaveRunAsync(run, cancellationToken);

    public Task CompleteRunAsync(MigrationRunRecord run, CancellationToken cancellationToken = default) => SaveRunAsync(run, cancellationToken);

    public async Task SaveWorkItemAsync(MigrationWorkItemState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        var entity = AzureTableMigrationWorkItemEntity.FromState(state);
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MigrationWorkItemState?> GetWorkItemAsync(string jobName, string workItemId, CancellationToken cancellationToken = default)
    {
        var partitionKey = SafeKey(jobName);
        var rowKey = SafeKey(workItemId);

        try
        {
            var response = await _table.GetEntityAsync<AzureTableMigrationWorkItemEntity>(partitionKey, rowKey, cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Value.ToState();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<MigrationWorkItemState>> ListWorkItemsAsync(string jobName, CancellationToken cancellationToken = default)
    {
        var states = new List<MigrationWorkItemState>();
        var prefix = SafeKey(jobName);

        await foreach (var entity in _table.QueryAsync<AzureTableMigrationWorkItemEntity>(
                           x => x.Kind == AzureTableMigrationEntityKinds.WorkItem,
                           cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (entity.PartitionKey.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                entity.PartitionKey.StartsWith(prefix + "::", StringComparison.OrdinalIgnoreCase))
            {
                states.Add(entity.ToState());
            }
        }

        return states
            .OrderBy(x => x.JobName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.WorkItemId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task ResetJobAsync(string jobName, CancellationToken cancellationToken = default)
    {
        var prefix = SafeKey(jobName);
        var toDelete = new List<(string PartitionKey, string RowKey)>();

        await foreach (var entity in _table.QueryAsync<TableEntity>(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (entity.PartitionKey.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                entity.PartitionKey.StartsWith(prefix + "::", StringComparison.OrdinalIgnoreCase))
            {
                toDelete.Add((entity.PartitionKey, entity.RowKey));
            }
        }

        foreach (var item in toDelete)
        {
            await _table.DeleteEntityAsync(item.PartitionKey, item.RowKey, ETag.All, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SaveRunAsync(MigrationRunRecord run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        var entity = AzureTableMigrationRunEntity.FromRun(run);
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
    }

    private static string SafeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        return value
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace('#', '_')
            .Replace('?', '_')
            .Trim();
    }

    private static string? SerializeProperties(Dictionary<string, string?> properties) =>
        properties.Count == 0 ? null : JsonSerializer.Serialize(properties, JsonOptions);

    private static Dictionary<string, string?> DeserializeProperties(string? propertiesJson)
    {
        if (string.IsNullOrWhiteSpace(propertiesJson))
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        return JsonSerializer.Deserialize<Dictionary<string, string?>>(propertiesJson, JsonOptions)
               ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    private static class AzureTableMigrationEntityKinds
    {
        public const string Run = "Run";
        public const string WorkItem = "WorkItem";
    }

    private sealed class AzureTableMigrationRunEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string Kind { get; set; } = AzureTableMigrationEntityKinds.Run;
        public string RunId { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public bool DryRun { get; set; }
        public string ExecutionMode { get; set; } = string.Empty;
        public DateTimeOffset StartedUtc { get; set; }
        public DateTimeOffset? CompletedUtc { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalWorkItems { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public int ValidationFailed { get; set; }

        public static AzureTableMigrationRunEntity FromRun(MigrationRunRecord run)
        {
            return new AzureTableMigrationRunEntity
            {
                PartitionKey = SafeKey(run.JobName),
                RowKey = SafeKey("run::" + run.RunId),
                RunId = run.RunId,
                JobName = run.JobName,
                SourceType = run.SourceType,
                TargetType = run.TargetType,
                DryRun = run.DryRun,
                ExecutionMode = run.DryRun ? "DryRun" : "Live",
                StartedUtc = run.StartedUtc,
                CompletedUtc = run.CompletedUtc,
                Status = run.Status,
                TotalWorkItems = run.TotalWorkItems,
                Succeeded = run.Succeeded,
                Failed = run.Failed,
                Skipped = run.Skipped,
                ValidationFailed = run.ValidationFailed
            };
        }
    }

    private sealed class AzureTableMigrationWorkItemEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string Kind { get; set; } = AzureTableMigrationEntityKinds.WorkItem;
        public string RunId { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string WorkItemId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool DryRun { get; set; }
        public string ExecutionMode { get; set; } = string.Empty;
        public DateTimeOffset? StartedUtc { get; set; }
        public DateTimeOffset? CompletedUtc { get; set; }
        public DateTimeOffset UpdatedUtc { get; set; }
        public string? SourceAssetId { get; set; }
        public string? TargetAssetId { get; set; }
        public string? Message { get; set; }
        public string? LastError { get; set; }
        public string? Checksum { get; set; }
        public int AttemptCount { get; set; }
        public string? PropertiesJson { get; set; }

        public static AzureTableMigrationWorkItemEntity FromState(MigrationWorkItemState state)
        {
            return new AzureTableMigrationWorkItemEntity
            {
                PartitionKey = SafeKey(state.JobName),
                RowKey = SafeKey(state.WorkItemId),
                RunId = state.RunId,
                JobName = state.JobName,
                WorkItemId = state.WorkItemId,
                Status = state.Status,
                DryRun = state.DryRun,
                ExecutionMode = state.DryRun ? "DryRun" : "Live",
                StartedUtc = state.StartedUtc,
                CompletedUtc = state.CompletedUtc,
                UpdatedUtc = state.UpdatedUtc,
                SourceAssetId = state.SourceAssetId,
                TargetAssetId = state.TargetAssetId,
                Message = state.Message,
                LastError = state.LastError,
                Checksum = state.Checksum,
                AttemptCount = state.AttemptCount,
                PropertiesJson = SerializeProperties(state.Properties)
            };
        }

        public MigrationWorkItemState ToState()
        {
            return new MigrationWorkItemState
            {
                RunId = RunId,
                JobName = JobName,
                WorkItemId = WorkItemId,
                Status = Status,
                DryRun = DryRun,
                StartedUtc = StartedUtc,
                CompletedUtc = CompletedUtc,
                UpdatedUtc = UpdatedUtc,
                SourceAssetId = SourceAssetId,
                TargetAssetId = TargetAssetId,
                Message = Message,
                LastError = LastError,
                Checksum = Checksum,
                AttemptCount = AttemptCount,
                Properties = DeserializeProperties(PropertiesJson)
            };
        }
    }
}
