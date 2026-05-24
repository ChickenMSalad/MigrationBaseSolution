using System;

namespace Migration.Infrastructure.Runtime.Hosted;

public sealed class SqlOperationalWorkerOptions
{
    public const string SectionName = "MigrationRuntime:SqlOperationalWorker";

    public bool Enabled { get; set; } = false;

    public string WorkerId { get; set; } = Environment.MachineName;

    public int BatchSize { get; set; } = 25;

    public int LeaseSeconds { get; set; } = 300;

    public int RetryDelaySeconds { get; set; } = 300;

    public int IdleDelayMilliseconds { get; set; } = 5000;

    public Guid? RunId { get; set; }

    public bool RunUntilIdleAndStop { get; set; } = false;
}
