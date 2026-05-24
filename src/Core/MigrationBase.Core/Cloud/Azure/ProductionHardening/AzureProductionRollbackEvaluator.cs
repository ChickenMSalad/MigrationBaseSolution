using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionRollbackEvaluator :
    IAzureProductionRollbackEvaluator
{
    public AzureProductionRollbackDecision Evaluate(
        AzureProductionRollbackRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var evidence = new Dictionary<string, string>(
            request.Evidence,
            StringComparer.OrdinalIgnoreCase)
        {
            ["trigger"] = request.Trigger.ToString()
        };

        if (request.ReleaseGateResult is not null)
        {
            evidence["releaseGate.status"] = request.ReleaseGateResult.Status.ToString();
            evidence["releaseGate.canRelease"] = request.ReleaseGateResult.CanRelease.ToString();
        }

        if (request.OperatorRequestedRollback)
        {
            return CreateDecision(
                request,
                AzureProductionRollbackDecisionStatus.RollbackRequired,
                "Operator requested production rollback.",
                evidence);
        }

        if (request.ReleaseGateResult is not null &&
            request.ReleaseGateResult.Status == AzureProductionReleaseGateStatus.Blocked &&
            request.RequireRollbackOnBlockedGate)
        {
            return CreateDecision(
                request,
                AzureProductionRollbackDecisionStatus.RollbackRequired,
                "Release gate is blocked and rollback is required by policy.",
                evidence);
        }

        if (request.ReleaseGateResult is not null &&
            !request.ReleaseGateResult.CanRelease)
        {
            return CreateDecision(
                request,
                AzureProductionRollbackDecisionStatus.RollbackRecommended,
                "Release gate did not permit release.",
                evidence);
        }

        if (request.Trigger is AzureProductionRollbackTrigger.ErrorRateExceeded or
            AzureProductionRollbackTrigger.HealthSignalDegraded)
        {
            return CreateDecision(
                request,
                AzureProductionRollbackDecisionStatus.RollbackRecommended,
                "Production health trigger recommends rollback.",
                evidence);
        }

        return CreateDecision(
            request,
            AzureProductionRollbackDecisionStatus.Continue,
            "No rollback condition matched.",
            evidence);
    }

    private static AzureProductionRollbackDecision CreateDecision(
        AzureProductionRollbackRequest request,
        AzureProductionRollbackDecisionStatus status,
        string reason,
        IReadOnlyDictionary<string, string> evidence)
    {
        return new AzureProductionRollbackDecision
        {
            ReleaseId = request.ReleaseId,
            Status = status,
            Trigger = request.Trigger,
            Reason = reason,
            EvaluatedAtUtc = DateTimeOffset.UtcNow,
            Evidence = evidence
        };
    }
}
