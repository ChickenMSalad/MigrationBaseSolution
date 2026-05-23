using Microsoft.Data.SqlClient;
using Migration.Admin.Api.Operational.Events;

namespace Migration.Admin.Api.Operational.Execution;

public sealed class SqlExecutionReplayPolicyOverrideService : IExecutionReplayPolicyOverrideService
{
    private readonly IConfiguration _configuration;
    private readonly IExecutionReplayPolicyService _policyService;
    private readonly IOperationalEventStore _eventStore;

    public SqlExecutionReplayPolicyOverrideService(
        IConfiguration configuration,
        IExecutionReplayPolicyService policyService,
        IOperationalEventStore eventStore)
    {
        _configuration = configuration;
        _policyService = policyService;
        _eventStore = eventStore;
    }

    public async Task<ExecutionReplayPolicyOverrideResult> OverrideAsync(
        OverrideExecutionReplayPolicyRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverriddenBy))
        {
            throw new InvalidOperationException("OverriddenBy is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OverrideReason))
        {
            throw new InvalidOperationException("OverrideReason is required.");
        }

        var scope = NormalizeScope(request.Scope);
        var policy = await _policyService.EvaluateAsync(request.SourceExecutionSessionId, scope, cancellationToken);

        if (policy.Decision == "allow")
        {
            throw new InvalidOperationException("Replay policy does not require an override because the decision is allow.");
        }

        if (policy.Decision == "block")
        {
            throw new InvalidOperationException("Replay policy block decisions cannot be overridden.");
        }

        var overrideId = Guid.NewGuid();
        var expiresUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Clamp(request.ExpiresInMinutes, 5, 1440));
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO dbo.MigrationExecutionReplayPolicyOverrides
(
    ReplayPolicyOverrideId,
    SourceExecutionSessionId,
    Scope,
    PolicyDecision,
    PolicyScore,
    OverriddenBy,
    OverrideReason,
    Status,
    ExpiresUtc,
    CreatedUtc
)
VALUES
(
    @ReplayPolicyOverrideId,
    @SourceExecutionSessionId,
    @Scope,
    @PolicyDecision,
    @PolicyScore,
    @OverriddenBy,
    @OverrideReason,
    'active',
    @ExpiresUtc,
    SYSUTCDATETIME()
);
";

        command.Parameters.AddWithValue("@ReplayPolicyOverrideId", overrideId);
        command.Parameters.AddWithValue("@SourceExecutionSessionId", request.SourceExecutionSessionId);
        command.Parameters.AddWithValue("@Scope", scope);
        command.Parameters.AddWithValue("@PolicyDecision", policy.Decision);
        command.Parameters.AddWithValue("@PolicyScore", policy.PolicyScore);
        command.Parameters.AddWithValue("@OverriddenBy", request.OverriddenBy.Trim());
        command.Parameters.AddWithValue("@OverrideReason", request.OverrideReason.Trim());
        command.Parameters.AddWithValue("@ExpiresUtc", expiresUtc);

        await command.ExecuteNonQueryAsync(cancellationToken);

        await _eventStore.WriteAsync(
            "ExecutionReplayPolicyOverridden",
            "warning",
            "execution",
            "Migration.Admin.Api",
            $"Replay policy warning overridden for scope '{scope}' by {request.OverriddenBy.Trim()}.",
            null,
            request.SourceExecutionSessionId,
            null,
            cancellationToken);

        var record = new ExecutionReplayPolicyOverrideRecord(
            overrideId,
            request.SourceExecutionSessionId,
            scope,
            policy.Decision,
            policy.PolicyScore,
            request.OverriddenBy.Trim(),
            request.OverrideReason.Trim(),
            "active",
            expiresUtc,
            DateTimeOffset.UtcNow,
            null,
            null);

        return new ExecutionReplayPolicyOverrideResult(record, policy.Violations);
    }

    public async Task<ExecutionReplayPolicyOverrideRecord?> FindActiveOverrideAsync(
        Guid sourceExecutionSessionId,
        string scope,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (1)
    ReplayPolicyOverrideId,
    SourceExecutionSessionId,
    Scope,
    PolicyDecision,
    PolicyScore,
    OverriddenBy,
    OverrideReason,
    Status,
    ExpiresUtc,
    CreatedUtc,
    ConsumedUtc,
    ReplayExecutionSessionId
FROM dbo.MigrationExecutionReplayPolicyOverrides
WHERE SourceExecutionSessionId = @SourceExecutionSessionId
  AND Scope = @Scope
  AND Status = 'active'
  AND ExpiresUtc > SYSUTCDATETIME()
ORDER BY CreatedUtc DESC;
";
        command.Parameters.AddWithValue("@SourceExecutionSessionId", sourceExecutionSessionId);
        command.Parameters.AddWithValue("@Scope", NormalizeScope(scope));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadOverride(reader);
    }

    public async Task<IReadOnlyList<ExecutionReplayPolicyOverrideRecord>> ReadHistoryAsync(
        Guid sourceExecutionSessionId,
        int take,
        CancellationToken cancellationToken)
    {
        var safeTake = Math.Clamp(take, 1, 250);
        var records = new List<ExecutionReplayPolicyOverrideRecord>();
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (@Take)
    ReplayPolicyOverrideId,
    SourceExecutionSessionId,
    Scope,
    PolicyDecision,
    PolicyScore,
    OverriddenBy,
    OverrideReason,
    CASE
        WHEN Status = 'active' AND ExpiresUtc <= SYSUTCDATETIME() THEN 'expired'
        ELSE Status
    END AS Status,
    ExpiresUtc,
    CreatedUtc,
    ConsumedUtc,
    ReplayExecutionSessionId
FROM dbo.MigrationExecutionReplayPolicyOverrides
WHERE SourceExecutionSessionId = @SourceExecutionSessionId
ORDER BY CreatedUtc DESC;
";
        command.Parameters.AddWithValue("@SourceExecutionSessionId", sourceExecutionSessionId);
        command.Parameters.AddWithValue("@Take", safeTake);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(ReadOverride(reader));
        }

        return records;
    }

    public async Task ConsumeAsync(
        Guid replayPolicyOverrideId,
        Guid replayExecutionSessionId,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE dbo.MigrationExecutionReplayPolicyOverrides
SET
    Status = 'consumed',
    ConsumedUtc = SYSUTCDATETIME(),
    ReplayExecutionSessionId = @ReplayExecutionSessionId
WHERE ReplayPolicyOverrideId = @ReplayPolicyOverrideId
  AND Status = 'active'
  AND ExpiresUtc > SYSUTCDATETIME();
";
        command.Parameters.AddWithValue("@ReplayPolicyOverrideId", replayPolicyOverrideId);
        command.Parameters.AddWithValue("@ReplayExecutionSessionId", replayExecutionSessionId);

        var updated = await command.ExecuteNonQueryAsync(cancellationToken);
        if (updated != 1)
        {
            throw new InvalidOperationException("Replay policy override could not be consumed because it was missing, expired, or already consumed.");
        }
    }

    private static ExecutionReplayPolicyOverrideRecord ReadOverride(SqlDataReader reader)
    {
        return new ExecutionReplayPolicyOverrideRecord(
            ReplayPolicyOverrideId: reader.GetGuid(0),
            SourceExecutionSessionId: reader.GetGuid(1),
            Scope: reader.GetString(2),
            PolicyDecision: reader.GetString(3),
            PolicyScore: reader.GetInt32(4),
            OverriddenBy: reader.GetString(5),
            OverrideReason: reader.GetString(6),
            Status: reader.GetString(7),
            ExpiresUtc: reader.GetFieldValue<DateTimeOffset>(8),
            CreatedUtc: reader.GetFieldValue<DateTimeOffset>(9),
            ConsumedUtc: reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
            ReplayExecutionSessionId: reader.IsDBNull(11) ? null : reader.GetGuid(11));
    }

    private static string NormalizeScope(string? scope)
    {
        var normalized = string.IsNullOrWhiteSpace(scope)
            ? "failed-only"
            : scope.Trim().ToLowerInvariant();

        return normalized switch
        {
            "failed-only" => normalized,
            "dead-letter-only" => normalized,
            "incomplete-only" => normalized,
            "all" => normalized,
            _ => "failed-only"
        };
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
