using System.Data;

namespace Migration.Infrastructure.Sql.Connections;

public interface ISqlConnectionFactory
{
    Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
