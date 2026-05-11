using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Connectors.Sources.SharePoint.Configuration;
using Migration.Connectors.Sources.SharePoint.Graph;
using Migration.Connectors.Sources.SharePoint.Rclone;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.SharePoint;

public sealed class SharePointSourceConnector : IAssetSourceConnector
{
    private readonly SharePointSourceOptions _options;
    private readonly RcloneSharePointSourceService _rclone;
    private readonly GraphSharePointSourceService _graph;
    private readonly ILogger<SharePointSourceConnector> _logger;

    public SharePointSourceConnector(
        IOptions<SharePointSourceOptions> options,
        RcloneSharePointSourceService rclone,
        GraphSharePointSourceService graph,
        ILogger<SharePointSourceConnector> logger)
    {
        _options = options.Value;
        _rclone = rclone;
        _graph = graph;
        _logger = logger;
    }

    public string Type => "SharePoint";

    public Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, CancellationToken cancellationToken = default)
    {
        var mode = ResolveMode(job);
        _logger.LogDebug("Resolving SharePoint asset with mode {Mode}. RowId={RowId}", mode, row.RowId);
        return mode.Equals("Graph", StringComparison.OrdinalIgnoreCase)
            ? _graph.GetAssetAsync(job, row, _options, cancellationToken)
            : _rclone.GetAssetAsync(job, row, _options, cancellationToken);
    }

    private string ResolveMode(MigrationJobDefinition job)
    {
        if (job.Settings.TryGetValue("SharePointMode", out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        if (job.Settings.TryGetValue("SourceBinaryMode", out value) && !string.IsNullOrWhiteSpace(value)) return value;
        return _options.Mode;
    }
}
