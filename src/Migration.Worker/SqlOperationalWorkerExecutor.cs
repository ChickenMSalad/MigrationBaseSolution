using System.Text.Json;
using Migration.Infrastructure.Runtime.SqlServer;

namespace Migration.Worker;

internal sealed class SqlOperationalWorkerExecutor
{
    private readonly ILogger<SqlOperationalWorkerExecutor> _logger;

    public SqlOperationalWorkerExecutor(ILogger<SqlOperationalWorkerExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<SqlOperationalWorkItemExecutionResult> ExecuteAsync(
        SqlOperationalWorkItem workItem,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (workItem is null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        _logger.LogInformation(
            "P7 worker host claimed work item {WorkItemId} for run {RunId}. WorkType={WorkType}, ManifestRowId={ManifestRowId}",
            workItem.WorkItemId,
            workItem.RunId,
            workItem.WorkType,
            workItem.ManifestRowId);

        // Intentional smoke executor for P7.5. Replace this method body with the real connector/runtime dispatch
        // once the concrete connector execution bridge is selected in P7.6.
        string resultJson = JsonSerializer.Serialize(new
        {
            executor = "p7-worker-host-smoke",
            status = "completed",
            workItemId = workItem.WorkItemId,
            runId = workItem.RunId,
            workType = workItem.WorkType,
            completedAtUtc = DateTime.UtcNow
        });

        return Task.FromResult(SqlOperationalWorkItemExecutionResult.Complete(resultJson));
    }
}
