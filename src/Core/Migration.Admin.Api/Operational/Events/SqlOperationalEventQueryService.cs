using Microsoft.Data.SqlClient;

namespace Migration.Admin.Api.Operational.Events;

public sealed class SqlOperationalEventQueryService : IOperationalEventQueryService
{
    private readonly IConfiguration _configuration;

    public SqlOperationalEventQueryService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<OperationalEventRecord>> QueryAsync(
        OperationalEventQueryRequest request,
        CancellationToken cancellationToken)
    {
        var safeSkip = Math.Max(0, request.Skip);
        var safeTake = Math.Clamp(request.Take, 1, 250);

        var connectionString = GetConnectionString();
        var events = new List<OperationalEventRecord>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = @"
SELECT
    OperationalEventId,
    EventType,
    Severity,
    Category,
    Source,
    Message,
    PayloadJson,
    CreatedUtc
FROM dbo.MigrationOperationalEvents
WHERE
    (@Severity IS NULL OR Severity = @Severity)
    AND (@Category IS NULL OR Category = @Category)
    AND (@EventType IS NULL OR EventType = @EventType)
ORDER BY CreatedUtc DESC
OFFSET @Skip ROWS
FETCH NEXT @Take ROWS ONLY;
";

        command.Parameters.AddWithValue("@Severity", (object?)request.Severity ?? DBNull.Value);
        command.Parameters.AddWithValue("@Category", (object?)request.Category ?? DBNull.Value);
        command.Parameters.AddWithValue("@EventType", (object?)request.EventType ?? DBNull.Value);
        command.Parameters.AddWithValue("@Skip", safeSkip);
        command.Parameters.AddWithValue("@Take", safeTake);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(new OperationalEventRecord(
                OperationalEventId: reader.GetGuid(0),
                EventType: reader.GetString(1),
                Severity: reader.GetString(2),
                Category: reader.GetString(3),
                Source: reader.GetString(4),
                Message: reader.GetString(5),
                PayloadJson: reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedUtc: reader.GetFieldValue<DateTimeOffset>(7)));
        }

        return events;
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
