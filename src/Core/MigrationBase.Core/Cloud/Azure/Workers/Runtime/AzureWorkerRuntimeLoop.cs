namespace MigrationBase.Core.Cloud.Azure.Workers.Runtime;

public sealed class AzureWorkerRuntimeLoop : IAzureWorkerRuntimeLoop
{
    private readonly AzureWorkerRuntimeLoopOptions _options;
    private readonly IReadOnlyList<IAzureWorkerRuntimeStep> _steps;

    public AzureWorkerRuntimeLoop(AzureWorkerRuntimeLoopOptions options, IEnumerable<IAzureWorkerRuntimeStep>? steps = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _steps = (steps ?? Array.Empty<IAzureWorkerRuntimeStep>()).ToArray();
    }

    public async ValueTask<AzureWorkerRuntimeLoopResult> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled)
        {
            return AzureWorkerRuntimeLoopResult.Disabled("Worker runtime loop is disabled by configuration.");
        }

        var iterationsCompleted = 0;
        var workWasProcessed = false;
        var maxIterations = Math.Max(1, _options.MaxIterationsPerRun);

        for (var index = 0; index < maxIterations; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var processedDuringIteration = false;
            foreach (var step in _steps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processedDuringIteration |= await step.ExecuteAsync(_options, cancellationToken).ConfigureAwait(false);
            }

            iterationsCompleted++;
            workWasProcessed |= processedDuringIteration;

            if (!processedDuringIteration)
            {
                break;
            }
        }

        return AzureWorkerRuntimeLoopResult.Completed(
            iterationsCompleted,
            workWasProcessed,
            workWasProcessed ? "Worker runtime loop processed work." : "Worker runtime loop found no work.");
    }
}
