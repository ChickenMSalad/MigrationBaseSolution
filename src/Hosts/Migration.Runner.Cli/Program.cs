using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Migration.Application.Abstractions;
using Migration.Application.Services;
using Migration.Connectors.Sources.Aem;
using Migration.Connectors.Sources.AzureBlob;
using Migration.Connectors.Sources.S3;
using Migration.Connectors.Sources.SharePoint;
using Migration.Connectors.Sources.Sitecore;
using Migration.Connectors.Sources.WebDam;
using Migration.Connectors.Targets.Aprimo;
using Migration.Connectors.Targets.AzureBlob;
using Migration.Connectors.Targets.Bynder;
using Migration.Connectors.Targets.Cloudinary;
using Migration.Domain.Models;
using Migration.Infrastructure.Mapping;
using Migration.Infrastructure.Profiles;
using Migration.Infrastructure.State;
using Migration.Infrastructure.Validation;
using Migration.Manifest.Csv;
using Migration.Manifest.Excel;
using Migration.Manifest.Sql;
using Migration.Manifest.Sqlite;

var root = ResolveRoot();
var configuration = new ConfigurationBuilder()
    .SetBasePath(root)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddJsonFile("secrets.json", optional: true)
    .AddEnvironmentVariables(prefix: "MIGRATION_")
    .Build();

var jobFile = args.Length > 0 ? args[0] : configuration["JobFile"] ?? Path.Combine(root, "profiles", "jobs", "webdam-to-bynder.sample.json");
if (!Path.IsPathRooted(jobFile))
{
    jobFile = Path.GetFullPath(Path.Combine(root, jobFile));
}

if (!File.Exists(jobFile))
{
    Console.Error.WriteLine($"Job file not found: {jobFile}");
    return 2;
}

var job = await LoadJobAsync(jobFile);
job = NormalizeJob(job, root, configuration);

var orchestrator = new MigrationOrchestrator(
    new IManifestProvider[]
    {
        new CsvManifestProvider(),
        new ExcelManifestProvider(),
        new SqlManifestProvider(),
        new SqliteManifestProvider()
    },
    new IAssetSourceConnector[]
    {
        new AemSourceConnector(),
        new SitecoreSourceConnector(),
        new WebDamSourceConnector(),
        new AzureBlobSourceConnector(),
        new S3SourceConnector(),
        new SharePointSourceConnector()
    },
    new IAssetTargetConnector[]
    {
        //new BynderTargetConnector(),
        new AprimoTargetConnector(),
        //new AzureBlobTargetConnector(),
        new CloudinaryTargetConnector()
    },
    new JsonMappingProfileLoader(),
    new CanonicalMapper(),
    Array.Empty<ITransformStep>(),
    new IValidationStep[] { new RequiredFieldValidationStep() },
    new InMemoryJobStateStore());

Console.WriteLine($"Running job '{job.JobName}'");
Console.WriteLine($"Source: {job.SourceType}  Target: {job.TargetType}  Manifest: {job.ManifestType}");
Console.WriteLine($"Mapping profile: {job.MappingProfilePath}");
Console.WriteLine($"Manifest path: {job.ManifestPath ?? "(not set)"}");
Console.WriteLine($"Dry run: {job.DryRun}");

var results = await orchestrator.RunAsync(job);
var success = results.Count(x => x.Success);
var failed = results.Count - success;
Console.WriteLine($"Completed {results.Count} work items. Succeeded: {success}. Failed: {failed}.");

if (failed > 0)
{
    foreach (var result in results.Where(x => !x.Success))
    {
        Console.WriteLine($"[FAILED] {result.WorkItemId}: {result.Message}");
    }
}

return failed > 0 ? 1 : 0;

static async Task<MigrationJobDefinition> LoadJobAsync(string jobFile)
{
    await using var stream = File.OpenRead(jobFile);
    var job = await JsonSerializer.DeserializeAsync<MigrationJobDefinition>(stream, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    return job ?? throw new InvalidOperationException($"Could not deserialize job file: {jobFile}");
}

static MigrationJobDefinition NormalizeJob(MigrationJobDefinition job, string root, IConfiguration configuration)
{
    string? ResolvePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return Path.IsPathRooted(value) ? value : Path.GetFullPath(Path.Combine(root, value));
    }

    var settings = new Dictionary<string, string?>(job.Settings, StringComparer.OrdinalIgnoreCase);
    foreach (var child in configuration.GetSection("Settings").GetChildren())
    {
        settings[child.Key] = child.Value;
    }

    return new MigrationJobDefinition
    {
        JobName = job.JobName,
        SourceType = job.SourceType,
        TargetType = job.TargetType,
        ManifestType = job.ManifestType,
        MappingProfilePath = ResolvePath(job.MappingProfilePath)!,
        ManifestPath = ResolvePath(job.ManifestPath),
        ConnectionString = job.ConnectionString ?? configuration["ConnectionStrings:ManifestDb"],
        QueryText = job.QueryText,
        DryRun = job.DryRun,
        Parallelism = job.Parallelism,
        Settings = settings
    };
}

static string ResolveRoot()
{
    var current = AppContext.BaseDirectory;
    var dir = new DirectoryInfo(current);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "MigrationBaseSolution.sln")))
        {
            return dir.FullName;
        }
        dir = dir.Parent;
    }

    return Directory.GetCurrentDirectory();
}
