using System.Text.Json;
using Microsoft.Extensions.Options;
using Migration.Orchestration.Abstractions;
using Migration.Orchestration.Options;

namespace Migration.Orchestration.State;

public sealed class JsonFileMigrationExecutionStateStore : IMigrationExecutionStateStore, IMigrationExecutionStateMaintenance
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _stateRoot;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonFileMigrationExecutionStateStore(IOptions<MigrationExecutionOptions> options)
    {
        _stateRoot = Path.GetFullPath(options.Value.StatePath ?? "Runtime/migration-state");
        Directory.CreateDirectory(_stateRoot);
    }

    public Task StartRunAsync(MigrationRunRecord run, CancellationToken cancellationToken = default) => SaveRunAsync(run, cancellationToken);

    public Task CompleteRunAsync(MigrationRunRecord run, CancellationToken cancellationToken = default) => SaveRunAsync(run, cancellationToken);

    public async Task SaveWorkItemAsync(MigrationWorkItemState state, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = GetWorkItemFile(state.JobName, state.WorkItemId);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            await using var stream = File.Create(file);
            await JsonSerializer.SerializeAsync(stream, state, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<MigrationWorkItemState?> GetWorkItemAsync(string jobName, string workItemId, CancellationToken cancellationToken = default)
    {
        var file = GetWorkItemFile(jobName, workItemId);
        if (!File.Exists(file))
        {
            return null;
        }

        await using var stream = File.OpenRead(file);
        return await JsonSerializer.DeserializeAsync<MigrationWorkItemState>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MigrationWorkItemState>> ListWorkItemsAsync(string jobName, CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(_stateRoot, Safe(jobName), "work-items");
        if (!Directory.Exists(folder))
        {
            return Array.Empty<MigrationWorkItemState>();
        }

        var states = new List<MigrationWorkItemState>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var stream = File.OpenRead(file);
            var state = await JsonSerializer.DeserializeAsync<MigrationWorkItemState>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
            if (state is not null)
            {
                states.Add(state);
            }
        }

        return states.OrderBy(x => x.WorkItemId, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task ResetJobAsync(string jobName, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var folder = Path.Combine(_stateRoot, Safe(jobName));
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveRunAsync(MigrationRunRecord run, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var file = Path.Combine(_stateRoot, Safe(run.JobName), run.RunId, "run.json");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            await using var stream = File.Create(file);
            await JsonSerializer.SerializeAsync(stream, run, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetWorkItemFile(string jobName, string workItemId) =>
        Path.Combine(_stateRoot, Safe(jobName), "work-items", $"{Safe(workItemId)}.json");

    private static string Safe(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '_');
        }
        return value;
    }
}
