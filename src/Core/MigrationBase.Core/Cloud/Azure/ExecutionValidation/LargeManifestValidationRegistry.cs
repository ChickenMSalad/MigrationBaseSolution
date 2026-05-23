using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public sealed class LargeManifestValidationRegistry : ILargeManifestValidationRegistry
{
    private readonly IReadOnlyCollection<LargeManifestValidationCheck> checks;

    public LargeManifestValidationRegistry(IEnumerable<LargeManifestValidationCheck> checks)
    {
        this.checks = (checks ?? Array.Empty<LargeManifestValidationCheck>()).ToArray();
    }

    public IReadOnlyCollection<LargeManifestValidationCheck> GetChecks() => checks;

    public LargeManifestValidationCheck? FindByName(string checkName)
    {
        if (string.IsNullOrWhiteSpace(checkName))
        {
            return null;
        }

        return checks.FirstOrDefault(check => string.Equals(check.CheckName, checkName, StringComparison.OrdinalIgnoreCase));
    }
}
