using System.Text.Json;
using Microsoft.Extensions.Options;
using Migration.ControlPlane.Models;
using Migration.ControlPlane.Options;

namespace Migration.ControlPlane.Services;

public sealed class FileBackedRunMonitoringStore : IRunMonitoringStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _eventsDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileBackedRunMonitoringStore(IOptions<AdminApiOptions> options)
    {
        var root = string.IsNullOrWhiteSpace(options.Value.StorageRoot) ? "Runtime/admin-api" : options.Value.StorageRoot;
        _eventsDirectory = Path.Combine(root, "events");
        Directory.CreateDirectory(_eventsDirectory);
    }

    public async Task SaveEventAsync(RunProgressEventRecord progressEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(progressEvent);

        var runDirectory = GetRunDirectory(progressEvent.RunId);
        var fileName = $"{progressEvent.TimestampUtc:yyyyMMddHHmmssfffffff}_{SafeFileName(progressEvent.EventId)}.json";
        var path = Path.Combine(runDirectory, fileName);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(runDirectory);
            var tempPath = path + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, progressEvent, JsonOptions, cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<RunProgressEventRecord>> ListEventsAsync(string runId, int take = 500, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            return Array.Empty<RunProgressEventRecord>();
        }

        take = Math.Clamp(take, 1, 5000);
        var runDirectory = GetRunDirectory(runId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!Directory.Exists(runDirectory))
            {
                return Array.Empty<RunProgressEventRecord>();
            }

            var results = new List<RunProgressEventRecord>();
            foreach (var file in Directory.EnumerateFiles(runDirectory, "*.json", SearchOption.TopDirectoryOnly)
                         .OrderByDescending(Path.GetFileName)
                         .Take(take))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var stream = File.OpenRead(file);
                var item = await JsonSerializer.DeserializeAsync<RunProgressEventRecord>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
                if (item is not null)
                {
                    results.Add(item);
                }
            }

            return results.OrderBy(x => x.TimestampUtc).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteEventsAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            return;
        }

        var runDirectory = GetRunDirectory(runId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Directory.Exists(runDirectory))
            {
                Directory.Delete(runDirectory, recursive: true);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private string GetRunDirectory(string runId) => Path.Combine(_eventsDirectory, SafeFileName(runId));

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) ? '-' : c).ToArray());
    }
}
