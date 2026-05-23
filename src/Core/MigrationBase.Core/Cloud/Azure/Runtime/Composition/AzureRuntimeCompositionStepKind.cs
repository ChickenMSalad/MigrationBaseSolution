namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public enum AzureRuntimeCompositionStepKind
{
    Configuration = 0,
    Options = 1,
    Services = 2,
    Persistence = 3,
    Queueing = 4,
    Observability = 5,
    WorkerRuntime = 6,
    Governance = 7,
    Validation = 8
}
