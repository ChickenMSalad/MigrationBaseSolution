using Microsoft.Extensions.Logging;
using Migration.Connectors.Targets.Bynder.Services;
using Migration.Hosts.Crocs.SitecoreToBynder.Console.Infrastructure;
using Migration.Shared.Files;

namespace Migration.Hosts.Crocs.SitecoreToBynder.Console.Plugins;

public sealed class BynderDataMigrationPlugin(
    BynderDataMigrationBatchService bynderDataMigrationBatchService,
    BynderUpdateDataService bynderUpdateDataService,
    BynderReportingService bynderReportingService,
    IConsoleReaderService reader,
    ILogger<BynderDataMigrationPlugin> logger) : IPlugin
{
    public string Name => "Bynder Data Migration Tool";
    public string Description => "Upload, update, and report on assets in Bynder for the Crocs flow.";
    public int Priority => 130;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Begin Bynder migration tasks.");
        logger.LogInformation("""
            Choose the migration step:

            1: UploadAssetsFromMetadata()
            2: UpdateAssetsFromMetadata()
            3: DoReportingTasks()
            4: FindDuplicatesInBynder()
            5: CombineRetryExcelFiles()
            6: UploadAssetsFromLocal()

            Enter your choice:
            """);

        var actions = new Dictionary<string, Func<Task>>
        {
            { "1", () => bynderDataMigrationBatchService.UploadAssetsFromMetadata(cancellationToken) },
            { "2", () => bynderUpdateDataService.UpdateAssetsFromMetadata(cancellationToken) },
            { "3", () => bynderReportingService.DoReportingTasks(cancellationToken) },
            { "4", bynderReportingService.FindDuplicatesInBynder },
            { "5", () => Task.Run(bynderReportingService.CombineRetryExcelFiles) },
            { "6", () => bynderDataMigrationBatchService.UploadAssetsFromLocal(cancellationToken) }
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
}
