using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public interface IAzureRealMigrationValidationRegistry
{
    IReadOnlyCollection<AzureRealMigrationValidationScenario> GetScenarios();

    AzureRealMigrationValidationScenario? FindScenario(string scenarioName);
}
