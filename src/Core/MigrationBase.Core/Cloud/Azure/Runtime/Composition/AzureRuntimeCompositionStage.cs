namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

/// <summary>
/// Defines the stable startup stages used by Azure-hosted runtime composition.
/// </summary>
public enum AzureRuntimeCompositionStage
{
    Foundation = 0,
    Configuration = 100,
    Identity = 200,
    Storage = 300,
    Sql = 400,
    Queueing = 500,
    WorkerRuntime = 600,
    Observability = 700,
    Governance = 800,
    Execution = 900,
    Validation = 1000
}
