using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Deployment;

public static class AzureDeploymentEnvironmentManifestValidator
{
    public static AzureDeploymentEnvironmentManifestValidationResult Validate(AzureDeploymentEnvironmentManifest? manifest)
    {
        if (manifest is null)
        {
            return AzureDeploymentEnvironmentManifestValidationResult.Failure(new[] { "Deployment environment manifest is required." });
        }

        var errors = new List<string>();
        AddRequired(errors, manifest.EnvironmentName, nameof(manifest.EnvironmentName));
        AddRequired(errors, manifest.DeploymentRing, nameof(manifest.DeploymentRing));
        AddRequired(errors, manifest.AzureTenantBoundary, nameof(manifest.AzureTenantBoundary));
        AddRequired(errors, manifest.SubscriptionAlias, nameof(manifest.SubscriptionAlias));
        AddRequired(errors, manifest.ResourceGroupName, nameof(manifest.ResourceGroupName));
        AddRequired(errors, manifest.Region, nameof(manifest.Region));
        AddRequired(errors, manifest.SqlOperationalStoreProfile, nameof(manifest.SqlOperationalStoreProfile));
        AddRequired(errors, manifest.StorageProfile, nameof(manifest.StorageProfile));
        AddRequired(errors, manifest.QueueProfile, nameof(manifest.QueueProfile));
        AddRequired(errors, manifest.TelemetryProfile, nameof(manifest.TelemetryProfile));

        if (manifest.Hosts.Count == 0)
        {
            errors.Add("At least one host manifest is required.");
        }

        foreach (var host in manifest.Hosts)
        {
            AddRequired(errors, host.HostName, $"Host.{nameof(host.HostName)}");
            AddRequired(errors, host.HostRole, $"Host.{nameof(host.HostRole)}");
            AddRequired(errors, host.DeploymentTargetProfile, $"Host.{nameof(host.DeploymentTargetProfile)}");
            AddRequired(errors, host.CapacityProfile, $"Host.{nameof(host.CapacityProfile)}");
            AddRequired(errors, host.ExecutionIsolationProfile, $"Host.{nameof(host.ExecutionIsolationProfile)}");
        }

        var duplicateHosts = manifest.Hosts
            .Where(static h => !string.IsNullOrWhiteSpace(h.HostName))
            .GroupBy(static h => h.HostName, StringComparer.OrdinalIgnoreCase)
            .Where(static g => g.Count() > 1)
            .Select(static g => $"Duplicate host manifest: {g.Key}")
            .ToArray();
        errors.AddRange(duplicateHosts);

        return errors.Count == 0
            ? AzureDeploymentEnvironmentManifestValidationResult.Success()
            : AzureDeploymentEnvironmentManifestValidationResult.Failure(errors);
    }

    private static void AddRequired(ICollection<string> errors, string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{name} is required.");
        }
    }
}
