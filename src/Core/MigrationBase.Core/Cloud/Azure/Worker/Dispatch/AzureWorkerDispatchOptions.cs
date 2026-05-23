namespace MigrationBase.Core.Cloud.Azure.Worker.Dispatch;

public sealed class AzureWorkerDispatchOptions
{
    public const string SectionName = "AzureRuntime:WorkerDispatch";

    public bool Enabled { get; set; } = true;

    public string QueueName { get; set; } = "migration-work-items";

    public TimeSpan DefaultClaimLeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    public int MaxConcurrentClaims { get; set; } = 4;

    public bool UseInMemoryClaimStore { get; set; } = true;
}
