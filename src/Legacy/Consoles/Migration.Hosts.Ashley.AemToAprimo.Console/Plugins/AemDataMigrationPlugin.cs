using System.Text;
using Migration.Connectors.Sources.Aem.Services;
using Migration.Hosts.Ashley.AemToAprimo.Console.Infrastructure;
using Migration.Shared.Files;
using Microsoft.Extensions.Logging;

namespace Migration.Hosts.Ashley.AemToAprimo.Console.Plugins;

/// <summary>
/// Console plugin that exposes the AEM → Azure migration operations.
/// Mirrors the shape of the Crocs and WebDam Bynder plugins: direct IPlugin,
/// menu printed via ILogger, input via IConsoleReaderService, dispatch via
/// a local label+action table so display and invocation stay in sync.
/// </summary>
public sealed class AemDataMigrationPlugin(
    AemDataMigrationService service,
    IConsoleReaderService reader,
    ILogger<AemDataMigrationPlugin> logger) : IPlugin
{
    public string Name => "AEM To Azure Data Migration Tool";
    public string Description => "Executes AEM to Azure operations.";
    public int Priority => 110;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Begin AEM migration tasks.");

        var items = BuildMenuItems(cancellationToken);

        logger.LogInformation("{Menu}", RenderMenu(items));

        var choice = (await reader.ReadInputAsync())?.Trim();
        if (choice is null || !items.TryGetValue(choice, out var item))
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

        await item.Action().ConfigureAwait(false);
    }

    private static string RenderMenu(IReadOnlyDictionary<string, MenuItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("Choose the migration step:");
        sb.AppendLine();
        foreach (var (key, item) in items)
        {
            sb.AppendLine($"{key,3}: {item.Label}");
        }
        sb.AppendLine();
        sb.Append("Enter your choice:");
        return sb.ToString();
    }

    private Dictionary<string, MenuItem> BuildMenuItems(CancellationToken cancellationToken)
    {
        Task Invoke(string methodName)
            => ServiceMethodInvoker.InvokeAsync(service, methodName, cancellationToken);

        Task InvokeWith(string methodName, object fixedArg)
            => ServiceMethodInvoker.InvokeAsync(service, methodName, fixedArg, cancellationToken);

        return new Dictionary<string, MenuItem>(StringComparer.Ordinal)
        {
            ["1"]  = new("CreateAssetUploadSpreadsheets()",                   () => Invoke("CreateAssetUploadSpreadsheets")),
            ["2"]  = new("ImportAemAssetsFromUploadSpreadsheet()",            () => Invoke("ImportAemAssetsFromUploadSpreadsheet")),
            ["3"]  = new("ImportAemAssetsFromAllUnprocessedSpreadsheets()",   () => Invoke("ImportAemAssetsFromAllUnprocessedSpreadsheets")),
            ["4"]  = new("SplitLargeImportSpreadsheets()",                    () => Invoke("SplitLargeImportSpreadsheets")),
            ["5"]  = new("DoMinuteTasks()",                                   () => Invoke("DoMinuteTasks")),
            ["6"]  = new("ProcessImageSets()",                                () => Invoke("ProcessImageSets")),
            ["7"]  = new("RenameImageSets()",                                 () => Invoke("RenameImageSets")),
            ["8"]  = new("GetAllRelatedDataJson()",                           () => Invoke("GetAllRelatedDataJson")),
            ["9"]  = new("MapAssetsToImageSets()",                            () => Invoke("MapAssetsToImageSets")),
            ["10"] = new("OutputImageSetCounts()",                            () => Invoke("OutputImageSetCounts")),
            ["11"] = new("ProcessAllImageSets()",                             () => Invoke("ProcessAllImageSets")),
            ["12"] = new("ProcessAllAssets()",                                () => Invoke("ProcessAllAssets")),
            ["13"] = new("CreateBatchExcelFilesSimple()",                     () => Invoke("CreateBatchExcelFilesSimple")),
            ["14"] = new("ProcessAllRetryAssets()",                           () => Invoke("ProcessAllRetryAssets")),
            ["15"] = new("ProcessRetryImageSets()",                           () => Invoke("ProcessRetryImageSets")),
            ["16"] = new("GetAllAssetMetadata(100000)",                       () => InvokeWith("GetAllAssetMetadata", 100000)),
            ["17"] = new("MergeExcelFiles()",                                 () => Invoke("MergeExcelFiles")),
            ["18"] = new("ReProcessAllAssets()",                              () => Invoke("ReProcessAllAssets")),
            ["19"] = new("CleanCSV()",                                        () => Invoke("CleanCSV")),
            ["20"] = new("CombineExcelFiles()",                               () => Invoke("CombineExcelFiles")),
            ["21"] = new("CombineSuccessFiles()",                             () => Invoke("CombineSuccessFiles")),
            ["22"] = new("CombineMetadataFiles()",                            () => Invoke("CombineMetadataFiles")),
            ["23"] = new("MapImagesetsToAssetsCSV()",                         () => Invoke("MapImagesetsToAssetsCSV")),
            ["24"] = new("CreateExcelChunksFromDBCSV()",                      () => Invoke("CreateExcelChunksFromDBCSV")),
            ["25"] = new("TagAllAssetsInAzure()",                             () => Invoke("TagAllAssetsInAzure")),
            ["26"] = new("ImportAemAssetRenditionsDirectlyFromAzure()",       () => Invoke("ImportAemAssetRenditionsDirectlyFromAzure")),
            ["27"] = new("ImportAemAssetRenditionsDirectlyFromRetryFile()",   () => Invoke("ImportAemAssetRenditionsDirectlyFromRetryFile")),
            ["28"] = new("CreateInitialProdMappingsFile()",                   () => Invoke("CreateInitialProdMappingsFile")),
            ["29"] = new("CreateDeltaUploadSpreadsheets()",                   () => Invoke("CreateDeltaUploadSpreadsheets")),
            ["30"] = new("DetermineAemAssetsStatusFrommUploadSpreadsheet()",  () => Invoke("DetermineAemAssetsStatusFrommUploadSpreadsheet")),
            ["31"] = new("ImportDeltaAemAssetsFromUploadSpreadsheet()",       () => Invoke("ImportDeltaAemAssetsFromUploadSpreadsheet")),
            ["32"] = new("FixProdMappingsFile()",                             () => Invoke("FixProdMappingsFile")),
            ["33"] = new("GetMissingMetadataAsync()",                         () => Invoke("GetMissingMetadataAsync")),
            ["34"] = new("GetImageSetDataTestsAsync()",                       () => Invoke("GetImageSetDataTestsAsync")),
            ["35"] = new("GetImageSetPreviewsAsync()",                        () => Invoke("GetImageSetPreviewsAsync")),
            ["36"] = new("GetMissingImageSetPreviewsByPathAsync()",           () => Invoke("GetMissingImageSetPreviewsByPathAsync")),
            ["37"] = new("GetMissingMetadataSpecificAsync()",                 () => Invoke("GetMissingMetadataSpecificAsync")),
            ["38"] = new("FindDeletedAemAssetsAsync()",                       () => Invoke("FindDeletedAemAssetsAsync")),
        };
    }

    private sealed record MenuItem(string Label, Func<Task> Action);
}
