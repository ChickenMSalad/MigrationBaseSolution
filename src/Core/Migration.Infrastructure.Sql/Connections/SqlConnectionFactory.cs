using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Migration.Infrastructure.Sql.Connections; 
using Migration.Infrastructure.Sql.Options;

namespace Migration.Infrastructure.Sql.Connections;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly IOptions<SqlOperationalStoreOptions> _options;

    public SqlConnectionFactory(IOptions<SqlOperationalStoreOptions> options)
    {
        _options = options;
    }

    public async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _options.Value.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("SQL operational store connection string is not configured.");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_options.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
