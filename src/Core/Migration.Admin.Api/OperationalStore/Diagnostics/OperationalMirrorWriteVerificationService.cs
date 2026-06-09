using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalMirrorWriteVerificationService
    : IOperationalMirrorWriteVerificationService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public OperationalMirrorWriteVerificationService(
        ISqlConnectionFactory connectionFactory,
        IOptions<SqlOperationalStoreOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options;
    }

    public async Task<OperationalMirrorWriteVerificationResult> VerifyAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();

        var schemaName = string.IsNullOrWhiteSpace(_options.Value.SchemaName)
            ? "migration"
            : _options.Value.SchemaName;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var runCount = await CountAsync(
            connection,
            schemaName,
            "Runs",
            cancellationToken);

        var manifestRecordCount = await CountAsync(
            connection,
            schemaName,
            "MigrationManifestRecords",
            cancellationToken);

        var workItemCount = await CountAsync(
            connection,
            schemaName,
            "WorkItems",
            cancellationToken);

        var checkpointCount = await CountAsync(
            connection,
            schemaName,
            "MigrationCheckpoints",
            cancellationToken);

        if (runCount == 0)
        {
            messages.Add("No operational run records found.");
        }

        if (manifestRecordCount == 0)
        {
            messages.Add("No operational manifest records found.");
        }

        if (workItemCount == 0)
        {
            messages.Add("No operational work item records found.");
        }

        if (checkpointCount == 0)
        {
            messages.Add("No operational checkpoint records found.");
        }

        if (runCount > 0 &&
            manifestRecordCount > 0 &&
            workItemCount > 0 &&
            checkpointCount > 0)
        {
            messages.Add("Operational mirror writes were found.");
        }

        return new OperationalMirrorWriteVerificationResult
        {
            HasRuns = runCount > 0,
            HasManifestRecords = manifestRecordCount > 0,
            HasWorkItems = workItemCount > 0,
            HasCheckpoints = checkpointCount > 0,
            RunCount = runCount,
            ManifestRecordCount = manifestRecordCount,
            WorkItemCount = workItemCount,
            CheckpointCount = checkpointCount,
            Messages = messages
        };
    }

    private static async Task<int> CountAsync(
        SqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT COUNT(1) FROM [{schemaName}].[{tableName}];";

        await using var command = new SqlCommand(
            sql,
            connection);

        var result = await command.ExecuteScalarAsync(
            cancellationToken);

        return Convert.ToInt32(result);
    }
}


