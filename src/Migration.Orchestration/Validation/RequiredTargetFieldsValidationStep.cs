using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Orchestration.Validation;

public sealed class RequiredTargetFieldsValidationStep : IValidationStep
{
    public Task<IReadOnlyList<ValidationIssue>> ValidateAsync(AssetWorkItem item, CancellationToken cancellationToken = default)
    {
        var issues = new List<ValidationIssue>();
        var payload = item.TargetPayload;

        if (payload is null)
        {
            issues.Add(new ValidationIssue("target.payload_missing", "Mapper did not produce a target payload."));
            return Task.FromResult<IReadOnlyList<ValidationIssue>>(issues);
        }

        if (payload.Fields.TryGetValue("name", out var name) && string.IsNullOrWhiteSpace(name?.ToString()))
        {
            issues.Add(new ValidationIssue("target.name_empty", "Mapped target name is empty."));
        }

        return Task.FromResult<IReadOnlyList<ValidationIssue>>(issues);
    }
}
