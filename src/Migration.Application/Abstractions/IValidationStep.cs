using Migration.Domain.Models;

namespace Migration.Application.Abstractions;

public interface IValidationStep
{
    Task<IReadOnlyList<ValidationIssue>> ValidateAsync(AssetWorkItem item, CancellationToken cancellationToken = default);
}
