namespace Migration.Infrastructure.Runtime.Composition;

public sealed class SqlOperationalRuntimeCompositionOptions
{
    public const string SectionName = "MigrationRuntime:SqlOperationalRuntime";

    public bool EnableHostedWorker { get; set; } = true;

    public bool EnableReadinessProbe { get; set; } = true;

    public int ReadinessCommandTimeoutSeconds { get; set; } = 15;
}
