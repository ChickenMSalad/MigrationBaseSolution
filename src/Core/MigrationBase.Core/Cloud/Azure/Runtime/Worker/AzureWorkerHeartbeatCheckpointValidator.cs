namespace MigrationBase.Core.Cloud.Azure.Runtime.Worker;

public static class AzureWorkerHeartbeatCheckpointValidator
{
    public static IReadOnlyList<string> Validate(AzureWorkerRuntimeHeartbeatCheckpoint checkpoint, AzureWorkerHeartbeatCheckpointOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        var issues = new List<string>();
        var effectiveOptions = options ?? new AzureWorkerHeartbeatCheckpointOptions();

        if (string.IsNullOrWhiteSpace(checkpoint.WorkerId))
        {
            issues.Add("WorkerId is required.");
        }

        if (string.IsNullOrWhiteSpace(checkpoint.HostRole))
        {
            issues.Add("HostRole is required.");
        }

        if (checkpoint.Status == AzureWorkerHeartbeatCheckpointStatus.Running &&
            effectiveOptions.RequireExecutionRunIdWhenRunningWork &&
            !string.IsNullOrWhiteSpace(checkpoint.WorkItemId) &&
            string.IsNullOrWhiteSpace(checkpoint.ExecutionRunId))
        {
            issues.Add("ExecutionRunId is required when a running worker is reporting a work item.");
        }

        if (checkpoint.Properties.Count > effectiveOptions.MaxProperties)
        {
            issues.Add($"Properties exceeds the maximum allowed count of {effectiveOptions.MaxProperties}.");
        }

        if (checkpoint.LeaseExpiresAtUtc.HasValue && checkpoint.LeaseExpiresAtUtc.Value < checkpoint.ObservedAtUtc)
        {
            issues.Add("LeaseExpiresAtUtc is earlier than ObservedAtUtc.");
        }

        return issues;
    }
}
