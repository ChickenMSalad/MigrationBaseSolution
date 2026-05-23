namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public interface IAzureRuntimeCompositionPlanner
{
    AzureRuntimeCompositionPlan CreatePlan(string environmentName, string hostRole);

    AzureRuntimeCompositionValidationResult Validate(AzureRuntimeCompositionPlan plan);
}
