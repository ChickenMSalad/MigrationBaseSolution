namespace MigrationBase.Core.Cloud.Azure.Workers.Concurrency;

public sealed class AzureWorkerConcurrencyLimit
{
    public string Scope { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int Limit { get; init; } = 1;
    public string Reason { get; init; } = string.Empty;
    public bool IsHardLimit { get; init; } = true;
}
