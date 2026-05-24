using Migration.Application.Operational.Health;
using Migration.Application.Operational.Readiness;

namespace Migration.Infrastructure.Sql.Operational.Health;

public sealed class SqlOperationalHealthProbeService : IOperationalHealthProbeService
{
    private readonly IOperationalRuntimeReadinessService _readinessService;

    public SqlOperationalHealthProbeService(IOperationalRuntimeReadinessService readinessService)
    {
        _readinessService = readinessService ?? throw new ArgumentNullException(nameof(readinessService));
    }

    public Task<OperationalHealthProbeResponse> GetLivenessAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new OperationalHealthProbeResponse
        {
            Status = OperationalHealthProbeStatuses.Healthy,
            EvaluatedUtc = DateTimeOffset.UtcNow,
            Component = "sql-operational-worker",
            Message = "Process is alive. Readiness is evaluated separately."
        });
    }

    public async Task<OperationalHealthProbeResponse> GetReadinessAsync(CancellationToken cancellationToken = default)
    {
        var report = await _readinessService.GetReadinessAsync(cancellationToken).ConfigureAwait(false);
        var blockingIssues = report.BlockingIssues ?? Array.Empty<string>();
        var status = report.IsReady ? OperationalHealthProbeStatuses.Healthy : OperationalHealthProbeStatuses.Unhealthy;

        return new OperationalHealthProbeResponse
        {
            Status = status,
            EvaluatedUtc = report.EvaluatedUtc,
            Component = "sql-operational-runtime",
            Message = report.IsReady
                ? "SQL operational runtime readiness check passed."
                : "SQL operational runtime readiness check failed.",
            BlockingIssues = blockingIssues,
            Dependencies = new[]
            {
                new OperationalHealthProbeDependencyStatus
                {
                    Name = "MigrationOperationalStore",
                    Status = status,
                    Message = report.Status
                }
            }
        };
    }
}
