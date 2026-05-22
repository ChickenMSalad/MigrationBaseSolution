using Migration.Domain.Models;

namespace Migration.Application.Abstractions;

public interface ITransformStep
{
    Task ApplyAsync(AssetWorkItem item, CancellationToken cancellationToken = default);
}
