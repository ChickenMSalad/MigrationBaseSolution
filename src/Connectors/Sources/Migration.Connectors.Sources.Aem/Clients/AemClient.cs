
using Migration.Connectors.Sources.Aem.Clients;
using Migration.Connectors.Sources.Aem.Models;
using Migration.Connectors.Sources.Aem.Configuration;
using Migration.Shared.Configuration.Hosts.Aem;
using Migration.Shared.Configuration.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Engineering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
//using static System.Runtime.InteropServices.JavaScript.JSType;
namespace Migration.Connectors.Sources.Aem.Clients;

public sealed class AemClient : IAemClient
{
    private readonly HttpClient _http;
    private readonly AemOptions _opt;

    private List<string> _failures = new List<string>();
    private string outputFilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Ntara\\Ashley Migration\\RerunFirstRun\\folder_errors_1.txt";

    public AemClient(HttpClient http, AemOptions opt)
    {
        _http = http;
        _opt = opt;

        _http.Timeout = TimeSpan.FromMinutes(10); // some folder contain tens of thousands of assets

        if (_opt.AuthType.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opt.DeveloperTokenOrUser);
            _http.DefaultRequestHeaders.Add("X-Api-Key", _opt.ClientId);
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
        }
        else if (_opt.AuthType.Equals("Basic", StringComparison.OrdinalIgnoreCase))
        {
            var raw = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_opt.DeveloperTokenOrUser}:{_opt.Password}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", raw);
        }

        _http.BaseAddress = new Uri(_opt.BaseUrl);
    }

    public async Task<AemFolder> GetFolderAsync(string folderPath, ILogger log, CancellationToken ct = default, bool useLastModifiedOnly = false)
    {
        string? cursor = null;
        var childFolders = new List<string>();
        var folderAssets = new List<AemAsset>();
        string? error = null;
        do
        {
            try
            {
                int limit = 500;
                int offset = 0;
                int total = int.MaxValue; // unknown until first call

                // using QueryBuilder with pagination
                while (offset < total)
                {
                    var resultQb = new QueryBuilderResponse();
                    try
                    {
                        var qbUrl =
                            $"{_opt.QueryBuilderRoot}" +
                            $"?path={folderPath}" +
                            $"&path.flat=true" +
                            $"&p.limit={limit}" +
                            $"&p.offset={offset}" +
                            $"&p.hits=selective" +
                            $"&p.properties=jcr:path%20jcr:primaryType";

                        log.LogInformation($"processing folders: offset {offset}");
                        log.LogInformation($"   url: {qbUrl}");

                        using var resQb = await _http.GetAsync(qbUrl, ct);
                        resQb.EnsureSuccessStatusCode();

                        using var streamQb = await resQb.Content.ReadAsStreamAsync(ct);
                        using var docQb = await JsonDocument.ParseAsync(streamQb, cancellationToken: ct);

                        resultQb = JsonConvert.DeserializeObject<QueryBuilderResponse>(docQb.RootElement.ToString());
                        if (resultQb == null)
                            break;

                        // Set the total on the first call
                        if (total == int.MaxValue)
                        {
                            total = resultQb.Total;
                            log.LogInformation($"Folders Total to process : {total}");
                        }
                            

                        // Process hits
                        if (resultQb.Hits != null)
                        {
                            foreach (var hit in resultQb.Hits)
                            {
                                if (hit.PrimaryType == "sling:Folder"
                                    || hit.PrimaryType == "sling:OrderedFolder"
                                    || hit.PrimaryType == "dam:AssetFolder")
                                {
                                    childFolders.Add(hit.Path);
                                }
                            }
                        }
                    } catch (Exception ex)
                    {
                        string err = $"Cannot process folder {folderPath} : {ex.Message}";
                        log.LogWarning(err);
                        _failures.Add(err);
                    }

                    // Move to next page
                    offset += limit;

                    // Safety break (should never trigger unless AEM returns weird totals)
                    if (resultQb.Hits == null || resultQb.Hits.Count == 0)
                        break;
                }


                // using Folders API
                //var url = $"{_opt.OpenApiFolderRoot}?path={folderPath}&limit={_opt.PageSize}&cursor={cursor}";
                //using var res = await _http.GetAsync(url, ct);
                //res.EnsureSuccessStatusCode();

                //using var stream = await res.Content.ReadAsStreamAsync(ct);
                //using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                //var root = doc.RootElement;
                //File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Ntara\\Ashley Migration\\test.json", root.ToString());

                //cursor = null;
                //if (root.TryGetProperty("cursor", out var cursorValue))
                //{
                //    cursor = cursorValue.ToString();
                //}

                var aas = await GetAssetsData(folderPath,log, ct, useLastModifiedOnly);
                folderAssets.AddRange(aas);

                //if (root.TryGetProperty("children", out var children))
                //{
                //    foreach (var item in children.EnumerateArray())
                //    {
                //        var path = item.GetProperty("path").GetString()!;
                //        childFolders.Add(path);

                //    }
                //}
            }
            catch (Exception ex)
            {
                string err = $"Cannot process folder {folderPath} : {ex.Message}";
                log.LogWarning(err);
                _failures.Add(err);
                error = err;
            }


        } while (!string.IsNullOrEmpty(cursor));


        return new AemFolder(folderPath, folderAssets, childFolders, error);
    }

    private async Task<List<AemAsset>> GetAssetsData(string path, ILogger log, CancellationToken ct = default, bool lastModifiedOnly = false)
    {
        List<AemAsset> assets = new List<AemAsset>();

        var limit = 500;
        var offset = 0;
        var total = int.MaxValue; // unknown until first response

        // using QueryBuilder with pagination
        while (offset < total)
        {
            try
            {
                var qbUrl =
                    $"{_opt.QueryBuilderRoot}" +
                    $"?path={path}" +
                    $"&path.flat=true" +
                    $"&type=dam:Asset" +
                    $"&mainasset=true" +
                    $"&p.limit={limit}" +
                    $"&p.offset={offset}" +
                    $"&p.hits=selective" +
                    $"&p.properties=jcr:path%20jcr:uuid%20jcr:created%20jcr:content/jcr:lastModified%20jcr:content/metadata/dam:size%20jcr:content/metadata/dam:MIMEtype%20jcr:content/metadata/dc:title";

                if (lastModifiedOnly)
                {
                    qbUrl =
                        $"{_opt.QueryBuilderRoot}" +
                        $"?path={path}" +
                        $"&path.flat=true" +
                        $"&type=dam:Asset" +
                        $"&mainasset=true" +
                        $"&p.limit={limit}" +
                        $"&p.offset={offset}" +

                        // Last modified filter - 11/15/2025 - First Run
                        // Last modified filter - 02/12/2026 - Second Run
                        $"&1_daterange.property=jcr:content/jcr:lastModified" +
                        $"&1_daterange.lowerBound=2026-03-13T00:00:00.000Z" +
                        $"&1_daterange.lowerOperation=>=" +

                        $"&p.hits=selective" +
                        $"&p.properties=jcr:path%20jcr:uuid%20jcr:created%20" +
                        $"jcr:content/jcr:lastModified%20" +
                        $"jcr:content/metadata/dam:size%20" +
                        $"jcr:content/metadata/dam:MIMEtype%20" +
                        $"jcr:content/metadata/dc:title";

                }

                log.LogInformation($"processing assets: offset {offset}");
                log.LogInformation($"   url: {qbUrl}");

                using var resQb = await _http.GetAsync(qbUrl, ct);
                resQb.EnsureSuccessStatusCode();

                using var streamQb = await resQb.Content.ReadAsStreamAsync(ct);
                using var docQb = await JsonDocument.ParseAsync(streamQb, cancellationToken: ct);

                var resultQb = JsonConvert.DeserializeObject<QueryBuilderResponse>(docQb.RootElement.ToString());
                if (resultQb == null)
                    break;

                // Initialize total on first page
                if (total == int.MaxValue)
                {
                    total = resultQb.Total;
                    log.LogInformation($"Assets Total to process : {total}");
                }

                if (resultQb.Hits != null && resultQb.Hits.Count > 0)
                {
                    foreach (var hit in resultQb.Hits)
                    {
                        // Create AAID
                        string aaid = $"urn:aaid:aem:{hit.Uuid}";

                        var aa = new AemAsset
                        {
                            Id = hit.Uuid,
                            Name = hit.Content?.Metadata?.Title,
                            Path = hit.Path,
                            MimeType = hit.Content?.Metadata?.MimeType,
                            SizeBytes = hit.Content?.Metadata?.Size ?? 0,
                            Created = hit.Created,
                            LastModified = hit.Content?.LastModified
                        };

                        assets.Add(aa);
                    }
                }
                else
                {
                    // No hits in this page, stop looping
                    break;
                }
            } catch (Exception ex)
            {
                string err = $"Error processing {path}: {ex.Message}";
                log.LogWarning(err);
                _failures.Add(err);

            }


            // Next page
            offset += limit;
        }


        return assets;
    }

    public async Task<AemAsset> GetAssetByUUID(string uuid, ILogger log, CancellationToken ct = default)
    {
        List<AemAsset> assets = new List<AemAsset>();


        try
        {
            var qbUrl =
                $"{_opt.QueryBuilderRoot}" +
                $"?type=dam:Asset" +
                $"&p.limit=1" +
                $"&property=jcr:uuid" +
                $"&property.value={uuid}" +
                $"&p.hits=selective" +
                $"&p.properties=jcr:path%20jcr:uuid%20jcr:created%20jcr:content/jcr:lastModified%20jcr:content/metadata/dam:size%20jcr:content/metadata/dam:MIMEtype%20jcr:content/metadata/dc:title";

            log.LogInformation($"   url: {qbUrl}");

            using var resQb = await _http.GetAsync(qbUrl, ct);
            resQb.EnsureSuccessStatusCode();

            using var streamQb = await resQb.Content.ReadAsStreamAsync(ct);
            using var docQb = await JsonDocument.ParseAsync(streamQb, cancellationToken: ct);

            var resultQb = JsonConvert.DeserializeObject<QueryBuilderResponse>(docQb.RootElement.ToString());
            if (resultQb == null)
            {
                log.LogInformation($"Null result returned! for {uuid}");
                return assets.FirstOrDefault();
            }
                

            if (resultQb.Hits != null && resultQb.Hits.Count > 0)
            {
                foreach (var hit in resultQb.Hits)
                {
                    var aa = new AemAsset
                    {
                        Id = hit.Uuid,
                        Name = hit.Content?.Metadata?.Title,
                        Path = hit.Path,
                        MimeType = hit.Content?.Metadata?.MimeType,
                        SizeBytes = hit.Content?.Metadata?.Size ?? 0,
                        Created = hit.Created,
                        LastModified = hit.Content?.LastModified
                    };

                    assets.Add(aa);
                }
            } else {
                log.LogInformation($"No hits for {uuid}");
            }
                
        }
        catch (Exception ex)
        {
            string err = $"Error processing {uuid}: {ex.Message}";
            log.LogWarning(err);
            _failures.Add(err);

        }


        return assets.FirstOrDefault();
    }
    public async Task<AemAsset> GetAssetByPath(string path, ILogger log, CancellationToken ct = default)
    {
        List<AemAsset> assets = new List<AemAsset>();


        try
        {
            var qbUrl =
                $"{_opt.QueryBuilderRoot}" +
                $"?type=dam:Asset" +
                $"&p.limit=1" +
                $"&property=jcr:path" +
                $"&property.value={path}" +
                $"&p.hits=selective" +
                $"&p.properties=jcr:path%20jcr:uuid%20jcr:created%20jcr:content/jcr:lastModified%20jcr:content/metadata/dam:size%20jcr:content/metadata/dam:MIMEtype%20jcr:content/metadata/dc:title";

            log.LogInformation($"   url: {qbUrl}");

            using var resQb = await _http.GetAsync(qbUrl, ct);
            resQb.EnsureSuccessStatusCode();

            using var streamQb = await resQb.Content.ReadAsStreamAsync(ct);
            using var docQb = await JsonDocument.ParseAsync(streamQb, cancellationToken: ct);

            var resultQb = JsonConvert.DeserializeObject<QueryBuilderResponse>(docQb.RootElement.ToString());
            if (resultQb == null)
            {
                log.LogInformation($"Null result returned! for {path}");
                return assets.FirstOrDefault();
            }


            if (resultQb.Hits != null && resultQb.Hits.Count > 0)
            {
                foreach (var hit in resultQb.Hits)
                {
                    var aa = new AemAsset
                    {
                        Id = hit.Uuid,
                        Name = hit.Content?.Metadata?.Title,
                        Path = hit.Path,
                        MimeType = hit.Content?.Metadata?.MimeType,
                        SizeBytes = hit.Content?.Metadata?.Size ?? 0,
                        Created = hit.Created,
                        LastModified = hit.Content?.LastModified
                    };

                    assets.Add(aa);
                }
            }
            else
            {
                log.LogInformation($"No hits for {path}");
            }

        }
        catch (Exception ex)
        {
            string err = $"Error processing {path}: {ex.Message}";
            log.LogWarning(err);
            _failures.Add(err);

        }


        return assets.FirstOrDefault();
    }

    public async IAsyncEnumerable<AemAsset> EnumerateAssetsAsync(string folderPath, bool recursive, ILogger log,CancellationToken ct = default, bool useLastModifiedOnly = false)
    {
        var queue = new Queue<string>();
        queue.Enqueue(folderPath);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var folder = await GetFolderAsync(current, log, ct, useLastModifiedOnly);

            foreach (var asset in folder.ChildAssets)
            {
                yield return asset;
            }

            if (recursive)
            {
                foreach (var sub in folder.ChildFolderPaths)
                    queue.Enqueue(sub);
            }
        }
        await AppendLinesToFileAsync(outputFilePath, _failures);
        _failures.Clear();
    }

    public static async Task AppendLinesToFileAsync(string filePath, List<string> lines)
    {
        using var writer = new StreamWriter(filePath, append: true);

        foreach (var line in lines)
        {
            await writer.WriteLineAsync(line);
        }
    }
    public async Task<Stream> GetOriginalAsync(string assetPath, CancellationToken ct = default)
    {
        string url = $"{_opt.BaseUrl.TrimEnd('/')}{assetPath}/_jcr_content/renditions/original";
        using var res = await _http.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync(ct);

        var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        return ms;
    }

    public async Task<Stream> GetMetadataAsync(string assetPath, CancellationToken ct = default)
    {
        string url = $"{_opt.BaseUrl.TrimEnd('/')}{assetPath}/jcr:content/metadata.json";
        using var res = await _http.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(ct);

        var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        return ms;
    }

    public async Task<Stream> GetRenditionsAsync(string assetPath, string assetName,CancellationToken ct = default)
    {
        string url = $"{_opt.BaseUrl.TrimEnd('/')}{assetPath}/jcr:content/renditions/{assetName}";
        using var res = await _http.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(ct);

        var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        return ms;
    }

    public async Task<Stream> GetJcrContentAsync(string assetPath, CancellationToken ct = default)
    {
        string url = $"{_opt.BaseUrl.TrimEnd('/')}{assetPath}/jcr:content.json";
        using var res = await _http.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(ct);

        var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        return ms;
    }

    public async Task<Stream> GetRenditionsFolderAsync(string assetPath, CancellationToken ct = default)
    {
        string url = $"{_opt.BaseUrl.TrimEnd('/')}{assetPath}/jcr:content/renditions.children.json";
        using var res = await _http.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(ct);

        var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        return ms;
    }

    public async Task<Stream> GetRelatedAsync(string assetPath, CancellationToken ct = default)
    {
        string url = $"{_opt.BaseUrl.TrimEnd('/')}{assetPath}/jcr:content/related/s7Set/sling:members.json";
        using var res = await _http.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(ct);

        var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        return ms;
    }



}
