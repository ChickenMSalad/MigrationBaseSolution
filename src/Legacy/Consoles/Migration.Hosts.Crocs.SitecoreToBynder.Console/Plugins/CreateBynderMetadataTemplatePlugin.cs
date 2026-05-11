using Microsoft.Extensions.Logging;
using Migration.Connectors.Targets.Bynder.Services;
using Migration.Hosts.Crocs.SitecoreToBynder.Console.Infrastructure;
using Migration.Shared.Files;

namespace Migration.Hosts.Crocs.SitecoreToBynder.Console.Plugins;

public sealed class CreateBynderMetadataTemplatePlugin(
    BynderMetadataPropertiesService bynderMetadataService,
    IConsoleReaderService reader,
    ILogger<CreateBynderMetadataTemplatePlugin> logger) : IPlugin
{
    public string Name => "Bynder Metadata Template Tool";
    public string Description => "Generate metadata templates, import files, and Sitecore/Bynder helper artifacts.";
    public int Priority => 120;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Begin Bynder metadata template tasks.");
        logger.LogInformation("""
            Choose the template step:

            1: CreateBlankMetadataTemplate()
            2: CreateMetadataTemplateFromClientFile()
            3: CreateBatchExcelFiles()
            4: TestBynderAsset()
            5: MergeSuccess()
            6: CreateBetterStateCityMetadata()

            Enter your choice:
            """);

        var actions = new Dictionary<string, Func<Task>>
        {
            { "1", bynderMetadataService.CreateBlankMetadataTemplate },
            { "2", bynderMetadataService.CreateMetadataTemplateFromClientFile },
            { "3", () => Task.Run(bynderMetadataService.CreateBatchExcelFiles) },
            { "4", bynderMetadataService.TestBynderAsset },
            { "5", () => Task.Run(bynderMetadataService.MergeSuccess) },
            { "6", () => Task.Run(bynderMetadataService.CreateBetterStateCityMetadata) },
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
