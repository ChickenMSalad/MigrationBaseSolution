using System.Text.Json;
using Microsoft.Extensions.Options;
using Migration.ControlPlane.Models;
using Migration.ControlPlane.Options;

namespace Migration.ControlPlane.Services;

public sealed class FileBackedCredentialSetStore : ICredentialSetStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _credentialsRoot;

    public FileBackedCredentialSetStore(IOptions<AdminApiOptions> options)
    {
        var root = options.Value.StorageRoot;
        _credentialsRoot = Path.Combine(root, "credentials");
        Directory.CreateDirectory(_credentialsRoot);
    }

    public async Task<IReadOnlyList<CredentialSetRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_credentialsRoot)) return Array.Empty<CredentialSetRecord>();

        var records = new List<CredentialSetRecord>();
        foreach (var file in Directory.EnumerateFiles(_credentialsRoot, "*.json"))
        {
            await using var stream = File.OpenRead(file);
            var record = await JsonSerializer.DeserializeAsync<CredentialSetRecord>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (record is not null) records.Add(record);
        }

        return records.OrderByDescending(x => x.UpdatedUtc).ToArray();
    }

    public async Task<CredentialSetRecord?> GetAsync(string credentialSetId, CancellationToken cancellationToken = default)
    {
        var path = GetPath(credentialSetId);
        if (!File.Exists(path)) return null;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<CredentialSetRecord>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(CredentialSetRecord credentialSet, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_credentialsRoot);
        var path = GetPath(credentialSet.CredentialSetId);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, credentialSet, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> DeleteAsync(string credentialSetId, CancellationToken cancellationToken = default)
    {
        var path = GetPath(credentialSetId);
        if (!File.Exists(path)) return Task.FromResult(false);
        File.Delete(path);
        return Task.FromResult(true);
    }

    private string GetPath(string credentialSetId)
    {
        var safe = string.Join("_", credentialSetId.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return Path.Combine(_credentialsRoot, $"{safe}.json");
    }
}
