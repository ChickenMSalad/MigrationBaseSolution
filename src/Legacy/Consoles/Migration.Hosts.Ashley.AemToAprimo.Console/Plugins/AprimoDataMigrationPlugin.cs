using System.Text;
using Migration.Connectors.Targets.Aprimo.Services;
using Migration.Hosts.Ashley.AemToAprimo.Console.Infrastructure;
using Migration.Shared.Files;
using Microsoft.Extensions.Logging;

namespace Migration.Hosts.Ashley.AemToAprimo.Console.Plugins;

/// <summary>
/// Console plugin that exposes the Azure → Aprimo migration operations.
/// Mirrors the shape of the Crocs and WebDam Bynder plugins: direct IPlugin,
/// menu printed via ILogger, input via IConsoleReaderService, dispatch via
/// a local label+action table so display and invocation stay in sync.
/// </summary>
public sealed class AprimoDataMigrationPlugin(
    AprimoDataMigrationService service,
    IConsoleReaderService reader,
    ILogger<AprimoDataMigrationPlugin> logger) : IPlugin
{
    public string Name => "Azure To Aprimo Data Migration Tool";
    public string Description => "Executes Azure to Aprimo operations.";
    public int Priority => 120;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Begin Aprimo migration tasks.");

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

        return new Dictionary<string, MenuItem>(StringComparer.Ordinal)
        {
            ["1"]  = new("TestImportBinariesToAprimo()",                 () => Invoke("TestImportBinariesToAprimo")),
            ["2"]  = new("ImportAemAssetsFromUploadSpreadsheet()",       () => Invoke("ImportAemAssetsFromUploadSpreadsheet")),
            ["3"]  = new("DoMinuteTasks()",                              () => Invoke("DoMinuteTasks")),
            ["4"]  = new("FindDuplicatesInAprimo()",                     () => Invoke("FindDuplicatesInAprimo")),
            ["5"]  = new("StampAssetsInAprimo()",                        () => Invoke("StampAssetsInAprimo")),
            ["6"]  = new("ImportAemAssetsDirectlyFromAzure()",           () => Invoke("ImportAemAssetsDirectlyFromAzure")),
            ["7"]  = new("CreateAllImageSetsJSONFromAzure()",            () => Invoke("CreateAllImageSetsJSONFromAzure")),
            ["8"]  = new("ImportAemImagesetsDirectlyFromFile()",         () => Invoke("ImportAemImagesetsDirectlyFromFile")),
            ["9"]  = new("Tests()",                                      () => Invoke("Tests")),
            ["10"] = new("CreateAssetsToImageSets()",                    () => Invoke("CreateAssetsToImageSets")),
            ["11"] = new("TestGLB()",                                    () => Invoke("TestGLB")),
            ["12"] = new("UpdateGLBsInAprimoFromFile()",                 () => Invoke("UpdateGLBsInAprimoFromFile")),
            ["13"] = new("UpdateGLBsInAprimoFromAzure()",                () => Invoke("UpdateGLBsInAprimoFromAzure")),
            ["14"] = new("CreateAllImageSetData()",                      () => Invoke("CreateAllImageSetData")),
            ["15"] = new("CombineFiles()",                               () => Invoke("CombineFiles")),
            ["16"] = new("IngestMappingHelperObjects()",                 () => Invoke("IngestMappingHelperObjects")),
            ["17"] = new("PopulateImageSetAssets()",                     () => Invoke("PopulateImageSetAssets")),
            ["18"] = new("DigestDelta()",                                () => Invoke("DigestDelta")),
            ["19"] = new("StampAllAssetsInAprimo()",                     () => Invoke("StampAllAssetsInAprimo")),
            ["20"] = new("TestAdditionalFileGLB()",                      () => Invoke("TestAdditionalFileGLB")),
            ["21"] = new("CreateImageSetsPreviewsInAprimoFromFile()",    () => Invoke("CreateImageSetsPreviewsInAprimoFromFile")),
            ["22"] = new("CreateAllImageSetsInAprimo()",                 () => Invoke("CreateAllImageSetsInAprimo")),
            ["23"] = new("CreateImageSetsPreviewsInAprimoFromBatch()",   () => Invoke("CreateImageSetsPreviewsInAprimoFromBatch")),
            ["24"] = new("TestStampAssetsInAprimo()",                    () => Invoke("TestStampAssetsInAprimo")),
            ["25"] = new("FixMissingRelations()",                        () => Invoke("FixMissingRelations")),
            ["26"] = new("FixMissingRelationsInMHO()",                   () => Invoke("FixMissingRelationsInMHO")),
            ["27"] = new("ImportAemAssetDirectlyFromAzure()",            () => Invoke("ImportAemAssetDirectlyFromAzure")),
            ["28"] = new("FixMissingImageSetCountsInMHO()",              () => Invoke("FixMissingImageSetCountsInMHO")),
            ["29"] = new("FindPossibleMetadataProperties()",             () => Invoke("FindPossibleMetadataProperties")),
            ["30"] = new("CreateAllFullImageSetsJSONFromAzure()",        () => Invoke("CreateAllFullImageSetsJSONFromAzure")),
            ["31"] = new("CreateAllFullMetadataJSONFromAzure()",         () => Invoke("CreateAllFullMetadataJSONFromAzure")),
            ["32"] = new("StampAssetsInAprimoEnv()",                     () => Invoke("StampAssetsInAprimoEnv")),
            ["33"] = new("RetryStampAssetsInAprimoEnv()",                () => Invoke("RetryStampAssetsInAprimoEnv")),
            ["34"] = new("FixImageSetPaths()",                           () => Invoke("FixImageSetPaths")),
            ["35"] = new("DigestDeltaUpdateOnly()",                      () => Invoke("DigestDeltaUpdateOnly")),
            ["36"] = new("FindBadParenthesesAemAssetsAsync()",           () => Invoke("FindBadParenthesesAemAssetsAsync")),
            ["37"] = new("FixBadParenthesesAemAssetsAsync()",            () => Invoke("FixBadParenthesesAemAssetsAsync")),
            ["38"] = new("TestAprimoEndpoint()",                         () => Invoke("TestAprimoEndpoint")),
            ["39"] = new("SearchAprimo()",                               () => Invoke("SearchAprimo")),
        };
    }

    private sealed record MenuItem(string Label, Func<Task> Action);
}
