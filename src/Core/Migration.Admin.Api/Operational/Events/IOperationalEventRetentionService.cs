namespace Migration.Admin.Api.Operational.Events;

public interface IOperationalEventRetentionService
{
    Task<OperationalEventRetentionResult> PruneAsync(
        int retentionDays,
        CancellationToken cancellationToken);
}


