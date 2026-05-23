namespace MigrationBase.Core.Cloud.Azure.Workers.Runtime;

public sealed class AzureWorkerRuntimeLoopResult
{
    public AzureWorkerRuntimeLoopState State { get; init; } = AzureWorkerRuntimeLoopState.NotStarted;

    public int IterationsCompleted { get; init; }

    public bool WorkWasProcessed { get; init; }

    public string? Message { get; init; }

    public static AzureWorkerRuntimeLoopResult Disabled(string message) => new()
    {
        State = AzureWorkerRuntimeLoopState.Disabled,
        Message = message
    };

    public static AzureWorkerRuntimeLoopResult Completed(int iterationsCompleted, bool workWasProcessed, string? message = null) => new()
    {
        State = workWasProcessed ? AzureWorkerRuntimeLoopState.Running : AzureWorkerRuntimeLoopState.Idle,
        IterationsCompleted = iterationsCompleted,
        WorkWasProcessed = workWasProcessed,
        Message = message
    };
}
