using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

/// <summary>
/// In-memory registry for real migration validation scenario descriptors.
/// Durable execution state remains SQL-first; this registry only describes validation intent.
/// </summary>
public sealed class AzureRealMigrationValidationRegistry : IAzureRealMigrationValidationRegistry
{
    private readonly IReadOnlyCollection<AzureRealMigrationValidationScenario> scenarios;

    public AzureRealMigrationValidationRegistry(IEnumerable<AzureRealMigrationValidationScenario> scenarios)
    {
        if (scenarios is null)
        {
            throw new ArgumentNullException(nameof(scenarios));
        }

        this.scenarios = scenarios
            .Where(scenario => scenario is not null)
            .ToArray();
    }

    public IReadOnlyCollection<AzureRealMigrationValidationScenario> GetScenarios() => scenarios;

    public AzureRealMigrationValidationScenario? FindScenario(string scenarioName)
    {
        if (string.IsNullOrWhiteSpace(scenarioName))
        {
            return null;
        }

        return scenarios.FirstOrDefault(scenario =>
            string.Equals(scenario.ScenarioName, scenarioName, StringComparison.OrdinalIgnoreCase));
    }
}
