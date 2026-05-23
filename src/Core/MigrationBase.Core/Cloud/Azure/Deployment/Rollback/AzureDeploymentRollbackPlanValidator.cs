using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Rollback;

public static class AzureDeploymentRollbackPlanValidator
{
    public static AzureDeploymentRollbackValidationResult Validate(AzureDeploymentRollbackPlan? plan)
    {
        var errors = new List<string>();

        if (plan is null)
        {
            return AzureDeploymentRollbackValidationResult.Failed(new[] { "Rollback plan is required." });
        }

        if (string.IsNullOrWhiteSpace(plan.PlanId)) errors.Add("Rollback plan id is required.");
        if (string.IsNullOrWhiteSpace(plan.EnvironmentName)) errors.Add("Environment name is required.");
        if (string.IsNullOrWhiteSpace(plan.DeploymentTarget)) errors.Add("Deployment target is required.");
        if (string.IsNullOrWhiteSpace(plan.ReleaseVersion)) errors.Add("Release version is required.");
        if (string.IsNullOrWhiteSpace(plan.PreviousReleaseVersion)) errors.Add("Previous release version is required.");

        var steps = plan.Steps?.ToArray() ?? System.Array.Empty<AzureDeploymentRollbackStep>();
        if (steps.Length == 0)
        {
            errors.Add("At least one rollback step is required.");
        }

        foreach (var step in steps)
        {
            if (step.Order <= 0) errors.Add("Rollback step order must be greater than zero.");
            if (string.IsNullOrWhiteSpace(step.ActionName)) errors.Add("Rollback step action name is required.");
            if (step.RequiresManualConfirmation && string.IsNullOrWhiteSpace(step.EvidenceKey))
            {
                errors.Add($"Rollback step '{step.ActionName}' requires an evidence key when manual confirmation is required.");
            }
        }

        var duplicateOrders = steps
            .GroupBy(step => step.Order)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (var duplicateOrder in duplicateOrders)
        {
            errors.Add($"Duplicate rollback step order detected: {duplicateOrder}.");
        }

        return AzureDeploymentRollbackValidationResult.Failed(errors);
    }
}
