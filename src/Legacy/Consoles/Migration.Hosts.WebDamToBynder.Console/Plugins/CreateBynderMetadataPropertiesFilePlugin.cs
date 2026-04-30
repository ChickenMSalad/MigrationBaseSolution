using Migration.Connectors.Targets.Bynder.Services;
using Migration.Hosts.WebDamToBynder.Console.Infrastructure;

using Microsoft.Extensions.Logging;

namespace Migration.Hosts.WebDamToBynder.Console.Plugins;

public sealed class CreateBynderMetadataPropertiesFilePlugin(
    BynderMetadataPropertiesService bynderMetadataService,
    ILogger<CreateBynderMetadataPropertiesFilePlugin> logger) : IPlugin
{
    public string Name => "Bynder Metadata Properties File";
    public string Description => "Generate the Bynder metaproperties export file.";
    public int Priority => 110;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating Bynder metadata properties file...");
        await bynderMetadataService.CreateMetadataPropertiesFile().ConfigureAwait(false);
    }
}
