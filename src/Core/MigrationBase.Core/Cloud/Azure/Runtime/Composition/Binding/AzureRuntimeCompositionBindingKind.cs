namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition.Binding;

public enum AzureRuntimeCompositionBindingKind
{
    Configuration = 0,
    OperationalStore = 1,
    Queue = 2,
    Telemetry = 3,
    WorkerLifecycle = 4,
    Governance = 5,
    Validation = 6
}
