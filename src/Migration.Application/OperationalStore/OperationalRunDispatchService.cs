using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalRunDispatchService : IOperationalRunDispatchService
{
    private readonly IOperationalRunLifecycleService _runLifecycleService;
    private readonly IOperationalManifestDispatchService _manifestDispatchService;

    public OperationalRunDispatchService(
        IOperationalRunLifecycleService runLifecycleService,
        IOperationalManifestDispatchService manifestDispatchService)
    {
        _runLifecycleService = runLifecycleService;
        _manifestDispatchService = manifestDispatchService;
    }

    public async Task<OperationalRunDispatchResult> DispatchAsync(
        string sourceSystem,
        string targetSystem,
        IReadOnlyCollection<MigrationManifestRecord> manifestRecords,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifestRecords);

        var run = await _runLifecycleService.CreateRunAsync(
            sourceSystem,
            targetSystem,
            cancellationToken);

        try
        {
            await _runLifecycleService.MarkRunStartedAsync(
                run.RunId,
                cancellationToken);

            var manifestDispatchResults = await _manifestDispatchService.DispatchBatchAsync(
                run.RunId,
                manifestRecords,
                cancellationToken);

            return new OperationalRunDispatchResult(
                run,
                manifestDispatchResults);
        }
        catch (Exception ex)
        {
            await _runLifecycleService.MarkRunFailedAsync(
                run.RunId,
                ex.Message,
                cancellationToken);

            throw;
        }
    }
}
