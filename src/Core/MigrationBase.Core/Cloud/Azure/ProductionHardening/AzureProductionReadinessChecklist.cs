using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionReadinessChecklist
{
    public required string ChecklistId { get; init; }

    public required string ReleaseId { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<AzureProductionReadinessChecklistItem> Items { get; init; } =
        new List<AzureProductionReadinessChecklistItem>();

    public bool IsReady =>
        Items.All(item =>
            !item.Required ||
            item.Status is AzureProductionReadinessItemStatus.Passed or
                AzureProductionReadinessItemStatus.Waived);
}
