namespace Migration.Admin.Api.Operational.Execution;

public interface IExecutionDiagnosticExportService
{
    Task<ExecutionDiagnosticExportBundle> BuildBundleAsync(
        Guid executionSessionId,
        CancellationToken cancellationToken);
}


