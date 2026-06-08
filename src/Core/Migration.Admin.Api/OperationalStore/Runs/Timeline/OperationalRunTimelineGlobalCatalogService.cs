using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunTimelineGlobalCatalogService
    : IOperationalRunTimelineGlobalCatalogService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public OperationalRunTimelineGlobalCatalogService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options;
    }

    public async Task<OperationalRunTimelineGlobalCatalogResponse> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var schema = GetSchemaName();

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var eventTypes = await ReadEventTypesAsync(
            connection,
            schema,
            cancellationToken);

        var sources = new[]
        {
            "MigrationRuns",
            "MigrationWorkItems",
            "MigrationCheckpoints",
            "MigrationFailures"
        };

        return new OperationalRunTimelineGlobalCatalogResponse
        {
            EventTypes = eventTypes,
            Sources = sources,
            EventTypeCount = eventTypes.Count,
            SourceCount = sources.Length,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    private static async Task<IReadOnlyCollection<string>> ReadEventTypesAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var eventTypes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RunCreated",
            "RunStarted",
            "RunCompleted",
            "RunFailed",
            "WorkItemCreated",
            "WorkItemLocked",
            "WorkItemCompleted",
            "WorkItemFailed",
            "CheckpointRecorded"
        };

        var failureSql = $"""
            SELECT DISTINCT FailureType
            FROM [{schema}].[MigrationFailures]
            WHERE FailureType IS NOT NULL
              AND LTRIM(RTRIM(FailureType)) <> N'';
            """;

        await using var failureCommand = new SqlCommand(failureSql, connection);
        await using var failureReader =
            await failureCommand.ExecuteReaderAsync(cancellationToken);

        while (await failureReader.ReadAsync(cancellationToken))
        {
            var failureType = failureReader.GetString(0);

            if (!string.IsNullOrWhiteSpace(failureType))
            {
                eventTypes.Add($"FailureRecorded:{failureType}");
            }
        }

        return eventTypes.ToArray();
    }

    private string GetSchemaName()
    {
        return string.IsNullOrWhiteSpace(_options.Value.SchemaName)
            ? "migration"
            : _options.Value.SchemaName;
    }
}
