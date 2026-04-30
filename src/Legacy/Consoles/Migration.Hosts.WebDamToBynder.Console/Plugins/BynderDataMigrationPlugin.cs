using Migration.Connectors.Targets.Bynder.Services;
using Migration.Hosts.WebDamToBynder.Console.Infrastructure;
using Migration.Shared.Files;
using Microsoft.Extensions.Logging;

namespace Migration.Hosts.WebDamToBynder.Console.Plugins;

public sealed class BynderDataMigrationPlugin(
    BynderDataMigrationBatchService bynderDataMigrationBatchService,
    BynderWebDamMigrationService bynderWebDamMigrationService,
    BynderS3BatchOperationsService bynderS3BatchOperationsService,
    BynderS3UpdateOperationsService bynderS3UpdateOperationsService,
    BynderReportingService bynderReportingService,
    IConsoleReaderService reader,
    ILogger<BynderDataMigrationPlugin> logger) : IPlugin
{
    public string Name => "Bynder Data Migration Tool";
    public string Description => "Upload, update, report, and restamp assets in Bynder.";
    public int Priority => 130;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Begin Bynder migration tasks.");

        logger.LogInformation("""
            Choose the migration step:

            1: UploadAssetsFromFile()
            2: UpdateAssetsFromMetadata()
            3: DoReportingTasks()
            4: Test()
            5: FindDuplicatesInBynder()
            6: CombineRetryExcelFiles()
            7: DoRestampTasks()
            8: UploadAssetsFromLocal()
            9: UploadWebDamAssetsFromFile()

            Enter your choice:
            """);

        var actions = new Dictionary<string, Func<Task>>
        {
            { "1", () => bynderS3BatchOperationsService.UploadAssetsFromFile(cancellationToken) },
            { "2", () => bynderS3UpdateOperationsService.UpdateAssetsFromMetadata(cancellationToken) },
            { "3", () => bynderReportingService.DoReportingTasks(cancellationToken) },
            { "4", bynderS3BatchOperationsService.Test },
            { "5", bynderReportingService.FindDuplicatesInBynder },
            { "6", () => Task.Run(bynderReportingService.CombineRetryExcelFiles) },
            { "7", () => bynderS3UpdateOperationsService.DoRestampTasks(cancellationToken) },
            { "8", () => bynderDataMigrationBatchService.UploadAssetsFromLocal(cancellationToken) },
            { "9", () => bynderWebDamMigrationService.UploadWebDamAssetsFromFile(cancellationToken) }
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
