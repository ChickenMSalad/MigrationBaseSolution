using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Infrastructure.Validation;

public sealed class RequiredFieldValidationStep : IValidationStep
{
    public Task<IReadOnlyList<ValidationIssue>> ValidateAsync(AssetWorkItem item, CancellationToken cancellationToken = default)
    {
        var issues = new List<ValidationIssue>();
        if (item.TargetPayload is null)
        {
            issues.Add(new ValidationIssue("payload.missing", "Target payload is missing."));
            return Task.FromResult<IReadOnlyList<ValidationIssue>>(issues);
        }

        foreach (var kvp in item.TargetPayload.Fields)
        {
            if (kvp.Value is null)
            {
                continue;
            }
        }

        return Task.FromResult<IReadOnlyList<ValidationIssue>>(issues);
    }
}
