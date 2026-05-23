namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

/// <summary>
/// Builds runtime composition plans using the current AzureRuntimeCompositionPlan model.
/// This type intentionally delegates to AzureRuntimeCompositionPlanner so the repo has one
/// canonical plan shape: Name, EnvironmentName, HostRole, RequiredConfigurationSections,
/// RequiredOperationalStores, and Steps.
/// </summary>
public sealed class AzureRuntimeCompositionPlanBuilder
{
    private readonly IAzureRuntimeCompositionPlanner _planner;

    public AzureRuntimeCompositionPlanBuilder()
        : this(new AzureRuntimeCompositionPlanner())
    {
    }

    public AzureRuntimeCompositionPlanBuilder(IAzureRuntimeCompositionPlanner planner)
    {
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
    }

    public AzureRuntimeCompositionPlan Build(string environmentName, string hostRole)
    {
        return _planner.CreatePlan(environmentName, hostRole);
    }

    public AzureRuntimeCompositionValidationResult Validate(AzureRuntimeCompositionPlan plan)
    {
        return _planner.Validate(plan);
    }
}
