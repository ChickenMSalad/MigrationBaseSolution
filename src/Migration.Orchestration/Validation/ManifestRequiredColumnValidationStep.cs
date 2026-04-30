using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Domain.Models;
using Migration.Orchestration.Options;

namespace Migration.Orchestration.Validation;

public sealed class ManifestRequiredColumnValidationStep : IValidationStep
{
    private readonly ValidationOptions _options;

    public ManifestRequiredColumnValidationStep(IOptions<MigrationExecutionOptions> options)
    {
        _options = options.Value.Validation;
    }

    public Task<IReadOnlyList<ValidationIssue>> ValidateAsync(AssetWorkItem item, CancellationToken cancellationToken = default)
    {
        var issues = new List<ValidationIssue>();
        foreach (var column in _options.RequiredManifestColumns)
        {
            if (!item.Manifest.Columns.TryGetValue(column, out var value) || string.IsNullOrWhiteSpace(value))
            {
                issues.Add(new ValidationIssue("manifest.required_column", $"Required manifest column '{column}' is missing or empty."));
            }
        }

        return Task.FromResult<IReadOnlyList<ValidationIssue>>(issues);
    }
}
