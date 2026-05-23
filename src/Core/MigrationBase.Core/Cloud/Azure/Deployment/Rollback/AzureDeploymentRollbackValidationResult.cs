using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Deployment.Rollback;

public sealed class AzureDeploymentRollbackValidationResult
{
    private AzureDeploymentRollbackValidationResult(bool isValid, IReadOnlyCollection<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public bool IsValid { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public static AzureDeploymentRollbackValidationResult Success() => new(true, Array.Empty<string>());

    public static AzureDeploymentRollbackValidationResult Failed(IEnumerable<string> errors)
    {
        var materialized = errors.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray();
        return materialized.Length == 0 ? Success() : new AzureDeploymentRollbackValidationResult(false, materialized);
    }
}
