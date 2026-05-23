using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace MigrationBase.Core.Cloud.Azure;

public sealed class AzureRuntimeOptionsValidator : IValidateOptions<AzureRuntimeOptions>
{
    public ValidateOptionsResult Validate(string? name, AzureRuntimeOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("AzureRuntime options are required.");
        }

        var failures = new List<string>();

        Require(options.Environment.EnvironmentName, "AzureRuntime:Environment:EnvironmentName", failures);
        Require(options.Environment.ApplicationName, "AzureRuntime:Environment:ApplicationName", failures);

        if (options.Environment.IsProductionLike && options.Identity.AllowDeveloperCredentialFallback)
        {
            failures.Add("Developer credential fallback must not be enabled for production-like environments.");
        }

        if (options.SqlOperationalStore.CommandTimeoutSeconds <= 0)
        {
            failures.Add("AzureRuntime:SqlOperationalStore:CommandTimeoutSeconds must be greater than zero.");
        }

        if (options.SqlOperationalStore.LongRunningCommandTimeoutSeconds < options.SqlOperationalStore.CommandTimeoutSeconds)
        {
            failures.Add("Long-running SQL command timeout must be greater than or equal to the regular command timeout.");
        }

        if (!options.ArtifactStorage.OperationalStateMustRemainInSql)
        {
            failures.Add("Operational state must remain in SQL. Artifact storage cannot be configured as the operational system of record.");
        }

        if (options.QueueTopology.MaxConcurrentWorkers <= 0)
        {
            failures.Add("AzureRuntime:QueueTopology:MaxConcurrentWorkers must be greater than zero.");
        }

        if (options.QueueTopology.LeaseRenewalSeconds >= options.QueueTopology.LeaseDurationSeconds)
        {
            failures.Add("Lease renewal interval must be less than lease duration.");
        }

        if (options.QueueTopology.MaxDeliveryAttempts <= 0)
        {
            failures.Add("AzureRuntime:QueueTopology:MaxDeliveryAttempts must be greater than zero.");
        }

        if (options.Telemetry.MetricsFlushIntervalSeconds <= 0)
        {
            failures.Add("AzureRuntime:Telemetry:MetricsFlushIntervalSeconds must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void Require(string? value, string key, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} is required.");
        }
    }
}
