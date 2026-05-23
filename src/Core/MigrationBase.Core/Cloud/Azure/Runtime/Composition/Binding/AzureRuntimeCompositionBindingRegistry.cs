using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition.Binding;

public sealed class AzureRuntimeCompositionBindingRegistry : IAzureRuntimeCompositionBindingRegistry
{
    private readonly List<AzureRuntimeCompositionBinding> _bindings = new()
    {
        new AzureRuntimeCompositionBinding
        {
            Key = "configuration.azure-runtime",
            DisplayName = "Azure runtime configuration",
            Kind = AzureRuntimeCompositionBindingKind.Configuration,
            Requirement = AzureRuntimeCompositionBindingRequirement.Required,
            ConfigurationSection = "AzureRuntime",
            Description = "Binds runtime topology and environment configuration used by Azure-hosted migration components."
        },
        new AzureRuntimeCompositionBinding
        {
            Key = "store.sql-operational",
            DisplayName = "SQL operational store",
            Kind = AzureRuntimeCompositionBindingKind.OperationalStore,
            Requirement = AzureRuntimeCompositionBindingRequirement.Required,
            ConfigurationSection = "AzureRuntime:Sql",
            Description = "Represents the durable SQL-first operational model for manifests, work items, leases, failures, and run state."
        },
        new AzureRuntimeCompositionBinding
        {
            Key = "telemetry.observability",
            DisplayName = "Operational telemetry",
            Kind = AzureRuntimeCompositionBindingKind.Telemetry,
            Requirement = AzureRuntimeCompositionBindingRequirement.RequiredForProduction,
            ConfigurationSection = "AzureRuntime:Telemetry",
            Description = "Represents correlation, logs, metrics, health signals, alerts, and dashboard readiness."
        }
    };

    public IReadOnlyCollection<AzureRuntimeCompositionBinding> GetBindings()
    {
        return _bindings;
    }

    public AzureRuntimeCompositionBindingValidationResult Validate()
    {
        var result = new AzureRuntimeCompositionBindingValidationResult();

        foreach (var binding in _bindings)
        {
            if (!binding.IsValid())
            {
                result.AddError($"Invalid runtime composition binding: {binding.Key}");
            }
        }

        var duplicateKeys = _bindings
            .GroupBy(binding => binding.Key)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (var duplicateKey in duplicateKeys)
        {
            result.AddError($"Duplicate runtime composition binding key: {duplicateKey}");
        }

        return result;
    }
}
