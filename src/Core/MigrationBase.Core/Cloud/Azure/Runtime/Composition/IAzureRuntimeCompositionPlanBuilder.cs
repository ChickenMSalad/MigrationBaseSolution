namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public interface IAzureRuntimeCompositionPlanBuilder
{
    AzureRuntimeCompositionPlan BuildPlan(
        string hostRole,
        string environmentName,
        IEnumerable<AzureRuntimeCompositionModuleDescriptor> modules);
}
