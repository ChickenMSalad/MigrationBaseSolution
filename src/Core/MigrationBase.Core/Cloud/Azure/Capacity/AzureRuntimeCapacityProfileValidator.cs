using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Capacity;

public static class AzureRuntimeCapacityProfileValidator
{
    public static AzureRuntimeCapacityValidationResult Validate(AzureRuntimeCapacityProfile profile)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        var messages = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.EnvironmentName))
        {
            messages.Add("EnvironmentName is required.");
        }

        if (profile.MaximumConcurrentRuns < 1)
        {
            messages.Add("MaximumConcurrentRuns must be at least 1.");
        }

        if (profile.MaximumActiveWorkers < 1)
        {
            messages.Add("MaximumActiveWorkers must be at least 1.");
        }

        if (profile.MaximumQueueReadersPerWorker < 1)
        {
            messages.Add("MaximumQueueReadersPerWorker must be at least 1.");
        }

        if (profile.MaximumManifestRowsPerRun < 1)
        {
            messages.Add("MaximumManifestRowsPerRun must be at least 1.");
        }

        if (profile.TargetBatchSize < 1)
        {
            messages.Add("TargetBatchSize must be at least 1.");
        }

        if (profile.MaximumBatchSize < profile.TargetBatchSize)
        {
            messages.Add("MaximumBatchSize must be greater than or equal to TargetBatchSize.");
        }

        if (profile.ExpectedHeartbeatInterval <= TimeSpan.Zero)
        {
            messages.Add("ExpectedHeartbeatInterval must be greater than zero.");
        }

        if (profile.WorkerDrainTimeout <= TimeSpan.Zero)
        {
            messages.Add("WorkerDrainTimeout must be greater than zero.");
        }

        return AzureRuntimeCapacityValidationResult.FromMessages(messages);
    }
}
