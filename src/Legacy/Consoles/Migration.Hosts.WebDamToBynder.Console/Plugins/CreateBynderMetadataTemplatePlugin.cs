using Migration.Connectors.Targets.Bynder.Services;
using Migration.Hosts.WebDamToBynder.Console.Infrastructure;
using Migration.Shared.Files;

using Microsoft.Extensions.Logging;

namespace Migration.Hosts.WebDamToBynder.Console.Plugins;

public sealed class CreateBynderMetadataTemplatePlugin(
    BynderMetadataPropertiesService bynderMetadataService,
    BynderS3MetadataOperationsService bynderS3MetadataOperationsService,
    IConsoleReaderService reader,
    ILogger<CreateBynderMetadataTemplatePlugin> logger) : IPlugin
{
    public string Name => "Bynder Metadata Template Tool";
    public string Description => "Generate or transform metadata templates and WebDam import files.";
    public int Priority => 120;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Begin Bynder metadata template tasks.");

        logger.LogInformation("""
            Choose the template step:

            1: CreateBlankMetadataTemplate()
            2: CreateMetadataTemplateFromClientFile()
            3: PopulateMetadataTemplateFromAzure()
            4: CreateBatchExcelFiles()
            5: TestBynderAsset()
            6: MergeSuccess()
            7: CreateBetterStateCityMetadata()
            8: DeleteUnwantedAssets()
            9: GetBynderCompleteReport()
            10: ImportWebDamMetadataSchemaFromExcel()
            11: CreateBynderImportExcelFromWebDamExport()

            Enter your choice:
            """);

        var actions = new Dictionary<string, Func<Task>>
        {
            { "1", bynderMetadataService.CreateBlankMetadataTemplate },
            { "2", bynderMetadataService.CreateMetadataTemplateFromClientFile },
            { "3", bynderS3MetadataOperationsService.PopulateMetadataTemplateFromAzure },
            { "4", () => Task.Run(bynderMetadataService.CreateBatchExcelFiles) },
            { "5", bynderMetadataService.TestBynderAsset },
            { "6", () => Task.Run(bynderMetadataService.MergeSuccess) },
            { "7", () => Task.Run(bynderMetadataService.CreateBetterStateCityMetadata) },
            { "8", bynderS3MetadataOperationsService.DeleteUnwantedAssets },
            { "9", bynderS3MetadataOperationsService.GetBynderCompleteReport },
            { "10", () => bynderMetadataService.ImportWebDamMetadataSchemaFromExcel(cancellationToken) },
            { "11", () => bynderMetadataService.CreateBynderImportExcelFromWebDamExport(cancellationToken) }
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
