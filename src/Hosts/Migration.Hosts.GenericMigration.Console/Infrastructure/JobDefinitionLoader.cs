using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Migration.Domain.Models;

namespace Migration.Hosts.GenericMigration.Console.Infrastructure;

public sealed class JobDefinitionLoader
{
    private readonly IConfiguration _configuration;

    public JobDefinitionLoader(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<MigrationJobDefinition> LoadAsync(string jobFile, CancellationToken cancellationToken = default)
    {
        var root = ResolveRoot();
        var resolved = ResolvePath(jobFile, root);
        if (!File.Exists(resolved)) throw new FileNotFoundException($"Job file not found: {resolved}", resolved);

        await using var stream = File.OpenRead(resolved);
        var job = await JsonSerializer.DeserializeAsync<MigrationJobDefinition>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }, cancellationToken).ConfigureAwait(false);

        if (job is null) throw new InvalidOperationException($"Could not deserialize job file: {resolved}");
        return Normalize(job, Path.GetDirectoryName(resolved)!, root);
    }

    public string GetDefaultJobFile()
    {
        var configured = _configuration["MigrationJob:JobFile"] ?? _configuration["JobFile"];
        return !string.IsNullOrWhiteSpace(configured)
            ? ResolvePath(configured, ResolveRoot())
            : ResolvePath(Path.Combine("Profiles", "Jobs", "sample-csv-to-cloudinary.json"), AppContext.BaseDirectory);
    }

    private MigrationJobDefinition Normalize(MigrationJobDefinition job, string jobDirectory, string root)
    {
        var settings = new Dictionary<string, string?>(job.Settings, StringComparer.OrdinalIgnoreCase);
        foreach (var child in _configuration.GetSection("Settings").GetChildren()) settings[child.Key] = child.Value;
        foreach (var child in _configuration.GetSection("Secrets").GetChildren()) settings[child.Key] = child.Value;

        string? ResolveMaybePath(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            if (Path.IsPathRooted(value)) return value;
            var fromJobDir = Path.GetFullPath(Path.Combine(jobDirectory, value));
            return File.Exists(fromJobDir) ? fromJobDir : Path.GetFullPath(Path.Combine(root, value));
        }

        return new MigrationJobDefinition
        {
            JobName = job.JobName,
            SourceType = job.SourceType,
            TargetType = job.TargetType,
            ManifestType = job.ManifestType,
            MappingProfilePath = ResolveMaybePath(job.MappingProfilePath)!,
            ManifestPath = ResolveMaybePath(job.ManifestPath),
            ConnectionString = job.ConnectionString ?? _configuration["ConnectionStrings:ManifestDb"],
            QueryText = job.QueryText,
            DryRun = job.DryRun,
            Parallelism = job.Parallelism,
            Settings = settings
        };
    }

    private static string ResolvePath(string value, string basePath) => Path.IsPathRooted(value) ? value : Path.GetFullPath(Path.Combine(basePath, value));

    private static string ResolveRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MigrationBaseSolution.sln"))) return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }
}
