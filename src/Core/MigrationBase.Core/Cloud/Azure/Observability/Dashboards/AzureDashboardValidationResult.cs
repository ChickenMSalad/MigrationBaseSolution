using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Observability.Dashboards;

public sealed class AzureDashboardValidationResult
{
    private AzureDashboardValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public bool IsValid { get; }

    public IReadOnlyList<string> Errors { get; }

    public static AzureDashboardValidationResult Success() => new(true, Array.Empty<string>());

    public static AzureDashboardValidationResult Failed(IEnumerable<string> errors)
    {
        return new AzureDashboardValidationResult(false, errors.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray());
    }
}
