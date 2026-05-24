using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Migration.Infrastructure.Runtime.SqlServer;

namespace Migration.Infrastructure.Runtime.Composition;

public sealed class DelegateSqlOperationalConnectionFactory : ISqlOperationalConnectionFactory
{
    private readonly Func<CancellationToken, Task<DbConnection>> _openConnection;

    public DelegateSqlOperationalConnectionFactory(Func<CancellationToken, Task<DbConnection>> openConnection)
    {
        _openConnection = openConnection ?? throw new ArgumentNullException(nameof(openConnection));
    }

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = await _openConnection(cancellationToken).ConfigureAwait(false);
        if (connection is null)
        {
            throw new InvalidOperationException("The SQL operational connection factory returned null.");
        }

        return connection;
    }
}
