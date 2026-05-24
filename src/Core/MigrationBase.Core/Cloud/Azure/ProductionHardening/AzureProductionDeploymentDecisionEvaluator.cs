using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionDeploymentDecisionEvaluator :
    IAzureProductionDeploymentDecisionEvaluator
{
    public AzureProductionDeploymentDecision Evaluate(
        AzureProductionDeploymentDecisionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ReleaseGateResult);

        var issues = new List<AzureProductionReleaseGateIssue>(
            request.ReleaseGateResult.Issues);

        if (!request.ReleaseGateResult.CanRelease)
        {
            issues.Add(
                new AzureProductionReleaseGateIssue
                {
                    Code = "production.deployment.releaseGate.blocked",
                    Component = "release-gate",
                    Message = "Release gate does not permit deployment.",
                    IsBlocking = true
                });
        }

        if (request.RequireReadinessChecklist && request.ReadinessChecklist is null)
        {
            issues.Add(
                new AzureProductionReleaseGateIssue
                {
                    Code = "production.deployment.readiness.missing",
                    Component = "readiness-checklist",
                    Message = "Production readiness checklist is required.",
                    IsBlocking = true
                });
        }

        if (request.ReadinessChecklist is not null && !request.ReadinessChecklist.IsReady)
        {
            issues.Add(
                new AzureProductionReleaseGateIssue
                {
                    Code = "production.deployment.readiness.failed",
                    Component = "readiness-checklist",
                    Message = "Production readiness checklist is not ready.",
                    IsBlocking = true
                });
        }

        if (request.RequireRollbackDecision && request.RollbackDecision is null)
        {
            issues.Add(
                new AzureProductionReleaseGateIssue
                {
                    Code = "production.deployment.rollbackDecision.missing",
                    Component = "rollback-decision",
                    Message = "Rollback decision is required.",
                    IsBlocking = true
                });
        }

        if (request.RollbackDecision is not null && request.RollbackDecision.ShouldRollback)
        {
            issues.Add(
                new AzureProductionReleaseGateIssue
                {
                    Code = "production.deployment.rollback.required",
                    Component = "rollback-decision",
                    Message = "Rollback decision requires rollback.",
                    IsBlocking = true
                });
        }

        var hasBlockingIssues = issues.Any(issue => issue.IsBlocking);
        var status = DetermineStatus(hasBlockingIssues, request.OperatorOverrideGranted, issues);

        return new AzureProductionDeploymentDecision
        {
            DecisionId = Guid.NewGuid().ToString("n"),
            ReleaseId = request.ReleaseId,
            Status = status,
            DecidedAtUtc = DateTimeOffset.UtcNow,
            Reason = CreateReason(status),
            Issues = issues
        };
    }

    private static AzureProductionDeploymentDecisionStatus DetermineStatus(
        bool hasBlockingIssues,
        bool operatorOverrideGranted,
        IReadOnlyCollection<AzureProductionReleaseGateIssue> issues)
    {
        if (hasBlockingIssues && !operatorOverrideGranted)
        {
            return AzureProductionDeploymentDecisionStatus.Blocked;
        }

        if (hasBlockingIssues)
        {
            return AzureProductionDeploymentDecisionStatus.ApprovedWithWarnings;
        }

        if (issues.Count > 0)
        {
            return AzureProductionDeploymentDecisionStatus.ApprovedWithWarnings;
        }

        return AzureProductionDeploymentDecisionStatus.Approved;
    }

    private static string CreateReason(AzureProductionDeploymentDecisionStatus status)
    {
        return status switch
        {
            AzureProductionDeploymentDecisionStatus.Approved =>
                "Production deployment is approved.",
            AzureProductionDeploymentDecisionStatus.ApprovedWithWarnings =>
                "Production deployment is approved with warnings or override.",
            AzureProductionDeploymentDecisionStatus.Blocked =>
                "Production deployment is blocked by required checks.",
            AzureProductionDeploymentDecisionStatus.Rejected =>
                "Production deployment is rejected.",
            _ =>
                "Production deployment was not evaluated."
        };
    }
}
