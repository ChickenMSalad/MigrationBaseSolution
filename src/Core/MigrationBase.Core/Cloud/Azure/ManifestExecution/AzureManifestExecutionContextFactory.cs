using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ManifestExecution;

public sealed class AzureManifestExecutionContextFactory : IAzureManifestExecutionContextFactory
{
    public AzureManifestExecutionContext Create(AzureManifestExecutionContextRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Plan);

        var runtimeProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["planId"] = request.Plan.PlanId,
            ["runId"] = request.Plan.Scope.RunId,
            ["manifestId"] = request.Plan.Scope.ManifestId,
            ["mode"] = request.Plan.Scope.Mode.ToString()
        };

        if (!string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            runtimeProperties["requestedBy"] = request.RequestedBy;
        }

        return new AzureManifestExecutionContext
        {
            ExecutionId = Guid.NewGuid().ToString("n"),
            Plan = request.Plan,
            Status = request.InitialStatus,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RuntimeProperties = runtimeProperties
        };
    }
}
