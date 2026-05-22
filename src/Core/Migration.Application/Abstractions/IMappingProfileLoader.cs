using Migration.Application.Models;

namespace Migration.Application.Abstractions;

public interface IMappingProfileLoader
{
    Task<MappingProfile> LoadAsync(string path, CancellationToken cancellationToken = default);
}
