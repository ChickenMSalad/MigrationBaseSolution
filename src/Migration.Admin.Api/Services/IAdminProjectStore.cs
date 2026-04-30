using Migration.Admin.Api.Models;

namespace Migration.Admin.Api.Services;

public interface IAdminProjectStore
{
    Task<IReadOnlyList<MigrationProjectRecord>> ListProjectsAsync(CancellationToken cancellationToken = default);
    Task<MigrationProjectRecord?> GetProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<MigrationProjectRecord> SaveProjectAsync(MigrationProjectRecord project, CancellationToken cancellationToken = default);
    Task<bool> DeleteProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MigrationRunControlRecord>> ListRunsAsync(CancellationToken cancellationToken = default);
    Task<MigrationRunControlRecord?> GetRunAsync(string runId, CancellationToken cancellationToken = default);
    Task<MigrationRunControlRecord> SaveRunAsync(MigrationRunControlRecord run, CancellationToken cancellationToken = default);
}
