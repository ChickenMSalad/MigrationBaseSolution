using System;

namespace MigrationBase.Core.Cloud.Azure.Runtime.Workers.Leasing;

public sealed class AzureWorkerLeasePolicy
{
    public TimeSpan DefaultLeaseDuration { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan RenewalInterval { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan AbandonmentTimeout { get; init; } = TimeSpan.FromMinutes(15);
    public int MaxRenewalFailuresBeforeAbandonment { get; init; } = 3;

    public bool IsValid()
    {
        return DefaultLeaseDuration > TimeSpan.Zero
            && RenewalInterval > TimeSpan.Zero
            && RenewalInterval < DefaultLeaseDuration
            && AbandonmentTimeout >= DefaultLeaseDuration
            && MaxRenewalFailuresBeforeAbandonment >= 1;
    }
}
