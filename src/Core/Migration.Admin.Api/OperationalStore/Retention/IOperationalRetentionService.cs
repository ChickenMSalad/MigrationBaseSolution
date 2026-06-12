namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalRetentionService
{
    Task<OperationalRetentionStatusResponse> GetStatusAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalRetentionActionResponse> ArchiveEligibleAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalRetentionActionResponse> PurgeArchivedAsync(
        CancellationToken cancellationToken = default);
}


