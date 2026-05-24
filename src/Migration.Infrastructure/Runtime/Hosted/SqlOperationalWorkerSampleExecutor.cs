using System.Threading;
using System.Threading.Tasks;
using Migration.Infrastructure.Runtime.SqlServer;

namespace Migration.Infrastructure.Runtime.Hosted;

public static class SqlOperationalWorkerSampleExecutor
{
    public static Task<SqlOperationalWorkItemExecutionResult> CompleteWithoutMigrationAsync(
        SqlOperationalWorkItem workItem,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string resultJson = "{\"executor\":\"sample\",\"status\":\"completed-without-migration\"}";
        return Task.FromResult(SqlOperationalWorkItemExecutionResult.Complete(resultJson));
    }
}
