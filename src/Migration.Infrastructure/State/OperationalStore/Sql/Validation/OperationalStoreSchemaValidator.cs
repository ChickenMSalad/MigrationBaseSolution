using Migration.Infrastructure.State.OperationalStore.Sql.Queries;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Validation;

public sealed class OperationalStoreSchemaValidator : IOperationalStoreSchemaValidator
{
    private const int ExpectedTableCount = 6;

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<OperationalStoreSchemaValidator> _logger;

    public OperationalStoreSchemaValidator(
        ISqlConnectionFactory connectionFactory,
        ILogger<OperationalStoreSchemaValidator> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task ValidateAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await ValidateConnectivityAsync(connection, cancellationToken);
        await ValidateRequiredTablesAsync(connection, cancellationToken);

        _logger.LogInformation(
            "Operational store schema validation completed successfully.");
    }

    private static async Task ValidateConnectivityAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            OperationalStoreValidationSql.ConnectivityProbe,
            connection);

        await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task ValidateRequiredTablesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            OperationalStoreValidationSql.RequiredTablesExist,
            connection);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        var tableCount = Convert.ToInt32(result);

        if (tableCount != ExpectedTableCount)
        {
            throw new InvalidOperationException(
                $"Operational store schema validation failed. Expected {ExpectedTableCount} required tables but found {tableCount}.");
        }
    }
}
