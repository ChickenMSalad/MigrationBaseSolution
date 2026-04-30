using Migration.Connectors.Targets.Aprimo.Clients;
using Migration.Connectors.Targets.Aprimo.Extensions;
using Migration.Connectors.Targets.Aprimo.Files;
using Migration.Connectors.Targets.Aprimo.Models;
using Migration.Connectors.Targets.Aprimo.Models.Aprimo;
using Migration.Connectors.Targets.Aprimo.Configuration;
using Migration.Shared.Configuration.Hosts.Aprimo;
using Migration.Shared.Configuration.Infrastructure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using Migration.Connectors.Targets.Aprimo.Utilities;

namespace Migration.Connectors.Targets.Aprimo.Clients
{
    public sealed class AprimoAssetClient : IAprimoAssetClient
    {
        const int MAX_CHUNK_SIZE = 15728640; // 15MB in bytes (1024 * 1024 * 15)
        private const long TwentyMb = 20L * 1024 * 1024;

        private readonly HttpClient _httpClient;
        private readonly IAprimoAuthClient _authClient;
        private readonly AprimoOptions _options;

        private IReadOnlyDictionary<string, string>? _languageCodeToId;
        private string? _defaultLanguageId;

        private readonly ConcurrentDictionary<string, string> _fieldNameToId = new(StringComparer.OrdinalIgnoreCase);

        public AprimoAssetClient(
            HttpClient httpClient,
            IAprimoAuthClient authClient,
            IOptions<AprimoOptions> options)
        {
            _httpClient = httpClient;
            _authClient = authClient;
            _options = options.Value;

            _httpClient.Timeout = TimeSpan.FromMinutes(60); // some assets are HUGE
        }


        public sealed class AprimoDownloadedFile
            {
                public required MemoryStream Stream { get; init; }
                public required string FileName { get; init; }
                public required string ContentType { get; init; }
        }

        public async Task<string> UploadNewMasterVersionAndResetRenditionRuleRanAsync(
            string recordId,
            Stream fileStream,
            string fileName,
            string contentType,
            string renditionRuleRanFieldId,
            string noOptionId,
            CancellationToken ct = default)
                {
                    if (string.IsNullOrWhiteSpace(recordId))
                        throw new ArgumentNullException(nameof(recordId));
                    if (fileStream == null)
                        throw new ArgumentNullException(nameof(fileStream));
                    if (string.IsNullOrWhiteSpace(fileName))
                        throw new ArgumentNullException(nameof(fileName));
                    if (string.IsNullOrWhiteSpace(contentType))
                        throw new ArgumentNullException(nameof(contentType));
                    if (string.IsNullOrWhiteSpace(renditionRuleRanFieldId))
                        throw new ArgumentNullException(nameof(renditionRuleRanFieldId));
                    if (string.IsNullOrWhiteSpace(noOptionId))
                        throw new ArgumentNullException(nameof(noOptionId));

                    if (fileStream.CanSeek)
                        fileStream.Position = 0;

                    await PrepareAuthAsync(ct).ConfigureAwait(false);

                    var baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

                    // 1) Create upload session
                    long sizeBytes = fileStream.Length;
                    var uploadSession = await CreateUploadSessionAsync(fileName, sizeBytes, contentType, ct)
                        .ConfigureAwait(false);

                    // 2) PUT bytes to SAS URL
                    if (fileStream.CanSeek)
                        fileStream.Position = 0;

                    await PutToAprimoUploadUrlAsync(
                        uploadSession.SasUrl,
                        fileStream,
                        contentType,
                        sizeBytes,
                        ct
                    ).ConfigureAwait(false);

                    var uploadToken = uploadSession.Token;

                    // 3) Read record to locate current master file id
                    var aprimoRecord = await GetAssetByAprimoIdAsync(recordId).ConfigureAwait(false);
                    var masterFileId = aprimoRecord.Embedded?.Masterfile?.Id;

                    if (string.IsNullOrWhiteSpace(masterFileId))
                        throw new InvalidOperationException(
                            $"Record {recordId} does not have an embedded masterfile id.");

                    // 4) Update record:
                    //    - add a NEW version to the existing master file
                    //    - reset RenditionRuleRan option list to "No"
                    //
                    // For language-independent fields Aprimo expects the empty guid as languageId.
                    var payload = new JObject
                    {
                        ["files"] = new JObject
                        {
                            ["addOrUpdate"] = new JArray
                    {
                        new JObject
                        {
                            ["id"] = masterFileId,
                            ["versions"] = new JObject
                            {
                                ["addOrUpdate"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["id"] = uploadToken,
                                        ["fileName"] = fileName
                                    }
                                }
                            }
                        }
                    }
                        },
                        ["fields"] = new JObject
                        {
                            ["addOrUpdate"] = new JArray
                    {
                        new JObject
                        {
                            ["id"] = renditionRuleRanFieldId,
                            ["localizedValues"] = new JArray
                            {
                                new JObject
                                {
                                    ["languageId"] = "00000000000000000000000000000000",
                                    ["values"] = new JArray(noOptionId)
                                }
                            }
                        }
                    }
                        }
                    };

                    var url = new Uri(baseUrl, $"api/core/record/{Uri.EscapeDataString(recordId)}");

                    using var req = new HttpRequestMessage(HttpMethod.Put, url);
                    req.Headers.Add("API-VERSION", "1");
                    req.Headers.Add("User-Agent", "Azure Aprimo Migration Connector");
                    // Optional if you want the record searchable immediately after update:
                    // req.Headers.Add("set-immediateSearchIndexUpdate", "true");

                    req.Content = new StringContent(
                        payload.ToString(Formatting.None),
                        Encoding.UTF8,
                        "application/json");

                    using var res = await _httpClient.SendAsync(
                        req,
                        HttpCompletionOption.ResponseHeadersRead,
                        ct).ConfigureAwait(false);

                    var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                    if (!res.IsSuccessStatusCode)
                        throw new HttpRequestException(
                            $"Create new master version + reset RenditionRuleRan failed: " +
                            $"{(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

                    // Aprimo often will not return the created fileversion id here.
                    // Returning the upload token lets the caller correlate later if needed.
                    return uploadToken;
                }


        public async Task<AprimoDownloadedFile> StreamExistingMasterFileAsync(
    string recordId,
    CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));

            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            // 1) Create a download order for the original master file
            var createOrderUrl = new Uri(baseUrl, "api/core/orders");

            var createOrderPayload = new JObject
            {
                ["type"] = "download",
                ["disableNotification"] = true,
                ["disableProcessing"] = "yes",
                ["targets"] = new JArray
        {
            new JObject
            {
                ["recordId"] = recordId,
                ["targetTypes"] = new JArray("Document"),
                ["assetType"] = "LatestVersionOfMasterFile"
            }
        }
            };

            using var createReq = new HttpRequestMessage(HttpMethod.Post, createOrderUrl);
            createReq.Headers.Add("API-VERSION", "1");
            createReq.Headers.Add("User-Agent", "Azure Aprimo Migration Connector");
            createReq.Content = new StringContent(
                createOrderPayload.ToString(Formatting.None),
                Encoding.UTF8,
                "application/json");

            using var createRes = await _httpClient.SendAsync(
                createReq,
                HttpCompletionOption.ResponseHeadersRead,
                ct).ConfigureAwait(false);

