namespace Migration.Shared.Configuration.Hosts.Common;

public sealed class HostBatchOptions
{
    public int BatchSize { get; set; } = 100;
    public bool RetryFailuresOnly { get; set; }
    public bool DryRun { get; set; }
}
