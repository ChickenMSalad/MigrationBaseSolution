using System.Text;
using Microsoft.Extensions.Logging;
using Migration.Connectors.Targets.Cloudinary.Services;
using Migration.Hosts.Cloudinary.CsvToCloudinary.Console.Infrastructure;
using Migration.Shared.Files;

namespace Migration.Hosts.Cloudinary.CsvToCloudinary.Console.Plugins;

public sealed class CloudinaryMigrationPlugin(
    CloudinaryCsvMigrationService service,
    IConsoleReaderService reader,
    ILogger<CloudinaryMigrationPlugin> logger) : IPlugin
{
    public string Name => "Cloudinary CSV Migration Tool";
    public string Description => "Runs the migration and Cloudinary helper routines from a menu.";
    public int Priority => 100;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{Menu}", RenderMenu());

        var choice = (await reader.ReadInputAsync())?.Trim();
        if (string.IsNullOrWhiteSpace(choice))
        {
            logger.LogWarning("No choice entered.");
            return;
        }

        if (!BuildActions(cancellationToken).TryGetValue(choice, out var action))
        {
            logger.LogWarning("Invalid choice '{Choice}'.", choice);
            return;
        }

        await action().ConfigureAwait(false);
    }

    private static string RenderMenu()
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("Choose the Cloudinary routine:");
        sb.AppendLine();
        sb.AppendLine("  1: RunMigrationAsync()");
        sb.AppendLine("  2: PreflightCheckAsync()");
        sb.AppendLine("  3: ListMetadataFieldsAsync()");
        sb.AppendLine("  4: AuditMissingAssetsAsync()");
        sb.AppendLine("  5: DetectDuplicatePublicIdsAsync()");
        sb.AppendLine("  6: DeleteAssetsFromManifestAsync()");
        sb.AppendLine();
        sb.Append("Enter your choice:");
        return sb.ToString();
    }

    private Dictionary<string, Func<Task>> BuildActions(CancellationToken cancellationToken) => new(StringComparer.Ordinal)
    {
        ["1"] = () => service.RunMigrationAsync(cancellationToken),
        ["2"] = () => service.PreflightCheckAsync(cancellationToken),
        ["3"] = () => service.ListMetadataFieldsAsync(cancellationToken),
        ["4"] = () => service.AuditMissingAssetsAsync(cancellationToken),
        ["5"] = () => service.DetectDuplicatePublicIdsAsync(cancellationToken),
        ["6"] = () => service.DeleteAssetsFromManifestAsync(cancellationToken)
    };
}
