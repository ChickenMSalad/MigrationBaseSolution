using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Operational.Events;

public sealed class SqlOperationalEventRetentionService : IOperationalEventRetentionService
{
    private readonly IConfiguration _configuration;

    public SqlOperationalEventRetentionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<OperationalEventRetentionResult> PruneAsync(
        int retentionDays,
        CancellationToken cancellationToken)
    {
        var safeRetentionDays = Math.Clamp(retentionDays, 1, 3650);
        var cutoffUtc = DateTimeOffset.UtcNow.AddDays(-safeRetentionDays);
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
DELETE FROM dbo.MigrationOperationalEvents
WHERE CreatedUtc < @CutoffUtc;
";

        command.Parameters.AddWithValue("@CutoffUtc", cutoffUtc);
        var deleted = await command.ExecuteNonQueryAsync(cancellationToken);

        return new OperationalEventRetentionResult(
            RetentionDays: safeRetentionDays,
            DeletedEvents: deleted,
            CutoffUtc: cutoffUtc);
    }

    private string GetConnectionString()
    {
        var connectionString =
            _configuration.GetConnectionString("OperationalSql") ??
            _configuration["OperationalSql:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Operational SQL connection string is not configured.");
        }

        return connectionString;
    }
}
