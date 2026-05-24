using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionReadinessChecklistBuilder :
    IAzureProductionReadinessChecklistBuilder
{
    public AzureProductionReadinessChecklist Build(
        AzureProductionReadinessChecklistRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var items = new List<AzureProductionReadinessChecklistItem>();

        if (request.IncludeReleaseGateCheck)
        {
            items.Add(BuildReleaseGateItem(request.ReleaseGateResult));
        }

        if (request.IncludeRollbackCheck)
        {
            items.Add(BuildRollbackItem(request.RollbackDecision));
        }

        if (request.IncludeOperatorSignoffCheck)
        {
            items.Add(BuildOperatorSignoffItem(request.OperatorSignoffGranted));
        }

        return new AzureProductionReadinessChecklist
        {
            ChecklistId = Guid.NewGuid().ToString("n"),
            ReleaseId = request.ReleaseId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Items = items
        };
    }

    private static AzureProductionReadinessChecklistItem BuildReleaseGateItem(
        AzureProductionReleaseGateResult? releaseGateResult)
    {
        if (releaseGateResult is null)
        {
            return new AzureProductionReadinessChecklistItem
            {
                ItemId = "release-gate",
                Name = "Production release gate",
                Description = "Production release gate must be evaluated.",
                Required = true,
                Status = AzureProductionReadinessItemStatus.Failed,
                Evidence = "Release gate result missing."
            };
        }

        return new AzureProductionReadinessChecklistItem
        {
            ItemId = "release-gate",
            Name = "Production release gate",
            Description = "Production release gate must permit release.",
            Required = true,
            Status = releaseGateResult.CanRelease
                ? AzureProductionReadinessItemStatus.Passed
                : AzureProductionReadinessItemStatus.Failed,
            Evidence = releaseGateResult.Status.ToString()
        };
    }

    private static AzureProductionReadinessChecklistItem BuildRollbackItem(
        AzureProductionRollbackDecision? rollbackDecision)
    {
        if (rollbackDecision is null)
        {
            return new AzureProductionReadinessChecklistItem
            {
                ItemId = "rollback-decision",
                Name = "Rollback decision",
                Description = "Rollback decision must be evaluated.",
                Required = true,
                Status = AzureProductionReadinessItemStatus.Failed,
                Evidence = "Rollback decision missing."
            };
        }

        return new AzureProductionReadinessChecklistItem
        {
            ItemId = "rollback-decision",
            Name = "Rollback decision",
            Description = "Rollback decision must not require rollback.",
            Required = true,
            Status = rollbackDecision.ShouldRollback
                ? AzureProductionReadinessItemStatus.Failed
                : AzureProductionReadinessItemStatus.Passed,
            Evidence = rollbackDecision.Status.ToString()
        };
    }

    private static AzureProductionReadinessChecklistItem BuildOperatorSignoffItem(
        bool operatorSignoffGranted)
    {
        return new AzureProductionReadinessChecklistItem
        {
            ItemId = "operator-signoff",
            Name = "Operator signoff",
            Description = "Operator signoff must be granted before production release.",
            Required = true,
            Status = operatorSignoffGranted
                ? AzureProductionReadinessItemStatus.Passed
                : AzureProductionReadinessItemStatus.Failed,
            Evidence = operatorSignoffGranted ? "Granted" : "Missing"
        };
    }
}
