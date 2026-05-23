namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

/// <summary>
/// Converts runtime composition planner validation results into an operator-facing validation report.
/// </summary>
public sealed class AzureRuntimeCompositionValidationReportBuilder
{
    private readonly IAzureRuntimeCompositionPlanner _planner;
    private readonly IAzureRuntimeCompositionValidationReporter _reporter;

    public AzureRuntimeCompositionValidationReportBuilder()
        : this(new AzureRuntimeCompositionPlanner(), new AzureRuntimeCompositionValidationReporter())
    {
    }

    public AzureRuntimeCompositionValidationReportBuilder(
        IAzureRuntimeCompositionPlanner planner,
        IAzureRuntimeCompositionValidationReporter reporter)
    {
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
    }

    public AzureRuntimeCompositionValidationReport Build(AzureRuntimeCompositionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var validationResult = _planner.Validate(plan);
        var findings = CreateFindings(validationResult);

        return _reporter.CreateReport(
            plan,
            string.IsNullOrWhiteSpace(plan.Name) ? "runtime-composition" : plan.Name,
            string.IsNullOrWhiteSpace(plan.EnvironmentName) ? null : plan.EnvironmentName,
            findings);
    }

    private static IEnumerable<AzureRuntimeCompositionValidationFinding> CreateFindings(
        AzureRuntimeCompositionValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        foreach (var error in validationResult.Errors ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                continue;
            }

            yield return new AzureRuntimeCompositionValidationFinding
            {
                Code = "runtime-composition.validation.error",
                Message = error,
                Severity = AzureRuntimeCompositionValidationSeverity.Error,
                Target = nameof(AzureRuntimeCompositionPlan),
                RecommendedAction = "Correct the runtime composition plan before enabling executable host wiring."
            };
        }

        foreach (var warning in validationResult.Warnings ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(warning))
            {
                continue;
            }

            yield return new AzureRuntimeCompositionValidationFinding
            {
                Code = "runtime-composition.validation.warning",
                Message = warning,
                Severity = AzureRuntimeCompositionValidationSeverity.Warning,
                Target = nameof(AzureRuntimeCompositionPlan),
                RecommendedAction = "Review the runtime composition plan before deployment promotion."
            };
        }
    }
}
