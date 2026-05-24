using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Migration.Infrastructure.Runtime.SqlServer;

namespace Migration.Infrastructure.Runtime.Composition;

public sealed record SqlOperationalRuntimeReadinessResult(
    bool IsReady,
    string Message,
    DateTime CheckedAtUtc,
    Exception? Exception = null);

public sealed class SqlOperationalRuntimeReadinessProbe
{
    private readonly ISqlOperationalConnectionFactory _connectionFactory;
    private readonly SqlOperationalRuntimeCompositionOptions _options;

    public SqlOperationalRuntimeReadinessProbe(
        ISqlOperationalConnectionFactory connectionFactory,
        IOptions<SqlOperationalRuntimeCompositionOptions> options)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _options = options?.Value ?? new SqlOperationalRuntimeCompositionOptions();
    }

    public async Task<SqlOperationalRuntimeReadinessResult> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using DbConnection connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using DbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "SELECT 1";
            command.CommandTimeout = _options.ReadinessCommandTimeoutSeconds <= 0
                ? 15
                : _options.ReadinessCommandTimeoutSeconds;

            object? value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return new SqlOperationalRuntimeReadinessResult(
                true,
                string.Concat("SQL operational store is reachable. Probe result: ", value?.ToString() ?? "<null>"),
                DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new SqlOperationalRuntimeReadinessResult(
                false,
                string.Concat("SQL operational store readiness check failed: ", ex.Message),
                DateTime.UtcNow,
                ex);
        }
    }
}