            var createBody = await createRes.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!createRes.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Create download order failed: {(int)createRes.StatusCode} " +
                    $"{createRes.ReasonPhrase}. Body: {createBody}");

            var orderLocation = createRes.Headers.Location;
            if (orderLocation == null)
                throw new InvalidOperationException(
                    "Aprimo did not return a Location header for the created order.");

            if (!orderLocation.IsAbsoluteUri)
                orderLocation = new Uri(baseUrl, orderLocation);

            // 2) Poll until a delivered file URL is available
            var downloadUrl = await WaitForDeliveredFileUrlAsync(orderLocation, ct)
                .ConfigureAwait(false);



            // 3) Download the original file bytes
            //using var downloadReq = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            //using var downloadRes = await _httpClient.SendAsync(
            //    downloadReq,
            //    HttpCompletionOption.ResponseHeadersRead,
            //    ct).ConfigureAwait(false);

            using var downloadClient = new HttpClient();

            using var downloadReq = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            // no Authorization header here

            using var downloadRes = await downloadClient.SendAsync(
                downloadReq,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            if (!downloadRes.IsSuccessStatusCode)
            {
                var errorBody = await downloadRes.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new HttpRequestException(
                    $"Downloading delivered file failed: {(int)downloadRes.StatusCode} " +
                    $"{downloadRes.ReasonPhrase}. Body: {errorBody}");
            }

            var contentType = downloadRes.Content.Headers.ContentType?.MediaType
                              ?? "application/octet-stream";

            var fileName = TryGetFileNameFromContentDisposition(downloadRes.Content.Headers.ContentDisposition)
                           ?? TryGetFileNameFromUrl(downloadUrl)
                           ?? "masterfile";

            var ms = new MemoryStream();
            await using (var responseStream = await downloadRes.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            {
                await responseStream.CopyToAsync(ms, ct).ConfigureAwait(false);
            }

            ms.Position = 0;

            return new AprimoDownloadedFile
            {
                Stream = ms,
                FileName = fileName,
                ContentType = contentType
            };
        }

        public async Task<string> RestampMasterFileNameAsync(
            string recordId,
            string correctedFileName,
            string renditionRuleRanFieldId,
            string noOptionId,
            CancellationToken ct = default)
        {
            var existing = await StreamExistingMasterFileAsync(recordId, ct).ConfigureAwait(false);

            await using (existing.Stream.ConfigureAwait(false))
            {
                return await UploadNewMasterVersionAndResetRenditionRuleRanAsync(
                    recordId,
                    existing.Stream,
                    correctedFileName,
                    existing.ContentType,
                    renditionRuleRanFieldId,
                    noOptionId,
                    ct
                ).ConfigureAwait(false);
            }
        }

        private async Task<Uri> WaitForDeliveredFileUrlAsync(Uri orderUrl, CancellationToken ct)
        {
            const int maxAttempts = 30;
            var delay = TimeSpan.FromSeconds(2);
            await PrepareAuthAsync(ct).ConfigureAwait(false);
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, orderUrl);
                req.Headers.Add("API-VERSION", "1");
                req.Headers.Add("User-Agent", "Azure Aprimo Migration Connector");

                using var res = await _httpClient.SendAsync(
                    req,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct).ConfigureAwait(false);

                var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                if (!res.IsSuccessStatusCode)
                    throw new HttpRequestException(
                        $"Reading order failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

                var json = JObject.Parse(body);

                var deliveredFiles = json["deliveredFiles"] as JArray;
                if (deliveredFiles is { Count: > 0 })
                {
                    var first = deliveredFiles[0];

                    string? raw = first switch
                    {
                        JValue v => v.Value<string>(),
                        JObject o => o["uri"]?.Value<string>()
                                  ?? o["url"]?.Value<string>()
                                  ?? o["_links"]?["self"]?["href"]?.Value<string>(),
                        _ => null
                    };

                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        if (Uri.TryCreate(raw, UriKind.Absolute, out var absolute))
                            return absolute;

                        var baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
                        return new Uri(baseUrl, raw);
                    }
                }

                var status = json["status"]?.Value<string>();
                if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Aprimo order failed. Body: {body}");

                await Task.Delay(delay, ct).ConfigureAwait(false);
            }

            throw new TimeoutException("Timed out waiting for Aprimo to produce a delivered file URL.");
        }

        private static string? TryGetFileNameFromContentDisposition(ContentDispositionHeaderValue? cd)
        {
            if (cd == null)
                return null;

            var fileNameStar = cd.FileNameStar?.Trim('"');
            if (!string.IsNullOrWhiteSpace(fileNameStar))
                return fileNameStar;

            var fileName = cd.FileName?.Trim('"');
            return string.IsNullOrWhiteSpace(fileName) ? null : fileName;
        }

        private static string? TryGetFileNameFromUrl(Uri url)
        {
            var last = url.Segments.LastOrDefault();
            if (string.IsNullOrWhiteSpace(last))
                return null;

            last = Uri.UnescapeDataString(last.Trim('/'));
            return string.IsNullOrWhiteSpace(last) ? null : last;
        }

        private async Task PrepareAuthAsync(CancellationToken cancellationToken)
        {
            var token = await _authClient.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<AprimoRecord?> GetAssetByAemAssetIdAsync(
            string aemAssetId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aemAssetId))
                throw new ArgumentException("aemAssetId cannot be null/empty.", nameof(aemAssetId));


            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            const string fieldName = "productsAEMAssetID";
            const string fieldLabel = "AEM Asset ID";

            // Per API docs: POST /search/records with searchExpression { expression, parameters, ... } :contentReference[oaicite:3]{index=3}
            //var requestBody = new
            //{
            //    searchExpression = new
            //    {
            //        disabledKeywords = (string?)null,
            //        supportWildcards = false,
            //        defaultLogicalOperator = "AND",
            //        languages = (string[]?)null,     // optional; omit or set if you want language-specific search
            //        expression = $"{fieldName} = ?",
            //        parameters = new object[] { aemAssetId },
            //        namedParameters = new { },
            //        subExpressions = (object?)null
            //    },
            //    logRequest = false
            //};

            var requestBody = new
            {
                searchExpression = new
                {
                    expression = $" '{fieldLabel}' = '{aemAssetId}'",
                },
                logRequest = false
            };

            var url = $"{_baseUrl}api/core/search/records";

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-additionalfile", "metadata, uri" },
                        //{ System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { "select-record", "masterfilelatestversion, fields, preview" },
                        { "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "*/*" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Aprimo Migration Connector" },
                    },
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"),
            };

            //using var req = new HttpRequestMessage(HttpMethod.Post, url);
            //req.Headers.TryAddWithoutValidation("API-Version", "1"); // API-Version header is required for most endpoints :contentReference[oaicite:4]{index=4}
            //req.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (res.StatusCode == HttpStatusCode.NotFound)
                return null;

            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Aprimo search failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

            var result = JsonConvert.DeserializeObject<AprimoRecordPagedCollection>(body);

            return result?.Items?.FirstOrDefault();
        }

        public async Task<AprimoRecord?> GetImageSetByAemImageSetIdAsync(
            string aemImageSetId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aemImageSetId))
                throw new ArgumentException("aemImageSetId cannot be null/empty.", nameof(aemImageSetId));


            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            const string fieldName = "productsAEMImageSetID";
            const string fieldLabel = "AEM Image Set Id";

            // Per API docs: POST /search/records with searchExpression { expression, parameters, ... } :contentReference[oaicite:3]{index=3}
            //var requestBody = new
            //{
            //    searchExpression = new
            //    {
            //        disabledKeywords = (string?)null,
            //        supportWildcards = false,
            //        defaultLogicalOperator = "AND",
            //        languages = (string[]?)null,     // optional; omit or set if you want language-specific search
            //        expression = $"{fieldName} = ?",
            //        parameters = new object[] { aemAssetId },
            //        namedParameters = new { },
            //        subExpressions = (object?)null
            //    },
            //    logRequest = false
            //};

            var requestBody = new
            {
                searchExpression = new
                {
                    expression = $" '{fieldLabel}' = '{aemImageSetId}'",
                },
                logRequest = false
            };

            var url = $"{_baseUrl}api/core/search/records";

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-additionalfile", "metadata, uri" },
                        //{ System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { "select-record", "masterfilelatestversion, fields, preview" },
                        { "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "*/*" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Aprimo Migration Connector" },
                    },
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"),
            };

            //using var req = new HttpRequestMessage(HttpMethod.Post, url);
            //req.Headers.TryAddWithoutValidation("API-Version", "1"); // API-Version header is required for most endpoints :contentReference[oaicite:4]{index=4}
            //req.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (res.StatusCode == HttpStatusCode.NotFound)
                return null;

            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Aprimo search failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

            var result = JsonConvert.DeserializeObject<AprimoRecordPagedCollection>(body);

            return result?.Items?.FirstOrDefault();
        }

        public async Task<List<AprimoRecord?>> GetAssetsByAemAssetIdAsync(
            string aemAssetId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aemAssetId))
                throw new ArgumentException("aemAssetId cannot be null/empty.", nameof(aemAssetId));


            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            const string fieldName = "productsAEMAssetID";
            const string fieldLabel = "AEM Asset ID";

            // Per API docs: POST /search/records with searchExpression { expression, parameters, ... } :contentReference[oaicite:3]{index=3}
            //var requestBody = new
            //{
            //    searchExpression = new
            //    {
            //        disabledKeywords = (string?)null,
            //        supportWildcards = false,
            //        defaultLogicalOperator = "AND",
            //        languages = (string[]?)null,     // optional; omit or set if you want language-specific search
            //        expression = $"{fieldName} = ?",
            //        parameters = new object[] { aemAssetId },
            //        namedParameters = new { },
            //        subExpressions = (object?)null
            //    },
            //    logRequest = false
            //};

            var requestBody = new
            {
                searchExpression = new
                {
                    expression = $" '{fieldLabel}' = '{aemAssetId}'",
                },
                logRequest = false
            };

            var url = $"{_baseUrl}api/core/search/records";

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-additionalfile", "metadata, uri" },
                        //{ System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { "select-record", "masterfilelatestversion, fields, preview" },
                        { "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "*/*" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Aprimo Migration Connector" },
                    },
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"),
            };

            //using var req = new HttpRequestMessage(HttpMethod.Post, url);
            //req.Headers.TryAddWithoutValidation("API-Version", "1"); // API-Version header is required for most endpoints :contentReference[oaicite:4]{index=4}
            //req.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (res.StatusCode == HttpStatusCode.NotFound)
                return null;

            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Aprimo search failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

            var result = JsonConvert.DeserializeObject<AprimoRecordPagedCollection>(body);

            return result?.Items?.ToList() ?? new List<AprimoRecord>();
        }

        public async Task<IReadOnlyList<AprimoRecord>> GetAssetsBySearchAsync(
            string searchExpression,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(searchExpression))
                throw new ArgumentException("searchExpression cannot be null/empty.", nameof(searchExpression));


            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");


            // Per API docs: POST /search/records with searchExpression { expression, parameters, ... } :contentReference[oaicite:3]{index=3}
            //var requestBody = new
            //{
            //    searchExpression = new
            //    {
            //        disabledKeywords = (string?)null,
            //        supportWildcards = false,
            //        defaultLogicalOperator = "AND",
            //        languages = (string[]?)null,     // optional; omit or set if you want language-specific search
            //        expression = $"{fieldName} = ?",
            //        parameters = new object[] { aemAssetId },
            //        namedParameters = new { },
            //        subExpressions = (object?)null
            //    },
            //    logRequest = false
            //};

            var results = new List<AprimoRecord>();
            int page = 1;
            bool hasMore = true;

            const int max401RetriesPerPage = 3;

            var requestBody = new
            {
                searchExpression = new
                {
                    expression = $"{searchExpression}",
                },
                logRequest = false
            };

            var baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            while (hasMore)
            {
                //var url = $"{baseUrl}api/core/records";
                Console.WriteLine($"Processing page: {page}. total results: {results.Count}");
                var url = $"{baseUrl}api/core/search/records?page={page}&pageSize=1000";

                int attempt = 0;

                while (true)
                {
                    attempt++;


                    var req = new HttpRequestMessage
                    {
                        RequestUri = new Uri(url),
                        Method = HttpMethod.Post,
                        Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-additionalfile", "metadata, uri" },
                        //{ System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { "select-record", "masterfilelatestversion, fields, preview" },
                        { "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "*/*" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Aprimo Migration Connector" },
                    },
                        Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"),
                    };


                    using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                    if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        if (attempt > max401RetriesPerPage)
                        {
                            var err = await res.Content.ReadAsStringAsync(ct);
                            throw new HttpRequestException(
                                $"Aprimo returned 401 after {max401RetriesPerPage} retries. Body: {err}");
                        }

                        // Force refresh token and retry
                        await PrepareAuthAsync(ct).ConfigureAwait(false);
                        continue;
                    }

                    // For other failures, throw (and include body for debugging)
                    if (!res.IsSuccessStatusCode)
                    {
                        var err = await res.Content.ReadAsStringAsync(ct);
                        //throw new HttpRequestException(
                        //    $"Aprimo request failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {err}");
                        return results;
                    }

                    var json = await res.Content.ReadAsStringAsync(ct);
                    var pageResult = JsonConvert.DeserializeObject<AprimoRecordPagedCollection>(json);

                    if (pageResult?.Items == null || pageResult.Items.Length == 0)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        results.AddRange(pageResult.Items);

                        // Stop when we've retrieved all records
                        if (results.Count >= pageResult.TotalCount)
                            hasMore = false;
                        else
                            page++;
                    }

                    break; // success -> exit retry loop
                }
            }

            return results;
        }

        public async Task<List<AprimoRecord?>> GetImageSetsByAemImageSetIdAsync(
            string aemImageSetId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aemImageSetId))
                throw new ArgumentException("aemImageSetId cannot be null/empty.", nameof(aemImageSetId));


            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            const string fieldName = "productsAEMImageSetID";
            const string fieldLabel = "AEM Image Set Id";

            // Per API docs: POST /search/records with searchExpression { expression, parameters, ... } :contentReference[oaicite:3]{index=3}
            //var requestBody = new
            //{
            //    searchExpression = new
            //    {
            //        disabledKeywords = (string?)null,
            //        supportWildcards = false,
            //        defaultLogicalOperator = "AND",
            //        languages = (string[]?)null,     // optional; omit or set if you want language-specific search
            //        expression = $"{fieldName} = ?",
            //        parameters = new object[] { aemAssetId },
            //        namedParameters = new { },
            //        subExpressions = (object?)null
            //    },
            //    logRequest = false
            //};

            var requestBody = new
            {
                searchExpression = new
                {
                    expression = $" '{fieldLabel}' = '{aemImageSetId}'",
                },
                logRequest = false
            };

            var url = $"{_baseUrl}api/core/search/records";

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-additionalfile", "metadata, uri" },
                        //{ System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { "select-record", "masterfilelatestversion, fields, preview" },
                        { "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "*/*" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Aprimo Migration Connector" },
                    },
                Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"),
            };

            //using var req = new HttpRequestMessage(HttpMethod.Post, url);
            //req.Headers.TryAddWithoutValidation("API-Version", "1"); // API-Version header is required for most endpoints :contentReference[oaicite:4]{index=4}
            //req.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (res.StatusCode == HttpStatusCode.NotFound)
                return null;

            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Aprimo search failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

            var result = JsonConvert.DeserializeObject<AprimoRecordPagedCollection>(body);

            return result?.Items?.ToList() ?? new List<AprimoRecord>();
        }

        public async Task<AprimoRecord?> GetAssetByAprimoIdAsync(
            string aprimoId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aprimoId))
                throw new ArgumentException("aprimoId cannot be null/empty.", nameof(aprimoId));


            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            var url = $"{_baseUrl}api/core/record/{aprimoId}";

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        { "select-record", "masterfile, masterfilelatestversion, fields, preview" },
                        { "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "*/*" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Aprimo Migration Connector" },
                    },
                Content = null,
            };

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (res.StatusCode == HttpStatusCode.NotFound)
                return null;

            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Aprimo search failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

            var result = JsonConvert.DeserializeObject<AprimoRecord>(body);

            return result;
        }

        public async Task<bool> DeleteAssetByAprimoIdAsync(
            string aprimoId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aprimoId))
                throw new ArgumentException("aprimoId cannot be null/empty.", nameof(aprimoId));


            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            var url = $"{_baseUrl}api/core/record/{aprimoId}";

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Delete,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        { "select-record", "masterfilelatestversion, fields, preview" },
                        { "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "*/*" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Aprimo Migration Connector" },
                    },
                Content = null,
            };

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (res.StatusCode == HttpStatusCode.NotFound)
                return false;

            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Aprimo search failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

            return true;
        }

        public async Task<IReadOnlyList<AprimoRecord>> GetAllAssetsAsync(CancellationToken ct = default)
        {
            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            var results = new List<AprimoRecord>();
            int page = 1;
            bool hasMore = true;

            const int max401RetriesPerPage = 3;

            while (hasMore)
            {
                //var url = $"{baseUrl}api/core/records";
                Console.WriteLine($"Processing page: {page}. total results: {results.Count}");
                var url = $"{baseUrl}api/core/records?page={page}&pageSize=1000";

                int attempt = 0;

                while (true)
                {
                    attempt++;

                    using var req = new HttpRequestMessage
                    {
                        RequestUri = new Uri(url),
                        Method = HttpMethod.Get,
                        Content = null
                    };

                    req.Headers.TryAddWithoutValidation("API-VERSION", "1");
                    req.Headers.TryAddWithoutValidation("select-record", "fields");  //"masterfilelatestversion, fields, preview"
                    req.Headers.TryAddWithoutValidation("select-fileversion", "filetype");
                    req.Headers.TryAddWithoutValidation("User-Agent", "Azure Aprimo Migration Connector");
                    req.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                    req.Headers.TryAddWithoutValidation("Accept", "*/*");
                    //req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                    if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        if (attempt > max401RetriesPerPage)
                        {
                            var err = await res.Content.ReadAsStringAsync(ct);
                            throw new HttpRequestException(
                                $"Aprimo returned 401 after {max401RetriesPerPage} retries. Body: {err}");
                        }

                        // Force refresh token and retry
                        await PrepareAuthAsync(ct).ConfigureAwait(false);
                        continue;
                    }

                    // For other failures, throw (and include body for debugging)
                    if (!res.IsSuccessStatusCode)
                    {
                        var err = await res.Content.ReadAsStringAsync(ct);
                        //throw new HttpRequestException(
                        //    $"Aprimo request failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {err}");
                        return results;
                    }

                    var json = await res.Content.ReadAsStringAsync(ct);
                    var pageResult = JsonConvert.DeserializeObject<AprimoRecordPagedCollection>(json);

                    if (pageResult?.Items == null || pageResult.Items.Length == 0)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        results.AddRange(pageResult.Items);

                        // Stop when we've retrieved all records
                        if (results.Count >= pageResult.TotalCount)
                            hasMore = false;
                        else
                            page++;
                    }

                    break; // success -> exit retry loop
                }
            }

            return results;
        }

        public async Task<IReadOnlyList<AprimoRecord>> GetImageSetsMissingPreviewAsync(CancellationToken ct = default)
        {
            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            var results = new List<AprimoRecord>();
            int page = 1;
            bool hasMore = true;

            const int max401RetriesPerPage = 3;

            var requestBody = new
            {
                searchExpression = new
                {
                    expression = " 'Asset Type' = '04c25cb5efbd49cd9e1db3f00033d745' AND fileCount = 0 "
                },
                logRequest = false
            };


            while (hasMore)
            {
                //var url = $"{baseUrl}api/core/records";
                Console.WriteLine($"Processing page: {page}. total results: {results.Count}");
                var url = $"{baseUrl}api/core/search/records?page={page}&pageSize=1000";

                int attempt = 0;

                while (true)
                {
                    attempt++;


                    var req = new HttpRequestMessage
                    {
                        RequestUri = new Uri(url),
                        Method = HttpMethod.Post,
                        Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-additionalfile", "metadata, uri" },
                        //{ System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { "select-record", "masterfilelatestversion, fields, preview" },
                        { "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "*/*" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Aprimo Migration Connector" },
                    },
                        Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"),
                    };


                    using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                    if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        if (attempt > max401RetriesPerPage)
                        {
                            var err = await res.Content.ReadAsStringAsync(ct);
                            throw new HttpRequestException(
                                $"Aprimo returned 401 after {max401RetriesPerPage} retries. Body: {err}");
                        }

                        // Force refresh token and retry
                        await PrepareAuthAsync(ct).ConfigureAwait(false);
                        continue;
                    }

                    // For other failures, throw (and include body for debugging)
                    if (!res.IsSuccessStatusCode)
                    {
                        var err = await res.Content.ReadAsStringAsync(ct);
                        //throw new HttpRequestException(
                        //    $"Aprimo request failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {err}");
                        return results;
                    }

                    var json = await res.Content.ReadAsStringAsync(ct);
                    var pageResult = JsonConvert.DeserializeObject<AprimoRecordPagedCollection>(json);

                    if (pageResult?.Items == null || pageResult.Items.Length == 0)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        results.AddRange(pageResult.Items);

                        // Stop when we've retrieved all records
                        if (results.Count >= pageResult.TotalCount)
                            hasMore = false;
                        else
                            page++;
                    }

                    break; // success -> exit retry loop
                }
            }

            return results;
        }

        public async Task<List<string>> GetAllAssetIdsAsync(CancellationToken ct = default)
        {
            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            var results = new List<string>();
            int page = 1;
            bool hasMore = true;

            const int max401RetriesPerPage = 3;

            while (hasMore)
            {
                //var url = $"{baseUrl}api/core/records";
                Console.WriteLine($"Processing page: {page}. total results: {results.Count}");
                var url = $"{baseUrl}api/core/records?page={page}&pageSize=1000";

                int attempt = 0;

                while (true)
                {
                    attempt++;

                    using var req = new HttpRequestMessage
                    {
                        RequestUri = new Uri(url),
                        Method = HttpMethod.Get,
                        Content = null
                    };

                    req.Headers.TryAddWithoutValidation("API-VERSION", "1");
                    req.Headers.TryAddWithoutValidation("select-record", "fields");
                    req.Headers.TryAddWithoutValidation("select-fileversion", "filetype");
                    req.Headers.TryAddWithoutValidation("User-Agent", "Azure Aprimo Migration Connector");
                    req.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                    req.Headers.TryAddWithoutValidation("Accept", "*/*");
                    //req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                    if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        if (attempt > max401RetriesPerPage)
                        {
                            var err = await res.Content.ReadAsStringAsync(ct);
                            throw new HttpRequestException(
                                $"Aprimo returned 401 after {max401RetriesPerPage} retries. Body: {err}");
                        }

                        // Force refresh token and retry
                        await PrepareAuthAsync(ct).ConfigureAwait(false);
                        continue;
                    }

                    // For other failures, throw (and include body for debugging)
                    if (!res.IsSuccessStatusCode)
                    {
                        var err = await res.Content.ReadAsStringAsync(ct);
                        //throw new HttpRequestException(
                        //    $"Aprimo request failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {err}");
                        return results;
                    }

                    var json = await res.Content.ReadAsStringAsync(ct);
                    var pageResult = JsonConvert.DeserializeObject<AprimoRecordPagedCollection>(json);

                    if (pageResult?.Items == null || pageResult.Items.Length == 0)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        List<string> resultStrings = new List<string>();
                        foreach (var item in pageResult.Items)
                        {
                            if(!string.IsNullOrWhiteSpace(item.GetSingleValue("productsAEMAssetID")))
                            {
                                resultStrings.Add($"{item.GetSingleValue("productsAEMAssetID")},{item.Id}");
                            } else
                            {
                                resultStrings.Add($"unstamped,{item.Id}");
                            }
                        }
                        results.AddRange(resultStrings);

                        // Stop when we've retrieved all records
                        if (results.Count >= pageResult.TotalCount)
                            hasMore = false;
                        else
                            page++;
                    }

                    break; // success -> exit retry loop
                }
            }

            return results;
        }

        public async Task<AprimoRecordCreated> UploadAzureBlobToAprimoAsync(BlobClient blobClient, string realFilename, string classificationId,CancellationToken ct = default)
        {
            if (blobClient == null) throw new ArgumentNullException(nameof(blobClient));
            string aprimoUniqueIdFieldName = "productsAEMAssetID"; 

            if (string.IsNullOrWhiteSpace(aprimoUniqueIdFieldName))
                throw new ArgumentException("Field name cannot be null/empty.", nameof(aprimoUniqueIdFieldName));

            // 1) Determine filename + unique id from blob name
            var blobName = blobClient.Name; // includes any virtual folders
            var fileName = Path.GetFileName(blobName);

            // Unique identifier from filename prefix: "{uuid}_filename.ext"
            // If there is no underscore, we fall back to filename without extension
            var uniqueId = ExtractUniqueIdFromFilename(fileName);

            // 2) Blob size + content type
            BlobProperties props = (await blobClient.GetPropertiesAsync(cancellationToken: ct)).Value;
            long sizeBytes = props.ContentLength;
            string contentType = props.ContentType ?? "application/octet-stream";

            // 3) Create Aprimo upload session (returns SAS upload URL + upload id)
            var uploadSession = await CreateUploadSessionAsync(fileName, sizeBytes, contentType, ct);

            // 4) Upload stream to the SAS URL
            if (sizeBytes <= TwentyMb)
            {
                // Small file: download fully into memory first (often faster; allows seek/retry)
                await using var ms = new MemoryStream(capacity: (int)Math.Min(sizeBytes, int.MaxValue));
                await blobClient.DownloadToAsync(ms, cancellationToken: ct);
                ms.Position = 0;

                await PutToAprimoUploadUrlAsync(
                    uploadSession.SasUrl,
                    ms,
                    contentType,
                    sizeBytes,
                    ct);
            }
            else
            {
                // Large file: stream directly (low memory)
                // OpenReadAsync provides a streaming read without buffering whole blob.
                await using var blobStream = await blobClient.OpenReadAsync(cancellationToken: ct);

                await PutToAprimoUploadUrlAsync(
                    uploadSession.SasUrl,
                    blobStream,
                    contentType,
                    sizeBytes,
                    ct);
            }

            // 5) Create Aprimo record
            // NOTE: does not do any stamping at this time
            var created = await CreateRecordFromUploadAsync(
                uploadSession.Token,
                title: realFilename,
                classificationId: classificationId,
                ct);

            return created;
        }

        public async Task<AprimoRecordCreated> UploadImageSetToAprimoAsync(string title, string classificationId, CancellationToken ct = default)
        {
            string aprimoUniqueIdFieldName = "productsAEMImageSetID";

            if (string.IsNullOrWhiteSpace(aprimoUniqueIdFieldName))
                throw new ArgumentException("Field name cannot be null/empty.", nameof(aprimoUniqueIdFieldName));


            // 5) Create Aprimo record
            // NOTE: does not do any stamping at this time
            var created = await CreateRecordFromImageSetAsync(
                title: title,
                classificationId: classificationId,
                ct);

            return created;
        }

        private async Task<AprimoUploadSession> CreateUploadSessionAsync(
            string fileName,
            long fileSize,
            string mimeType,
            CancellationToken ct)
        {
            // Aprimo DAM: POST /api/core/uploads (create upload session)
            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.Replace(".dam","").TrimEnd('/') + "/");
            var url = $"{_baseUrl}uploads";

            var payload = new
            {
                fileName = fileName,
                fileSize = fileSize,
                mimeType = mimeType
            };

            //using var req = new HttpRequestMessage(HttpMethod.Post, url);
            //req.Headers.TryAddWithoutValidation("API-Version", "1");
            //req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-additionalfile", "metadata, uri" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Azure Aprimo Migration Connector" },
                    },
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
            };

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"CreateUploadSession failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

            var session = JsonConvert.DeserializeObject<AprimoUploadSession>(body);
            if (session == null || string.IsNullOrWhiteSpace(session.Token) || string.IsNullOrWhiteSpace(session.SasUrl))
                throw new InvalidOperationException("Aprimo upload session response missing Token or SasUrl.");

            return session;
        }

        private static async Task PutToAprimoUploadUrlAsync(
            string uploadUrl,
            Stream contentStream,
            string contentType,
            long contentLength,
            CancellationToken ct)
        {
            // IMPORTANT: This is a PUT to the SAS URL (Azure). Do NOT send Aprimo bearer token.

            // Base handler (lowest in chain)
            var socketsHandler = new SocketsHttpHandler
            {
                // optional: keep defaults unless you have a reason to change
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };

            // Diagnostics handler sits "above" the base handler
            var diagnostics = new HttpClientDiagnosticsHandler("PutAprimoClient")
            {
                InnerHandler = socketsHandler
            };

            var http = new HttpClient(diagnostics)
            {
                Timeout = TimeSpan.FromMinutes(60) // ⏰ override global timeout
            };
            http.DefaultRequestHeaders.Add("X-Client-Name", "PutAprimoClient");

            using var req = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
            req.Content = new StreamContent(contentStream);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            req.Content.Headers.ContentLength = contentLength;
            // 🔴 REQUIRED by Azure Blob Storage
            req.Headers.Add("x-ms-blob-type", "BlockBlob");

            using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Upload PUT to Aprimo SAS URL failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {err}");
            }
        }

        private async Task<AprimoRecordCreated> CreateRecordFromUploadAsync(
            string uploadId,
            string title,
            string classificationId,
            CancellationToken ct)
        {
            // Aprimo DAM: POST /api/core/records (create record based on upload)
            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
            var url = $"{_baseUrl}api/core/records";

            // The exact payload shape depends on your Aprimo configuration.
            // This pattern is commonly used: uploadId + title + fields.
            var payload = new
            {
                title = title,
                uploadId = uploadId,
                classifications = new
                {
                    addOrUpdate = new[]
                    {
                        new
                        {
                            id = classificationId,  
                            sortIndex = 1  //int.MaxValue // 2147483647
                        }
                    }
                },
                files = new
                {
                    master = uploadId,
                    addOrUpdate = new[]
                    {
                        new
                        {
                            versions = new
                            {
                                addOrUpdate = new[]
                                {
                                    new { id = uploadId, filename = title }
                                }
                            }
                        }
                    }
                }
                /// dont stamp with metadata yet.
                //,
                //fields = new System.Collections.Generic.Dictionary<string, object>
                //{
                //{ uniqueIdFieldName, uniqueIdValue }
            };

            //using var req = new HttpRequestMessage(HttpMethod.Post, url);
            //req.Headers.TryAddWithoutValidation("API-Version", "1");
            //req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-additionalfile", "metadata, uri" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Azure Aprimo Migration Connector" },
                    },
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
            };

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"CreateRecord failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

            // Some tenants return a record object; some return a wrapper.
            // Adjust model as needed.
            var created = JsonConvert.DeserializeObject<AprimoRecordCreated>(body);
            if (created == null)
                throw new InvalidOperationException("CreateRecord response could not be deserialized.");

            return created;
        }

        private async Task<AprimoRecordCreated> CreateRecordFromImageSetAsync(
            string title,
            string classificationId,
            CancellationToken ct)
        {
            // Aprimo DAM: POST /api/core/records (create record based on upload)
            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
            var url = $"{_baseUrl}api/core/records";

            // The exact payload shape depends on your Aprimo configuration.
            // This pattern is commonly used: uploadId + title + fields.
            var payload = new
            {
                status = "draft",
                title = title,
                contentType = "Image Set",
                classifications = new
                {
                    addOrUpdate = new[]
                    {
                        new
                        {
                            id = classificationId,
                            sortIndex = 1  //int.MaxValue // 2147483647
                        }
                    }
                },
                /// dont stamp with metadata yet.
                //,
                //fields = new System.Collections.Generic.Dictionary<string, object>
                //{
                //{ uniqueIdFieldName, uniqueIdValue }
            };

            //using var req = new HttpRequestMessage(HttpMethod.Post, url);
            //req.Headers.TryAddWithoutValidation("API-Version", "1");
            //req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-additionalfile", "metadata, uri" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Azure Aprimo Migration Connector" },
                    },
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
            };

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"CreateRecord failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

            // Some tenants return a record object; some return a wrapper.
            // Adjust model as needed.
            var created = JsonConvert.DeserializeObject<AprimoRecordCreated>(body);
            if (created == null)
                throw new InvalidOperationException("CreateRecord response could not be deserialized.");

            return created;
        }

        public async Task<IReadOnlyDictionary<string, AprimoClassification>> GetAllClassificationsAsync(CancellationToken ct = default)
        {
            var result = new Dictionary<string, AprimoClassification>(
                StringComparer.OrdinalIgnoreCase);
            await PrepareAuthAsync(ct).ConfigureAwait(false);
            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            int page = 1;
            const int pageSize = 100;
            bool hasMore = true;
            int undefinedCount = 0;
            int duplicateCount = 0;
            while (hasMore)
            {
                var url =
                    $"{_baseUrl}api/core/classifications" +
                    $"?page={page}&pageSize={pageSize}";

                //using var req = new HttpRequestMessage(HttpMethod.Get, url);
                //req.Headers.TryAddWithoutValidation("API-Version", "1");
                //req.Headers.Accept.Add(
                //    new MediaTypeWithQualityHeaderValue("application/json"));

                var req = new HttpRequestMessage
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get,
                    Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-record", "masterfilelatestversion, fields, preview" },
                        { "select-classification", "parent, children" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "*/*" },
                        { "User-Agent", "Azure Aprimo Migration Connector" },
                    },
                    Content = null,
                };

                using var res = await _httpClient.SendAsync(
                    req,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);

                res.EnsureSuccessStatusCode();

                var json = await res.Content.ReadAsStringAsync(ct);
                var pageResult =
                    JsonConvert.DeserializeObject<AprimoClassificationPagedCollection>(json);

                if (pageResult?.Items == null || pageResult.Items.Count == 0)
                {
                    hasMore = false;
                    continue;
                } 

                foreach (var cls in pageResult.Items)
                {
                    if (!string.IsNullOrWhiteSpace(cls.Name))
                    {
                        // Keyed by name
                        if (result.ContainsKey(cls.Name))
                        {
                            duplicateCount++;
                            result[$"{cls.Name}_{duplicateCount}"] = cls;
                            ;
                        } else
                        {
                            result[cls.Name] = cls;
                        }
                    } else
                    {
                        undefinedCount++;
                        result[$"undefined_{undefinedCount}"] = cls;
                    }

                }

                if (result.Count >= pageResult.TotalCount)
                    hasMore = false;
                else
                    page++;
            }

            return result;
        }

        public async Task<IReadOnlyList<AprimoFieldDefinition>> GetAllDefinitionsAsync(CancellationToken ct = default)
        {
            var results = new List<AprimoFieldDefinition>(capacity: 512);


            await PrepareAuthAsync(ct).ConfigureAwait(false);
            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            int page = 1;
            const int pageSize = 100;
            bool hasMore = true;
            int undefinedCount = 0;
            int duplicateCount = 0;
            while (hasMore)
            {
                var url =
                    $"{_baseUrl}api/core/fielddefinitions" +
                    $"?page={page}&pageSize={pageSize}";

                var req = new HttpRequestMessage
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get,
                    Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-record", "masterfilelatestversion, fields, preview" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "*/*" },
                        { "User-Agent", "Azure Aprimo Migration Connector" },
                    },
                    Content = null,
                };

                using var res = await _httpClient.SendAsync(
                    req,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);

                res.EnsureSuccessStatusCode();

                var json = await res.Content.ReadAsStringAsync(ct);
                var pageResult =
                    JsonConvert.DeserializeObject<AprimoDefinitions>(json);

                if (pageResult?.Items == null || pageResult.Items.Count == 0)
                {
                    hasMore = false;
                    continue;
                }


                results.AddRange(pageResult.Items);

                if (results.Count >= pageResult.TotalCount)
                    hasMore = false;
                else
                    page++;


            }

            return results;
        }

        public async Task PrimeFieldIdCacheFromRecordAsync(string recordId, CancellationToken ct = default)
        {
            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
            var url = $"{_baseUrl}api/core/record/{Uri.EscapeDataString(recordId)}";

            //using var req = new HttpRequestMessage(HttpMethod.Get, url);
            //req.Headers.TryAddWithoutValidation("API-Version", "1");

            //// Select headers include subresources like fields/files/etc. :contentReference[oaicite:5]{index=5}
            //req.Headers.TryAddWithoutValidation("Select-record", "fields");

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        { "select-record", "masterfilelatestversion, fields, preview" },
                        //{ "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { "User-Agent", "Azure Aprimo Migration Connector" },
                    },
                Content = null,
            };


            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync(ct);
            var record = JsonConvert.DeserializeObject<AprimoRecordWithFields>(json);

            foreach (var f in record?.Fields?.Items ?? new List<AprimoField>())
            {
                if (!string.IsNullOrWhiteSpace(f.FieldName) && !string.IsNullOrWhiteSpace(f.Id))
                    _fieldNameToId[f.FieldName] = f.Id;
            }
            ;
        }

        public string GetRequiredFieldId(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name is required.", nameof(fieldName));

            if (!_fieldNameToId.TryGetValue(fieldName, out var fieldId) ||
                string.IsNullOrWhiteSpace(fieldId))
            {
                throw new InvalidOperationException(
                    $"Field ID for '{fieldName}' is not in cache. " +
                    $"Call PrimeFromRecordFields(...) before stamping metadata.");
            }

            return fieldId;
        }
        public async Task StampMetadataAsync(
            string recordId,
            IEnumerable<AprimoFieldUpsert> fieldsToUpsert,
            IEnumerable<AprimoFieldUpsert> fieldsToRemove,
            IEnumerable<AprimoFieldUpsert> classificationsToUpsert,
            IEnumerable<AprimoFieldUpsert> classificationsToRemove,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(recordId))
                throw new ArgumentException("recordId is required.", nameof(recordId));

            // NOTE: The update verb (PUT vs PATCH) depends on your Aprimo endpoint’s contract.
            // Many tenants use PUT /records/{id} for updates.
            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
            var url = $"{_baseUrl}api/core/record/{Uri.EscapeDataString(recordId)}";

            // build addOrUpdate entries
            var addOrUpdate = new JArray(
                fieldsToUpsert.Select(f =>
                {
                    var localized = new JObject
                    {
                        ["languageId"] = f.LanguageId
                    };

                    if (f.IsRecordLink)
                    {
                        // RecordLink uses children[] with { recordId }
                        var recordIds = (f.Values ?? Enumerable.Empty<string>())
                            .Where(v => !string.IsNullOrWhiteSpace(v))
                            .Select(v => v.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase);

                        localized["children"] = new JArray(
                            recordIds.Select(id => new JObject { ["recordId"] = id })
                        );

                        // If you ever need to support parents or links too, add them here:
                        // localized["parents"] = ...
                        // localized["links"] = ...
                    }
                    else if (f.Values != null)
                    {
                        // List-type fields (TextList / OptionList / ClassificationList / RecordList)
                        localized["values"] = new JArray(f.Values);
                    }
                    else
                    {
                        // Scalar fields (SingleLineText / MultiLineText / Numeric / Date / DateTime / Html)
                        localized["value"] = f.Value ?? string.Empty;
                    }


                    return new JObject
                    {
                        ["id"] = f.FieldId,
                        ["localizedValues"] = new JArray(localized)
                    };
                })
            );



            var payload = new JObject
            {
                ["fields"] = new JObject
                {
                    ["addOrUpdate"] = addOrUpdate
                }
            };

            if (fieldsToRemove.Any())
            {
                // build remove entries
                var remove = new JArray(
                    fieldsToRemove.Select(f =>
                    {
                        var localized = new JObject
                        {
                            ["languageId"] = f.LanguageId
                        };

                        if (f.Values != null)
                        {
                            // List-type fields (TextList / OptionList / ClassificationList / RecordList)
                            localized["values"] = new JArray(f.Values);
                        }
                        else
                        {
                            // Scalar fields (SingleLineText / MultiLineText / Numeric / Date / DateTime / Html)
                            localized["value"] = f.Value ?? string.Empty;
                        }


                        return new JObject
                        {
                            ["id"] = f.FieldId,
                            ["localizedValues"] = new JArray(localized)
                        };
                    })
                );

                payload["fields"]["remove"] = remove;
            }

            // Add classifications only when you have entries
            if ((classificationsToUpsert != null && classificationsToUpsert.Any()) ||
                (classificationsToRemove != null && classificationsToRemove.Any()))
            {
                var classificationsObj = new JObject();

                if (classificationsToUpsert != null && classificationsToUpsert.Any())
                {
                    classificationsObj["addOrUpdate"] = new JArray(
                        classificationsToUpsert.Select(c =>
                            new JObject
                            {
                                ["id"] = c.Value   // must be classification GUID
                            })
                    );
                }

                if (classificationsToRemove != null && classificationsToRemove.Any())
                {
                    classificationsObj["remove"] = new JArray(
                        classificationsToRemove.Select(c =>
                            new JObject
                            {
                                ["id"] = c.Value        // must be classification GUID
                            })
                    );
                }

                payload["classifications"] = classificationsObj;
            }


            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Put,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-record", "masterfilelatestversion, fields, preview" },
                        //{ "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Azure Aprimo Migration Connector" },
                    },
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
            };

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"StampMetadata failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");
        }

        public async Task ClearRecordLinkMetadataAsync(
            string recordId,
            IEnumerable<AprimoFieldUpsert> fieldsToUpsert,
            IEnumerable<AprimoFieldUpsert> classificationsToUpsert,
            IEnumerable<AprimoFieldUpsert> classificationsToRemove,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(recordId))
                throw new ArgumentException("recordId is required.", nameof(recordId));

            // NOTE: The update verb (PUT vs PATCH) depends on your Aprimo endpoint’s contract.
            // Many tenants use PUT /records/{id} for updates.
            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
            var url = $"{_baseUrl}api/core/record/{Uri.EscapeDataString(recordId)}";

            // build addOrUpdate entries
            var addOrUpdate = new JArray(
                fieldsToUpsert.Select(f =>
                {
                    var localized = new JObject
                    {
                        ["languageId"] = f.LanguageId
                    };

                    if (f.IsRecordLink)
                    {
                        // RecordLink uses children[] with { recordId }
                        var recordIds = (f.Values ?? Enumerable.Empty<string>())
                            .Where(v => !string.IsNullOrWhiteSpace(v))
                            .Select(v => v.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase);

                        localized["children"] = new JArray();  // clear the children

                        // If you ever need to support parents or links too, add them here:
                        // localized["parents"] = ...
                        // localized["links"] = ...
                    }
                    else if (f.Values != null)
                    {
                        // List-type fields (TextList / OptionList / ClassificationList / RecordList)
                        localized["values"] = new JArray(f.Values);
                    }
                    else
                    {
                        // Scalar fields (SingleLineText / MultiLineText / Numeric / Date / DateTime / Html)
                        localized["value"] = f.Value ?? string.Empty;
                    }


                    return new JObject
                    {
                        ["id"] = f.FieldId,
                        ["localizedValues"] = new JArray(localized)
                    };
                })
            );

            var payload = new JObject
            {
                ["fields"] = new JObject
                {
                    ["addOrUpdate"] = addOrUpdate
                }
            };



            // Add classifications only when you have entries
            if ((classificationsToUpsert != null && classificationsToUpsert.Any()) ||
                (classificationsToRemove != null && classificationsToRemove.Any()))
            {
                var classificationsObj = new JObject();

                if (classificationsToUpsert != null && classificationsToUpsert.Any())
                {
                    classificationsObj["addOrUpdate"] = new JArray(
                        classificationsToUpsert.Select(c =>
                            new JObject
                            {
                                ["id"] = c.Value   // must be classification GUID
                            })
                    );
                }

                if (classificationsToRemove != null && classificationsToRemove.Any())
                {
                    classificationsObj["remove"] = new JArray(
                        classificationsToRemove.Select(c =>
                            new JObject
                            {
                                ["id"] = c.Value        // must be classification GUID
                            })
                    );
                }

                payload["classifications"] = classificationsObj;
            }


            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Put,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-record", "masterfilelatestversion, fields, preview" },
                        //{ "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Azure Aprimo Migration Connector" },
                    },
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
            };

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"StampMetadata failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");
        }

        public async Task StampAemAssetIdAsync(
                string recordId,
                string aemAssetId,
                string fieldName = "productsAEMAssetID",
                string locale = "en-US",
                CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aemAssetId))
                throw new ArgumentException("aemAssetId is required.", nameof(aemAssetId));

            string fieldId = GetRequiredFieldId(fieldName);

            await EnsureLanguagesLoadedAsync(ct);

            string languageId = ResolveLanguageId(locale);

            IEnumerable<AprimoFieldUpsert> classificationsToUpsert = new List<AprimoFieldUpsert>();

            await StampMetadataAsync(
                recordId,
                new[]
                {
                    new AprimoFieldUpsert
                    {
                        FieldId = fieldId,
                        LanguageId = languageId,
                        Value = aemAssetId
                    }
                },
                classificationsToUpsert,
                classificationsToUpsert,
                classificationsToUpsert,
                ct);
        }

        public async Task StampAemImageSetIdAsync(
                string recordId,
                string aemImageSetId,
                string fieldName = "productsAEMImageSetID",
                string locale = "en-US",
                CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aemImageSetId))
                throw new ArgumentException("aemAssetId is required.", nameof(aemImageSetId));

            string fieldId = GetRequiredFieldId(fieldName);

            await EnsureLanguagesLoadedAsync(ct);

            string languageId = ResolveLanguageId(locale);

            IEnumerable<AprimoFieldUpsert> classificationsToUpsert = new List<AprimoFieldUpsert>();

            await StampMetadataAsync(
                recordId,
                new[]
                {
                    new AprimoFieldUpsert
                    {
                        FieldId = fieldId,
                        LanguageId = languageId,
                        Value = aemImageSetId
                    }
                },
                classificationsToUpsert,
                classificationsToUpsert,
                classificationsToUpsert,
                ct);
        }

        public async Task UploadFileToRecordAsync(
            string recordId,
            Stream fileStream,
            string fileName,
            string contentType,
            bool setAsPreview,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));

            if (fileStream.CanSeek)
                fileStream.Position = 0;

            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");


            long sizeBytes = fileStream.Length;

            // 3) Create Aprimo upload session (returns SAS upload URL + upload id)
            var uploadSession = await CreateUploadSessionAsync(fileName, sizeBytes, contentType, ct);

            // 4) Upload stream to the SAS URL
            if (sizeBytes <= TwentyMb)
            {
                await PutToAprimoUploadUrlAsync(
                    uploadSession.SasUrl,
                    fileStream,
                    contentType,
                    sizeBytes,
                    ct);
            }
            else
            {

                await PutToAprimoUploadUrlAsync(
                    uploadSession.SasUrl,
                    fileStream,
                    contentType,
                    sizeBytes,
                    ct);
            }

            var aprimoRecord = await GetAssetByAprimoIdAsync(recordId);

            var masterId = aprimoRecord.Embedded.Masterfile.Id;
            var masterVersion = aprimoRecord.Embedded.MasterfileLatestVersion.Id;

            var payload = new JObject
            {
                ["files"] = new JObject
                {
                    ["addOrUpdate"] = new JArray
                    {
                        new JObject
                        {
                            ["id"] = masterId,
                            ["versions"] = new JObject
                            {
                                ["addOrUpdate"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["id"] = masterVersion,
                                        ["Previews"] = new JObject
                                        {
                                            ["master"] = uploadSession.Token,
                                            ["addOrUpdate"] = new JArray
                                            {
                                                new JObject
                                                {
                                                    ["id"] = uploadSession.Token,
                                                    ["name"] = fileName
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            //var payload = new JObject
            //{
            //    ["masterFileLatestVersion"] = new JObject
            //    {
            //        ["Previews"] = new JObject
            //        {
            //            ["master"] = uploadSession.Token,
            //            ["addOrUpdate"] = new JArray
            //            {
            //                new JObject
            //                {
            //                    ["id"] = uploadSession.Token,
            //                    ["name"] = fileName
            //                }
            //            }
            //        }
            //    }
            //};

            var url = $"{_baseUrl}api/core/record/{Uri.EscapeDataString(recordId)}";

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Put,
                Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-record", "masterfilelatestversion, fields, preview" },
                        //{ "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { "User-Agent", "Azure Aprimo Migration Connector" },
                    },
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
            };

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"StampMetadata failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");
        }

        public async Task<string> UploadNewVersionFileToRecordAsync(
            string recordId,
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentNullException(nameof(contentType));

            if (fileStream.CanSeek)
                fileStream.Position = 0;

            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            // 1) Create upload session (SAS + token)
            long sizeBytes = fileStream.Length;
            var uploadSession = await CreateUploadSessionAsync(fileName, sizeBytes, contentType, ct).ConfigureAwait(false);

            // 2) PUT bytes to SAS URL
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            await PutToAprimoUploadUrlAsync(
                uploadSession.SasUrl,
                fileStream,
                contentType,
                sizeBytes,
                ct
            ).ConfigureAwait(false);

            var uploadToken = uploadSession.Token;

            // 3) Read record to locate its current master file id
            var aprimoRecord = await GetAssetByAprimoIdAsync(recordId).ConfigureAwait(false);
            var masterFileId = aprimoRecord.Embedded?.Masterfile?.Id;

            if (string.IsNullOrWhiteSpace(masterFileId))
                throw new InvalidOperationException($"Record {recordId} does not have an embedded masterfile id.");

            // 4) PUT record update that creates a NEW version with master = uploadToken
            //    IMPORTANT: omit version "id" to create a new version.

            var payload = new JObject
            {
                ["files"] = new JObject
                {
                    ["addOrUpdate"] = new JArray
                    {
                        new JObject
                        {
                            ["id"] = masterFileId, // existing master file id
                            ["versions"] = new JObject
                            {
                                ["addOrUpdate"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["id"] = uploadToken, 
                                        ["fileName"] = fileName // optional but usually good
                                        // you can also add comment/versionLabel/etc if your tenant supports it
                                    }
                                }
                            }
                        }
                    }
                }
            };


            var url = new Uri(baseUrl, $"api/core/record/{Uri.EscapeDataString(recordId)}");

            using var req = new HttpRequestMessage(HttpMethod.Put, url);
            req.Headers.Add("API-VERSION", "1");
            req.Headers.Add("User-Agent", "Azure Aprimo Migration Connector");
            req.Content = new StringContent(payload.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json");

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Create new version failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

            // NOTE:
            // Aprimo often doesn't return the new fileversion id on this PUT.
            // Return the upload token so caller can follow up by GET-ing latest version and correlating by fileName or timestamps.
            return uploadToken;
        }


        public async Task UploadPreviewFileToRecordAsync(
            string recordId,
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));
            if (fileStream is null)
                throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentNullException(nameof(contentType));

            if (fileStream.CanSeek)
                fileStream.Position = 0;

            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            long sizeBytes = fileStream.Length;

            // 1) Create Aprimo upload session (SAS URL + token)
            var uploadSession = await CreateUploadSessionAsync(fileName, sizeBytes, contentType, ct)
                .ConfigureAwait(false);

            // 2) Upload stream to the SAS URL
            await PutToAprimoUploadUrlAsync(
                uploadSession.SasUrl,
                fileStream,
                contentType,
                sizeBytes,
                ct).ConfigureAwait(false);

            // 3) Attach uploaded file as MASTER on the record.
            //    IMPORTANT: Do NOT send a "Previews" block here.
            //    Aprimo will generate/serve record preview + thumbnail from the master file version (as in your sample JSON).
            var payload = new JObject
            {
                ["files"] = new JObject
                {
                    // This is the key: treat your "preview image" as the record master.
                    ["master"] = uploadSession.Token,

                    // Some tenants like having the version explicitly added; it’s harmless and helps consistency.
                    ["addOrUpdate"] = new JArray
            {
                new JObject
                {
                    ["versions"] = new JObject
                    {
                        ["addOrUpdate"] = new JArray
                        {
                            new JObject
                            {
                                ["id"] = uploadSession.Token,
                                ["fileName"] = fileName
                            }
                        }
                    }
                }
            }
                }
            };

            var url = $"{baseUrl}api/core/record/{Uri.EscapeDataString(recordId)}";

            var req = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Put,
                Headers =
        {
            { "API-VERSION", "1" },
            { System.Net.HttpRequestHeader.ContentType.ToString(), "application/json" },
            { "User-Agent", "Azure Aprimo Migration Connector" },
        },
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
            };

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);

            var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"UploadPreviewFileToRecordAsync failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");
        }


        public async Task<string> UploadAdditionalFileToRecordAsync(
            string recordId,
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(recordId))
                throw new ArgumentNullException(nameof(recordId));
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentNullException(nameof(contentType));

            if (fileStream.CanSeek)
                fileStream.Position = 0;

            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            // 1) Upload to Aprimo upload session (gives SAS + upload TOKEN)
            long sizeBytes = fileStream.Length;
            var uploadSession = await CreateUploadSessionAsync(fileName, sizeBytes, contentType, ct).ConfigureAwait(false);

            // 2) PUT bytes to SAS URL
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            await PutToAprimoUploadUrlAsync(
                uploadSession.SasUrl,
                fileStream,
                contentType,
                sizeBytes,
                ct
            ).ConfigureAwait(false);

            // IMPORTANT:
            // The "id" you use below is the upload TOKEN returned by the upload session.
            // In many wrappers this is called UploadId or Token. It is NOT the recordId.
            var uploadToken = uploadSession.Token; // <-- rename if your model uses Token instead

            // 3) Read record so we know which master file + version to attach the additional file to
            var aprimoRecord = await GetAssetByAprimoIdAsync(recordId).ConfigureAwait(false);

            // These names match your posted object model; adjust if yours differs.
            var masterFileId = aprimoRecord.Embedded.Masterfile.Id;
            var masterLatestVersionId = aprimoRecord.Embedded.MasterfileLatestVersion.Id;

            // 4) Update the RECORD, embedding "additionalFiles" under the target fileversion
            // Per Aprimo docs, additionalFiles.addOrUpdate[].id can be an upload TOKEN (to create) or a GUID (to edit). :contentReference[oaicite:1]{index=1}
            var payload = new
            {
                // Including contentType is often safest with PUT; keep the existing record contentType.
                // If your GetAssetByAprimoIdAsync model has ContentType, use it:
                contentType = aprimoRecord.ContentType,

                files = new
                {
                    addOrUpdate = new[]
                    {
                new
                {
                    id = masterFileId,
                    versions = new
                    {
                        addOrUpdate = new[]
                        {
                            new
                            {
                                id = masterLatestVersionId,
                                additionalFiles = new
                                {
                                    addOrUpdate = new[]
                                    {
                                        new
                                        {
                                            id = uploadToken,        // <-- token from upload session
                                            label = Path.GetFileNameWithoutExtension(fileName),
                                            filename = fileName,     // docs use "filename" in the record update shape :contentReference[oaicite:2]{index=2}
                                            purposes = new[] { "Preview3D" },
                                            extension = Path.GetExtension(fileName),
                                            metadata = new[]
                                            {
                                                new { Key = "purpose",  Value = "3dpreview" },
                                                new { Key = "isManual", Value = "True" }
                                            },
                                            type = "ThreeDimensional"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
                }
            };

            var url = new Uri(baseUrl, $"api/core/record/{recordId}");

            using var req = new HttpRequestMessage(HttpMethod.Put, url);
            req.Headers.Add("API-VERSION", "1");
            req.Headers.Add("User-Agent", "Azure Aprimo Migration Connector");

            // If you want the response to include file/version info, you can add select-record/select-fileversion headers.
            // req.Headers.Add("select-record", "masterfilelatestversion,files");
            // req.Headers.Add("select-fileversion", "additionalfiles");

            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Create additional file failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");

            // At this point the additional file is queued/created.
            // Aprimo may not return the additional file GUID directly in the PUT response.
            // Easiest: return the uploadToken, or follow up with GET additionalfiles and find by filename/purpose.
            return uploadToken;
        }


    public async Task<Stream> Build3dPackageZipFromExistingRecordAsync(
        Stream glbStream,
        string glbFileName,
        Stream previewStream,
        string previewFileName,
        CancellationToken ct = default)
        {
            if (glbStream == null) throw new ArgumentNullException(nameof(glbStream));
            if (previewStream == null) throw new ArgumentNullException(nameof(previewStream));
            if (string.IsNullOrWhiteSpace(glbFileName)) throw new ArgumentNullException(nameof(glbFileName));
            if (string.IsNullOrWhiteSpace(previewFileName)) throw new ArgumentNullException(nameof(previewFileName));

            // Package configs you pasted use regex like: ^\\[^\\]+\.(glb)$
            // That means: the GLB must be at the ZIP ROOT (no folders).
            var glbEntryName = Path.GetFileName(glbFileName);
            var previewEntryName = Path.GetFileName(previewFileName);

            if (glbStream.CanSeek) glbStream.Position = 0;
            if (previewStream.CanSeek) previewStream.Position = 0;

            // Return a seekable stream for your upload session logic (Length is required).
            var zipStream = new MemoryStream();

            // Important: leaveOpen = true so we can reset Position and return the stream.
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                // GLB entry
                var glbEntry = archive.CreateEntry(glbEntryName, CompressionLevel.Optimal);
                await using (var entryStream = glbEntry.Open())
                {
                    await glbStream.CopyToAsync(entryStream, 81920, ct).ConfigureAwait(false);
                }

                // Preview entry
                var previewEntry = archive.CreateEntry(previewEntryName, CompressionLevel.Optimal);
                await using (var entryStream = previewEntry.Open())
                {
                    await previewStream.CopyToAsync(entryStream, 81920, ct).ConfigureAwait(false);
                }
            }

            zipStream.Position = 0;
            return zipStream;
        }


    //public async Task<Stream> Build3dPackageZipFromExistingRecordAsync(
    //    string recordId,
    //    CancellationToken ct = default)
    //    {
    //        if (string.IsNullOrWhiteSpace(recordId))
    //            throw new ArgumentNullException(nameof(recordId));

    //        await PrepareAuthAsync(ct).ConfigureAwait(false);

    //        var baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

    //        // 1) Read record (include masterfilelatestversion + preview in the response)
    //        var recordUrl = new Uri(baseUrl, $"api/core/record/{recordId}");
    //        using var recordReq = new HttpRequestMessage(HttpMethod.Get, recordUrl);
    //        recordReq.Headers.Add("API-VERSION", "1");
    //        recordReq.Headers.Add("User-Agent", "Azure Aprimo Migration Connector");

    //        // ask Aprimo to embed the objects we need; harmless if your tenant ignores it
    //        recordReq.Headers.Add("select-record", "masterfilelatestversion,preview,masterfile");

    //        using var recordRes = await _httpClient.SendAsync(recordReq, HttpCompletionOption.ResponseHeadersRead, ct)
    //            .ConfigureAwait(false);

    //        var recordJson = await recordRes.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    //        if (!recordRes.IsSuccessStatusCode)
    //            throw new HttpRequestException($"Get record failed: {(int)recordRes.StatusCode} {recordRes.ReasonPhrase}. Body: {recordJson}");

    //        var record = JObject.Parse(recordJson);

    //        // 2) Pull preview image signed URI (this is already a direct downloadable URL)
    //        var previewUri = record.SelectToken("_embedded.preview.uri")?.Value<string>();
    //        if (string.IsNullOrWhiteSpace(previewUri))
    //            throw new InvalidOperationException("Record did not include _embedded.preview.uri (preview image).");

    //        // 3) Pull master latest fileversion id (GLB)
    //        var masterLatestVersionId =
    //            record.SelectToken("_embedded.masterfilelatestversion.id")?.Value<string>()
    //            ?? record.SelectToken("_embedded.masterfileLatestVersion.id")?.Value<string>(); // defensive

    //        if (string.IsNullOrWhiteSpace(masterLatestVersionId))
    //            throw new InvalidOperationException("Record did not include _embedded.masterfilelatestversion.id.");

    //        // The record payload you pasted shows a link exists:
    //        // _embedded.masterfilelatestversion._links.publicuris.href
    //        var publicUrisHref =
    //            record.SelectToken("_embedded.masterfilelatestversion._links.publicuris.href")?.Value<string>();

    //        // Fallback if not embedded for some reason:
    //        if (string.IsNullOrWhiteSpace(publicUrisHref))
    //            publicUrisHref = new Uri(baseUrl, $"api/core/fileversion/{masterLatestVersionId}/publicuris").ToString();

    //        // 4) Resolve master GLB download URL via publicuris
    //        using var pubReq = new HttpRequestMessage(HttpMethod.Get, publicUrisHref);
    //        pubReq.Headers.Add("API-VERSION", "1");
    //        pubReq.Headers.Add("User-Agent", "Azure Aprimo Migration Connector");

    //        using var pubRes = await _httpClient.SendAsync(pubReq, HttpCompletionOption.ResponseHeadersRead, ct)
    //            .ConfigureAwait(false);

    //        var pubJson = await pubRes.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    //        if (!pubRes.IsSuccessStatusCode)
    //            throw new HttpRequestException($"Get publicuris failed: {(int)pubRes.StatusCode} {pubRes.ReasonPhrase}. Body: {pubJson}");

    //        var pubObj = JObject.Parse(pubJson);

    //        // Different tenants return different shapes; handle common ones.
    //        // Look for the first usable URL string in the response.
    //        var masterDownloadUrl =
    //            pubObj.SelectToken("items[0].uri")?.Value<string>()
    //            ?? pubObj.SelectToken("items[0].url")?.Value<string>()
    //            ?? pubObj.SelectToken("uri")?.Value<string>()
    //            ?? pubObj.SelectToken("url")?.Value<string>();

    //        if (string.IsNullOrWhiteSpace(masterDownloadUrl))
    //            throw new InvalidOperationException("Could not find a downloadable master URI in publicuris response.");

    //        // 5) Download both as byte[] (easiest + safe for ZipArchive)
    //        async Task<byte[]> DownloadBytesAsync(string url)
    //        {
    //            using var req = new HttpRequestMessage(HttpMethod.Get, url);
    //            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
    //                .ConfigureAwait(false);

    //            if (!res.IsSuccessStatusCode)
    //            {
    //                var b = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    //                throw new HttpRequestException($"Download failed ({(int)res.StatusCode} {res.ReasonPhrase}): {url}. Body: {b}");
    //            }

    //            return await res.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    //        }

    //        var glbBytes = await DownloadBytesAsync(masterDownloadUrl).ConfigureAwait(false);
    //        var previewBytes = await DownloadBytesAsync(previewUri).ConfigureAwait(false);

    //        // 6) Choose names (keep your own naming rules if you prefer)
    //        var masterFileName =
    //            record.SelectToken("_embedded.masterfilelatestversion.fileName")?.Value<string>()
    //            ?? $"master-{recordId}.glb";

    //        // If you already have a filename you want for preview, use it.
    //        // Otherwise keep it simple; your package previewRegex matches by extension.
    //        var previewFileName = "preview.png";

    //        // 7) Build ZIP with required structure:
    //        // Root: master GLB + preview image
    //        // additional/: duplicate GLB
    //        var zipMs = new MemoryStream();
    //        using (var zip = new ZipArchive(zipMs, ZipArchiveMode.Create, leaveOpen: true))
    //        {
    //            // root master
    //            var masterEntry = zip.CreateEntry(masterFileName, CompressionLevel.Optimal);
    //            await using (var es = masterEntry.Open())
    //            {
    //                await es.WriteAsync(glbBytes, 0, glbBytes.Length, ct).ConfigureAwait(false);
    //            }

    //            // root preview
    //            var previewEntry = zip.CreateEntry(previewFileName, CompressionLevel.Optimal);
    //            await using (var es = previewEntry.Open())
    //            {
    //                await es.WriteAsync(previewBytes, 0, previewBytes.Length, ct).ConfigureAwait(false);
    //            }

    //            // additional folder GLB (this is what triggers your <add regex="\\(.*\.glb$)" purpose="3dpreview" />)
    //            var additionalEntry = zip.CreateEntry($"additional/{masterFileName}", CompressionLevel.Optimal);
    //            await using (var es = additionalEntry.Open())
    //            {
    //                await es.WriteAsync(glbBytes, 0, glbBytes.Length, ct).ConfigureAwait(false);
    //            }
    //        }

    //        zipMs.Position = 0;
    //        return zipMs;
    //    }


    public async Task<IReadOnlyDictionary<string, string>> GetLanguageCodeToIdMapAsync(CancellationToken ct = default)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            int page = 1;
            const int pageSize = 100;
            bool more = true;

            await PrepareAuthAsync(ct).ConfigureAwait(false);

            var _baseUrl = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

            while (more)
            {
                var url =
                    $"{_baseUrl}api/core/languages" +
                    $"?page={page}&pageSize={pageSize}";

                var req = new HttpRequestMessage
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get,
                    Headers =
                    {
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        //{ "select-record", "masterfilelatestversion, fields, preview" },
                        //{ "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { "User-Agent", "Azure Aprimo Migration Connector" },
                    },
                    Content = null,
                };

                using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                res.EnsureSuccessStatusCode();

                var json = await res.Content.ReadAsStringAsync(ct);
                var pageResult = JsonConvert.DeserializeObject<AprimoLanguagePagedResult>(json);

                if (pageResult?.Items == null || pageResult.Items.Count == 0)
                    break;

                foreach (var lang in pageResult.Items)
                {
                    if (!string.IsNullOrWhiteSpace(lang.Code) &&
                        !string.IsNullOrWhiteSpace(lang.Id))
                    {
                        result[lang.Code] = lang.Id;
                    }
                }

                if (result.Count >= pageResult.TotalCount)
                    more = false;
                else
                    page++;
            }

            return result;
        }

        public async Task EnsureLanguagesLoadedAsync(CancellationToken ct = default)
        {
            if (_languageCodeToId != null)
                return;

            var map = await GetLanguageCodeToIdMapAsync(ct);
            _languageCodeToId = map;

            // Pick default language if needed
            _defaultLanguageId =
                map.TryGetValue("en-US", out var en)
                    ? en
                    : map.Values.FirstOrDefault();
        }

        public string ResolveLanguageId(string? languageCode = null)
        {
            if (_languageCodeToId == null)
                throw new InvalidOperationException("Languages not loaded. Call EnsureLanguagesLoadedAsync first.");

            if (!string.IsNullOrWhiteSpace(languageCode) &&
                _languageCodeToId.TryGetValue(languageCode, out var id))
            {
                return id;
            }

            if (!string.IsNullOrWhiteSpace(_defaultLanguageId))
                return _defaultLanguageId;

            throw new InvalidOperationException("No languageId could be resolved.");
        }
        private static string ExtractUniqueIdFromFilename(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            var underscoreIndex = fileName.IndexOf('_');
            if (underscoreIndex > 0)
                return fileName.Substring(0, underscoreIndex);

            return Path.GetFileNameWithoutExtension(fileName);
        }
    }



}
