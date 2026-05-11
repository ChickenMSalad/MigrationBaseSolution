using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Domain.Models;
using Migration.Orchestration.Options;

namespace Migration.Orchestration.Validation;

public sealed class TargetBinaryValidationStep : IValidationStep
{
    private readonly ValidationOptions _options;

    public TargetBinaryValidationStep(IOptions<MigrationExecutionOptions> options)
    {
        _options = options.Value.Validation;
    }

    public Task<IReadOnlyList<ValidationIssue>> ValidateAsync(AssetWorkItem item, CancellationToken cancellationToken = default)
    {
        if (!_options.RequireBinaryForTargetWrites)
        {
            return Task.FromResult<IReadOnlyList<ValidationIssue>>(Array.Empty<ValidationIssue>());
        }

        var binary = item.TargetPayload?.Binary ?? item.SourceAsset?.Binary;
        if (binary is null)
        {
            return Task.FromResult<IReadOnlyList<ValidationIssue>>(new[]
            {
                new ValidationIssue("target.binary_missing", "Target payload has no binary. A target write would create metadata-only or bad asset.")
            });
        }

        if (string.IsNullOrWhiteSpace(binary.SourceUri))
        {
            return Task.FromResult<IReadOnlyList<ValidationIssue>>(new[]
            {
                new ValidationIssue("target.binary_source_missing", "Target payload binary has no SourceUri/path/url.")
            });
        }

        if (binary.Length is <= 0)
        {
            return Task.FromResult<IReadOnlyList<ValidationIssue>>(new[]
            {
                new ValidationIssue("target.binary_empty", "Target payload binary length is zero.")
            });
        }

        return Task.FromResult<IReadOnlyList<ValidationIssue>>(Array.Empty<ValidationIssue>());
    }
}
