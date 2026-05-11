using Migration.Connectors.Sources.WebDam.Services;
using Migration.Hosts.WebDamToBynder.Console.Infrastructure;
using Migration.Shared.Files;

using Microsoft.Extensions.Logging;

namespace Migration.Hosts.WebDamToBynder.Console.Plugins;

public sealed class WebDamDataMigrationPlugin(
    WebDamExportService webDamExportService,
    WebDamExcelExporter webDamExcelExporter,
    IConsoleReaderService reader,
    ILogger<WebDamDataMigrationPlugin> logger) : IPlugin
{
    public string Name => "WebDam Export Tool";
    public string Description => "Pull assets, metadata, and schema out of WebDam.";
    public int Priority => 100;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Begin WebDam export.");

        logger.LogInformation("""
            Choose a step:

            1: Export assets/metadata/schema to Excel
            2: Test WebDam connection

            Enter your choice:
            """);

        var options = new Dictionary<string, Func<Task>>
        {
            { "1", () => ExportAssetsToExcelAsync(cancellationToken) },
            { "2", () => TestConnectionAsync(cancellationToken) }
        };

        var userChoice = await reader.ReadInputAsync();
        if (!options.TryGetValue(userChoice ?? string.Empty, out var action))
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

    private async Task ExportAssetsToExcelAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Enter full output path for the Excel file, or press Enter to use the default WebDam Migration folder.");

        var defaultDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Ntara",
            "WebDam Migration");

        var userPath = (await reader.ReadInputAsync())?.Trim();
        var outputPath = string.IsNullOrWhiteSpace(userPath)
            ? Path.Combine(defaultDirectory, $"WebDamExport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx")
            : userPath;

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var export = await webDamExportService.ExportAllAssetsAsync(cancellationToken).ConfigureAwait(false);
        await using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        webDamExcelExporter.Write(stream, export);

        logger.LogInformation("WebDam export written to {OutputPath}", outputPath);
    }

    private async Task TestConnectionAsync(CancellationToken cancellationToken)
    {
        var export = await webDamExportService.ExportAllAssetsAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Connection succeeded. Retrieved {AssetCount} assets.", export.Assets.Count);
    }
}
