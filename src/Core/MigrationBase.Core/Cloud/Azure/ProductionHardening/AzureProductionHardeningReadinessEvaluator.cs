using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionHardeningReadinessEvaluator :
    IAzureProductionHardeningReadinessEvaluator
{
    private readonly IAzureProductionReleaseGateEvaluator? releaseGateEvaluator;
    private readonly IAzureProductionRollbackEvaluator? rollbackEvaluator;
    private readonly IAzureProductionAbortController? abortController;
    private readonly IAzureProductionReadinessChecklistBuilder? readinessChecklistBuilder;
    private readonly IAzureProductionDeploymentDecisionEvaluator? deploymentDecisionEvaluator;

    public AzureProductionHardeningReadinessEvaluator(
        IAzureProductionReleaseGateEvaluator? releaseGateEvaluator = null,
        IAzureProductionRollbackEvaluator? rollbackEvaluator = null,
        IAzureProductionAbortController? abortController = null,
        IAzureProductionReadinessChecklistBuilder? readinessChecklistBuilder = null,
        IAzureProductionDeploymentDecisionEvaluator? deploymentDecisionEvaluator = null)
    {
        this.releaseGateEvaluator = releaseGateEvaluator;
        this.rollbackEvaluator = rollbackEvaluator;
        this.abortController = abortController;
        this.readinessChecklistBuilder = readinessChecklistBuilder;
        this.deploymentDecisionEvaluator = deploymentDecisionEvaluator;
    }

    public Task<AzureProductionHardeningReadinessReport> EvaluateAsync(
        AzureProductionHardeningReadinessRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<AzureProductionHardeningReadinessIssue>();

        AddMissingIssueIfRequired(
            issues,
            request.RequireReleaseGateEvaluator,
            releaseGateEvaluator,
            "release-gate-evaluator",
            "Production release gate evaluator is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireRollbackEvaluator,
            rollbackEvaluator,
            "rollback-evaluator",
            "Production rollback evaluator is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireAbortController,
            abortController,
            "abort-controller",
            "Production abort controller is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireReadinessChecklistBuilder,
            readinessChecklistBuilder,
            "readiness-checklist-builder",
            "Production readiness checklist builder is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireDeploymentDecisionEvaluator,
            deploymentDecisionEvaluator,
            "deployment-decision-evaluator",
            "Production deployment decision evaluator is not registered.");

        var status = issues.Count == 0
            ? AzureProductionHardeningReadinessStatus.Ready
            : AzureProductionHardeningReadinessStatus.NotReady;

        return Task.FromResult(
            new AzureProductionHardeningReadinessReport
            {
                Status = status,
                EvaluatedAtUtc = DateTimeOffset.UtcNow,
                Issues = issues
            });
    }

    private static void AddMissingIssueIfRequired(
        ICollection<AzureProductionHardeningReadinessIssue> issues,
        bool required,
        object? service,
        string component,
        string message)
    {
        if (!required || service is not null)
        {
            return;
        }

        issues.Add(
            new AzureProductionHardeningReadinessIssue
            {
                Code = "production.hardening.component.missing",
                Component = component,
                Message = message
            });
    }
}
