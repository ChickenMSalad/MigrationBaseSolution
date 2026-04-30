using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Migration.Connectors.Sources.Sitecore.Services;

namespace Migration.Hosts.Crocs.SitecoreToBynder.Functions.TimerFunctions;

public sealed class DailyModifiedUpdatesFunction(ContentHubDataMigrationService contentHubDataMigrationService, ILogger<DailyModifiedUpdatesFunction> logger)
{
    [Function("DailyModifiedUpdates")]
    public async Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo timer)
    {
        var count = await contentHubDataMigrationService.GetLastModifiedAssetIdsFromContentHub();
        logger.LogInformation("Queued/identified {Count} modified Content Hub assets for daily update processing.", count);
    }
}
