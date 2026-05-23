using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionPlanBuilder : IAzureManifestExecutionPlanBuilder
{
    public AzureManifestExecutionPlan Build(AzureManifestExecutionPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Scope);

        var steps = new List<AzureManifestExecutionPlanStep>();
        var order = 0;

        AddStepIfEnabled(
            steps,
            request.IncludeValidationStep,
            ++order,
            "validate-manifest",
            "Validate manifest",
            "Validate manifest availability, shape, and execution prerequisites.");

        AddStepIfEnabled(
            steps,
            request.IncludePreparationStep,
            ++order,
            "prepare-execution",
            "Prepare execution",
            "Prepare durable execution state and runtime context.");

        AddStepIfEnabled(
            steps,
            request.IncludeExecutionStep,
            ++order,
            "execute-manifest",
            "Execute manifest",
            "Execute manifest work items according to the selected execution mode.");

        AddStepIfEnabled(
            steps,
            request.IncludeVerificationStep,
            ++order,
            "verify-results",
            "Verify results",
            "Verify execution output, audit expectations, and completion criteria.");

        AddStepIfEnabled(
            steps,
            request.IncludeCompletionStep,
            ++order,
            "complete-execution",
            "Complete execution",
            "Finalize execution status and hand off operational evidence.");

        return new AzureManifestExecutionPlan
        {
            PlanId = Guid.NewGuid().ToString("n"),
            Scope = request.Scope,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Steps = steps
        };
    }

    private static void AddStepIfEnabled(
        ICollection<AzureManifestExecutionPlanStep> steps,
        bool enabled,
        int order,
        string stepId,
        string name,
        string description)
    {
        if (!enabled)
        {
            return;
        }

        steps.Add(
            new AzureManifestExecutionPlanStep
            {
                StepId = stepId,
                Name = name,
                Order = order,
                Required = true,
                Description = description
            });
    }
}
