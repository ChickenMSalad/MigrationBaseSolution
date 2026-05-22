using Microsoft.Data.SqlClient;

namespace Migration.Infrastructure.State.OperationalStore.Sql;

internal sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly ISqlOperationalStoreConnectionStringResolver _connectionStringResolver;

    public SqlConnectionFactory(
        ISqlOperationalStoreConnectionStringResolver connectionStringResolver)
    {
        _connectionStringResolver = connectionStringResolver;
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connectionString = _connectionStringResolver.ResolveConnectionString();

        var connection = new SqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
