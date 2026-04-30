using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Configuration.Workflows;
using Migration.Connectors.Sources.Sitecore.Clients;
using Migration.Connectors.Sources.Sitecore.Services;
using Migration.Hosts.Crocs.SitecoreToBynder.Console.Infrastructure;
using Migration.Shared.Runtime;
using Migration.Shared.Configuration.Hosts.Sitecore;
using Migration.Shared.Files;

namespace Migration.Hosts.Crocs.SitecoreToBynder.Console.Plugins;

public sealed class SitecoreMigrationPlugin(
    ContentHubDataMigrationService contentHubDataMigrationService,
    INodeBynderAssetClient nodeBynderAssetClient,
    SitecoreHostPathResolver pathResolver,
    IOptions<SitecoreHostOptions> hostOptions,
    IOptions<SitecoreToBynderWorkflowOptions> workflowOptions,
    IConsoleReaderService reader,
    ILogger<SitecoreMigrationPlugin> logger) : IPlugin
{
    private readonly SitecoreHostOptions _hostOptions = hostOptions.Value;
    private readonly SitecoreToBynderWorkflowOptions _workflow = workflowOptions.Value;

    public string Name => "Sitecore Export Tool";
    public string Description => "Pull assets, metadata, and modified snapshots out of Sitecore/Content Hub.";
    public int Priority => 100;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Begin Sitecore export and staging tasks.");
        logger.LogInformation("""
            Choose a step:

            1: Process batches listed in a file into Azure
            2: Get last modified asset IDs from Content Hub
            3: Create batch Excel files
            4: Export node-based Bynder asset snapshot (JSON)
            5: Export node-based Bynder flat rows (JSON)

            Enter your choice:
            """);

        var actions = new Dictionary<string, Func<Task>>
        {
            { "1", () => ProcessBatchesIntoAzureAsync(cancellationToken) },
            { "2", async () => { var count = await contentHubDataMigrationService.GetLastModifiedAssetIdsFromContentHub(_hostOptions.Batch.BatchSize).ConfigureAwait(false); logger.LogInformation("Retrieved {Count} last-modified asset ids.", count); } },
            { "3", contentHubDataMigrationService.CreateBatchExcelFiles },
            { "4", () => ExportNodeAssetsAsync(cancellationToken) },
            { "5", () => ExportNodeFlatRowsAsync(cancellationToken) }
        };

        var choice = (await reader.ReadInputAsync())?.Trim();
        if (choice is null || !actions.TryGetValue(choice, out var action))
        {
            logger.LogWarning("Invalid choice.");
            return;
        }

        logger.LogInformation("Are you sure? (y/n)");
        var confirmation = (await reader.ReadInputAsync())?.Trim().ToLowerInvariant();
        if (confirmation != "y")
        {
            logger.LogInformation("Operation canceled.");
            return;
        }

        await action().ConfigureAwait(false);
    }

    private async Task ProcessBatchesIntoAzureAsync(CancellationToken cancellationToken)
    {
        var batchFile = _hostOptions.Files.BatchBlobListingFile ?? "contenthub-batches.txt";
        var candidatePath = Path.IsPathRooted(batchFile) ? batchFile : pathResolver.GetSourceFile(batchFile);

        logger.LogInformation("Using batch listing file {BatchFile}", candidatePath);
        await using var stream = File.OpenRead(candidatePath);
        await contentHubDataMigrationService.ProcessBatchesIntoAzureAsync(stream, _hostOptions.Batch.BatchSize).ConfigureAwait(false);
    }

    private async Task ExportNodeAssetsAsync(CancellationToken cancellationToken)
    {
        var modifiedAfter = ResolveModifiedAfter();
        var assets = await nodeBynderAssetClient.GetAssetsModifiedAfterAsync(modifiedAfter, cancellationToken).ConfigureAwait(false);
        var fileName = _hostOptions.Files.NodeAssetsOutputFile ?? "sitecore-node-assets.json";
        var outputPath = Path.IsPathRooted(fileName) ? fileName : pathResolver.GetOutputFile(fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(assets, new JsonSerializerOptions { WriteIndented = true }), cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Wrote {Count} node asset records to {OutputPath}", assets.Count, outputPath);
    }

    private async Task ExportNodeFlatRowsAsync(CancellationToken cancellationToken)
    {
        var modifiedAfter = ResolveModifiedAfter();
        var rows = await nodeBynderAssetClient.GetFlatRowsModifiedAfterAsync(modifiedAfter, cancellationToken).ConfigureAwait(false);
        var fileName = _hostOptions.Files.NodeFlatRowsOutputFile ?? "sitecore-node-flat-rows.json";
        var outputPath = Path.IsPathRooted(fileName) ? fileName : pathResolver.GetOutputFile(fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }), cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Wrote {Count} flat rows to {OutputPath}", rows.Count, outputPath);
    }

    private DateTimeOffset ResolveModifiedAfter()
    {
        if (DateTimeOffset.TryParse(_workflow.ModifiedAfterUtc, out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow.AddDays(-1);
    }
}
