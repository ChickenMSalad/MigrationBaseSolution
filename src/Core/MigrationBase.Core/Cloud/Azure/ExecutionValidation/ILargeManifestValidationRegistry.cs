using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public interface ILargeManifestValidationRegistry
{
    IReadOnlyCollection<LargeManifestValidationCheck> GetChecks();

    LargeManifestValidationCheck? FindByName(string checkName);
}
