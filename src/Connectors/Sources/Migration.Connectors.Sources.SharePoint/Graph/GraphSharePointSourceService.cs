using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Migration.Connectors.Sources.SharePoint.Configuration;
using Migration.Connectors.Sources.SharePoint.Services;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Connectors.Sources.SharePoint.Graph;

public sealed class GraphSharePointSourceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphSharePointSourceService> _logger;

    public GraphSharePointSourceService(HttpClient httpClient, ILogger<GraphSharePointSourceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AssetEnvelope> GetAssetAsync(MigrationJobDefinition job, ManifestRow row, SharePointSourceOptions options, CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(job, options, cancellationToken).ConfigureAwait(false);
        var siteId = await ResolveSiteIdAsync(job, options, accessToken, cancellationToken).ConfigureAwait(false);
        var driveId = await ResolveDriveIdAsync(job, options, accessToken, siteId, cancellationToken).ConfigureAwait(false);
        var relativePath = ResolvePath(row, options);
        var graphPath = SharePointPathUtilities.CombineRemotePath(GetSetting(job, "SharePointRootPath", "GraphRootPath") ?? options.Graph.RootPath, relativePath);

        var item = await GetDriveItemByPathAsync(accessToken, driveId, graphPath, cancellationToken).ConfigureAwait(false);
        var metadata = new Dictionary<string, string>(row.Columns, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in SharePointPathUtilities.BuildPathMetadata(relativePath)) metadata.TryAdd(kvp.Key, kvp.Value);
        metadata["_sourceConnector"] = "SharePoint";
        metadata["_sharepointMode"] = "Graph";
        metadata["sharepoint_site_id"] = siteId;
        metadata["sharepoint_drive_id"] = driveId;
        Add(metadata, "sharepoint_drive_item_id", item.Id);
        Add(metadata, "sharepoint_web_url", item.WebUrl);
        Add(metadata, "sharepoint_etag", item.ETag);
        Add(metadata, "sharepoint_created_datetime", item.CreatedDateTime);
        Add(metadata, "sharepoint_modified_datetime", item.LastModifiedDateTime);
        Add(metadata, "sharepoint_created_by", item.CreatedBy?.User?.DisplayName);
        Add(metadata, "sharepoint_modified_by", item.LastModifiedBy?.User?.DisplayName);

        return new AssetEnvelope
        {
            SourceAssetId = row.SourceAssetId ?? item.Id ?? relativePath,
            ExternalId = item.Id,
            Path = relativePath,
            SourceType = ConnectorType.SharePoint,
            Metadata = metadata,
            Binary = new AssetBinary
            {
                FileName = item.Name ?? Path.GetFileName(relativePath),
                ContentType = SharePointPathUtilities.GuessContentType(item.Name ?? relativePath),
                Length = item.Size,
                SourceUri = item.DownloadUrl ?? item.WebUrl
            }
        };
    }

    public async Task<IReadOnlyList<GraphDriveItem>> ListFilesAsync(MigrationJobDefinition job, SharePointSourceOptions options, CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(job, options, cancellationToken).ConfigureAwait(false);
        var siteId = await ResolveSiteIdAsync(job, options, accessToken, cancellationToken).ConfigureAwait(false);
        var driveId = await ResolveDriveIdAsync(job, options, accessToken, siteId, cancellationToken).ConfigureAwait(false);
        var rootPath = SharePointPathUtilities.NormalizeRelativePath(GetSetting(job, "SharePointRootPath", "GraphRootPath") ?? options.Graph.RootPath);
        var startUrl = string.IsNullOrWhiteSpace(rootPath)
            ? $"https://graph.microsoft.com/v1.0/drives/{driveId}/root/children"
            : $"https://graph.microsoft.com/v1.0/drives/{driveId}/root:/{Uri.EscapeDataString(rootPath)}:/children";

        var results = new List<GraphDriveItem>();
        await WalkAsync(accessToken, startUrl, string.Empty, results, cancellationToken).ConfigureAwait(false);
        return results;
    }

    private async Task WalkAsync(string accessToken, string url, string parentPath, List<GraphDriveItem> results, CancellationToken cancellationToken)
    {
        while (!string.IsNullOrWhiteSpace(url))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Graph list failed: {(int)response.StatusCode} {body}");
            var page = JsonSerializer.Deserialize<GraphCollectionResponse<GraphDriveItem>>(body, JsonOptions())!;

            foreach (var item in page.Value ?? new List<GraphDriveItem>())
            {
                var itemPath = SharePointPathUtilities.CombineRemotePath(parentPath, item.Name);
                if (item.Folder is not null)
                    await WalkAsync(accessToken, $"https://graph.microsoft.com/v1.0/drives/{item.ParentReference?.DriveId}/items/{item.Id}/children", itemPath, results, cancellationToken).ConfigureAwait(false);
                else if (item.File is not null)
                {
                    item.RelativePath = itemPath;
                    results.Add(item);
                }
            }

            url = page.NextLink;
        }
    }

    private async Task<GraphDriveItem> GetDriveItemByPathAsync(string accessToken, string driveId, string path, CancellationToken cancellationToken)
    {
        var url = $"https://graph.microsoft.com/v1.0/drives/{driveId}/root:/{Uri.EscapeDataString(path)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Graph item lookup failed: {(int)response.StatusCode} {body}");
        return JsonSerializer.Deserialize<GraphDriveItem>(body, JsonOptions())!;
    }

    private async Task<string> ResolveSiteIdAsync(MigrationJobDefinition job, SharePointSourceOptions options, string accessToken, CancellationToken cancellationToken)
    {
        var explicitSiteId = GetSetting(job, "SharePointSiteId", "GraphSiteId") ?? options.Graph.SiteId;
        if (!string.IsNullOrWhiteSpace(explicitSiteId)) return explicitSiteId;

        var hostname = GetSetting(job, "SharePointSiteHostname", "GraphSiteHostname") ?? options.Graph.SiteHostname;
        var sitePath = GetSetting(job, "SharePointSitePath", "GraphSitePath") ?? options.Graph.SitePath;
        if (string.IsNullOrWhiteSpace(hostname) || string.IsNullOrWhiteSpace(sitePath))
            throw new InvalidOperationException("Graph mode requires SharePointSiteId, or SharePointSiteHostname + SharePointSitePath.");

        var url = $"https://graph.microsoft.com/v1.0/sites/{hostname}:{sitePath}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Graph site lookup failed: {(int)response.StatusCode} {body}");
        var site = JsonSerializer.Deserialize<GraphSite>(body, JsonOptions())!;
        return site.Id ?? throw new InvalidOperationException("Graph site lookup did not return an id.");
    }

    private async Task<string> ResolveDriveIdAsync(MigrationJobDefinition job, SharePointSourceOptions options, string accessToken, string siteId, CancellationToken cancellationToken)
    {
        var explicitDriveId = GetSetting(job, "SharePointDriveId", "GraphDriveId") ?? options.Graph.DriveId;
        if (!string.IsNullOrWhiteSpace(explicitDriveId)) return explicitDriveId;

        var driveName = GetSetting(job, "SharePointDriveName", "GraphDriveName") ?? options.Graph.DriveName;
        var url = $"https://graph.microsoft.com/v1.0/sites/{siteId}/drives";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Graph drive lookup failed: {(int)response.StatusCode} {body}");
        var drives = JsonSerializer.Deserialize<GraphCollectionResponse<GraphDrive>>(body, JsonOptions())!;
        var drive = drives.Value?.FirstOrDefault(d => string.Equals(d.Name, driveName, StringComparison.OrdinalIgnoreCase));
        return drive?.Id ?? throw new InvalidOperationException($"Could not find SharePoint document library/drive named '{driveName}'.");
    }

    private async Task<string> GetAccessTokenAsync(MigrationJobDefinition job, SharePointSourceOptions options, CancellationToken cancellationToken)
    {
        var tenantId = GetSetting(job, "TenantId", "GraphTenantId", "SharePointTenantId") ?? options.Graph.TenantId;
        var clientId = GetSetting(job, "ClientId", "GraphClientId", "SharePointClientId") ?? options.Graph.ClientId;
        var clientSecret = GetSetting(job, "ClientSecret", "GraphClientSecret", "SharePointClientSecret") ?? options.Graph.ClientSecret;
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException("Graph mode requires TenantId, ClientId, and ClientSecret.");

        var form = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["scope"] = "https://graph.microsoft.com/.default",
            ["grant_type"] = "client_credentials"
        };
        using var response = await _httpClient.PostAsync($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token", new FormUrlEncodedContent(form), cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Graph token request failed: {(int)response.StatusCode} {body}");
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    private static string ResolvePath(ManifestRow row, SharePointSourceOptions options)
    {
        if (!string.IsNullOrWhiteSpace(row.SourcePath)) return row.SourcePath!;
        foreach (var field in options.PathFields)
            if (row.Columns.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        throw new InvalidOperationException($"Manifest row {row.RowId} does not contain a SharePoint source path.");
    }

    private static void Add(IDictionary<string, string> values, string key, string? value) { if (!string.IsNullOrWhiteSpace(value)) values.TryAdd(key, value); }
    private static string? GetSetting(MigrationJobDefinition job, params string[] keys) => keys.Select(k => job.Settings.TryGetValue(k, out var v) ? v : null).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };
}

public sealed class GraphCollectionResponse<T>
{
    public List<T>? Value { get; init; }
    public string? NextLink { get; init; }
}

public sealed class GraphSite { public string? Id { get; init; } }
public sealed class GraphDrive { public string? Id { get; init; } public string? Name { get; init; } }
public sealed class GraphDriveItem
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public long? Size { get; init; }
    public string? WebUrl { get; init; }
    public string? ETag { get; init; }
    public string? CreatedDateTime { get; init; }
    public string? LastModifiedDateTime { get; init; }
    public GraphIdentitySet? CreatedBy { get; init; }
    public GraphIdentitySet? LastModifiedBy { get; init; }
    public GraphFolder? Folder { get; init; }
    public GraphFile? File { get; init; }
    public GraphParentReference? ParentReference { get; init; }
    public string? DownloadUrl { get; init; }
    public string? RelativePath { get; set; }
}
public sealed class GraphFolder { }
public sealed class GraphFile { public string? MimeType { get; init; } }
public sealed class GraphParentReference { public string? DriveId { get; init; } }
public sealed class GraphIdentitySet { public GraphIdentity? User { get; init; } }
public sealed class GraphIdentity { public string? DisplayName { get; init; } }
