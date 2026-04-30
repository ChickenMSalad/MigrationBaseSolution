using System.Text.Json;
using Microsoft.Extensions.Options;
using Migration.Admin.Api.Models;
using Migration.Admin.Api.Options;

namespace Migration.Admin.Api.Services;

public sealed class FileBackedAdminProjectStore : IAdminProjectStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _projectsDirectory;
    private readonly string _runsDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileBackedAdminProjectStore(IOptions<AdminApiOptions> options)
    {
        var root = string.IsNullOrWhiteSpace(options.Value.StorageRoot) ? "Runtime/admin-api" : options.Value.StorageRoot;
        _projectsDirectory = Path.Combine(root, "projects");
        _runsDirectory = Path.Combine(root, "runs");
        Directory.CreateDirectory(_projectsDirectory);
        Directory.CreateDirectory(_runsDirectory);
    }

    public Task<IReadOnlyList<MigrationProjectRecord>> ListProjectsAsync(CancellationToken cancellationToken = default) => ReadAllAsync<MigrationProjectRecord>(_projectsDirectory, cancellationToken);
    public Task<MigrationProjectRecord?> GetProjectAsync(string projectId, CancellationToken cancellationToken = default) => ReadAsync<MigrationProjectRecord>(Path.Combine(_projectsDirectory, SafeFileName(projectId) + ".json"), cancellationToken);
    public async Task<MigrationProjectRecord> SaveProjectAsync(MigrationProjectRecord project, CancellationToken cancellationToken = default) { await WriteAsync(Path.Combine(_projectsDirectory, SafeFileName(project.ProjectId) + ".json"), project, cancellationToken); return project; }

    public async Task<bool> DeleteProjectAsync(string projectId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = Path.Combine(_projectsDirectory, SafeFileName(projectId) + ".json");
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<MigrationRunControlRecord>> ListRunsAsync(CancellationToken cancellationToken = default)
    {
        var runs = await ReadAllAsync<MigrationRunControlRecord>(_runsDirectory, cancellationToken);
        return runs.OrderByDescending(x => x.CreatedUtc).ToList();
    }

    public Task<MigrationRunControlRecord?> GetRunAsync(string runId, CancellationToken cancellationToken = default) => ReadAsync<MigrationRunControlRecord>(Path.Combine(_runsDirectory, SafeFileName(runId) + ".json"), cancellationToken);
    public async Task<MigrationRunControlRecord> SaveRunAsync(MigrationRunControlRecord run, CancellationToken cancellationToken = default) { await WriteAsync(Path.Combine(_runsDirectory, SafeFileName(run.RunId) + ".json"), run, cancellationToken); return run; }

    private async Task<IReadOnlyList<T>> ReadAllAsync<T>(string directory, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!Directory.Exists(directory)) return Array.Empty<T>();
            var results = new List<T>();
            foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var stream = File.OpenRead(file);
                var item = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
                if (item is not null) results.Add(item);
            }
            return results;
        }
        finally { _gate.Release(); }
    }

    private async Task<T?> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(path)) return default;
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    private async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tempPath = path + ".tmp";
            await using (var stream = File.Create(tempPath))
                await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
            File.Move(tempPath, path, overwrite: true);
        }
        finally { _gate.Release(); }
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) ? '-' : c).ToArray());
    }
}
