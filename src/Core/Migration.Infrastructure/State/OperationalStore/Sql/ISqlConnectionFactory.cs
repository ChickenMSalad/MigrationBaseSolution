using Microsoft.Data.SqlClient;

namespace Migration.Infrastructure.State.OperationalStore.Sql;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(
        CancellationToken cancellationToken = default);
}
