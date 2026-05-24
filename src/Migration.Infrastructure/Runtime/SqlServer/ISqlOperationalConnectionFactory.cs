using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Infrastructure.Runtime.SqlServer;

public interface ISqlOperationalConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
