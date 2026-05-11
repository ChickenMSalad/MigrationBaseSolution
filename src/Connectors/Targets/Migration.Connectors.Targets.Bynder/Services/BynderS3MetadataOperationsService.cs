using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Connectors.Sources.S3.Clients;
using Migration.Connectors.Targets.Bynder.Clients;
using Migration.Shared.Files;
using Migration.Connectors.Targets.Bynder.Models;
using Bynder.Sdk.Query.Asset;
using Bynder.Sdk.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Migration.Shared.Configuration.Hosts.Bynder;

namespace Migration.Connectors.Targets.Bynder.Services
{
    public class BynderS3MetadataOperationsService
    {
        private readonly ILogger<BynderS3MetadataOperationsService> _logger;
        private readonly IOptions<BynderOptions> _bynderOptions;
        private readonly IBynderClient _bynderClient;
        private readonly IMemoryCache _memoryCache;
        private readonly S3Storage _s3Storage;
        private readonly IConsoleReaderService _reader;
        private readonly IOptions<BynderHostOptions> _hostOptions;

        private readonly string _blankMetadataTemplate;
        private readonly string _sourceDirectory;
        private Dictionary<string, BynderMetaProperty> _metaProperties = new();

        public BynderS3MetadataOperationsService(
            ILogger<BynderS3MetadataOperationsService> logger,
            IOptions<BynderOptions> bynderOptions,
            IOptions<BynderHostOptions> hostOptions,
            IBynderClient bynderClient,
            IMemoryCache memoryCache,
            IConsoleReaderService reader,
            S3Storage s3Storage)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bynderOptions = bynderOptions ?? throw new ArgumentNullException(nameof(bynderOptions));
            _bynderClient = bynderClient ?? throw new ArgumentNullException(nameof(bynderClient));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _s3Storage = s3Storage ?? throw new ArgumentNullException(nameof(s3Storage));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _hostOptions = hostOptions ?? throw new ArgumentNullException(nameof(hostOptions));
            _sourceDirectory = _hostOptions.Value.Paths.SourceDirectory ?? Path.GetTempPath();
            _blankMetadataTemplate = _hostOptions.Value.Files.BlankMetadataTemplate ?? "blank_metadata_template.xlsx";
        }

        private async Task<string> PromptForFilenameAsync(string promptMessage, string defaultFilename)
        {
            _logger.LogInformation($"{promptMessage} (Default is {defaultFilename})");
            var input = await _reader.ReadInputAsync();
            return string.IsNullOrWhiteSpace(input) ? defaultFilename : input;
        }

        public async Task PopulateMetadataTemplateFromAzure()
        {
            var blankTemplateFilename = await PromptForFilenameAsync("Please enter the Output Template filename", _blankMetadataTemplate);
            var metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            _metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);
            _logger.LogInformation($"Reading {blankTemplateFilename} from Azure.");
            MemoryStream ms = new();
            await using var s = await _s3Storage.OpenReadAsync(blankTemplateFilename);
            s.CopyTo(ms);
            ms.Position = 0;
            var metadataTable = ExcelReader.LoadExcelWorksheetsToDataTables(ms).FirstOrDefault() ?? new DataTable();
            var batchBlobs = await _s3Storage.ListKeysAsync();
            foreach (var blob in batchBlobs)
            {
                string fileName = Path.GetFileName(blob);
                string folderName = Path.GetDirectoryName(blob) ?? string.Empty;
                string extension = Path.GetExtension(blob).Replace(".", "");
                string[] parts = folderName.Split("\\");
                int depth = parts.Length;
                DataRow dr = metadataTable.NewRow();
                dr["OriginId"] = GetSha256Hash(blob);
                dr["File_Type"] = extension.ToLowerInvariant();
                dr["Brand"] = GetMatchingValue(blob, "Brand");
                dr["Aspect_Ratio"] = GetMatchingValue(blob, "Aspect_Ratio");
                dr["Asset_Type"] = GetAssetType(blob, extension);
                dr["Asset_Sub-Type"] = GetAssetSubType(blob, extension);
                int n = depth > 1 ? 2 : 1;
                dr["Keywords"] = string.Join(",", parts.Take(Math.Min(parts.Length, n)));
                dr["AssetFolder"] = folderName.Replace("\\", "/");
                dr["AzureFilename"] = fileName;
                dr["Filename"] = fileName;
                dr["City"] = GetMatchingValue(blob, "City");
                dr["Environment"] = GetMatchingValue(blob, "Environment");
                dr["Photography_Action"] = GetMatchingValue(blob, "Photography_Action");
                dr["Product_Category"] = GetMatchingValue(blob, "Product_Category");
                dr["Product_Gender"] = GetMatchingValue(blob, "Product_Gender");
                dr["Quarter"] = GetMatchingValue(blob, "Quarter");
                dr["Region"] = GetMatchingValue(blob, "Region");
                dr["Season"] = GetMatchingValue(blob, "Season");
                dr["State"] = GetMatchingValue(blob, "State");
                dr["Year"] = GetMatchingValue(blob, "Year");
                metadataTable.Rows.Add(dr);
            }
            var stream = ExcelWriter.WriteDataTable(metadataTable);
            var outputFileName = $"{_sourceDirectory}populatedMetadata.xlsx";
            using var fsOut = new FileStream(outputFileName, FileMode.Create, FileAccess.Write);
            stream.WriteTo(fsOut);
        }

        public async Task DeleteUnwantedAssets()
        {
            string folderPath = "2024 LIfestyle Images Folder/B-roll video/";
            var assetBlobs = await _s3Storage.ListKeysAsync(folderPath);
            int deletedCount = 0;
            foreach (var assetBlob in assetBlobs)
            {
                string hsh = GetSha256Hash(assetBlob);
                _logger.LogInformation($"found {hsh} : {assetBlob}");
                var foundAssets = await _bynderClient.GetAssetService().GetMediaListAsync(new MediaQuery()
                {
                    MetaProperties = new Dictionary<string, IList<string>> { { "OriginId", [hsh] } }
                });
                if (foundAssets.Any())
                {
                    foreach (var asset in foundAssets)
                    {
                        deletedCount++;
                        await _bynderClient.GetAssetService().DeleteAssetAsync(asset.Id);
                        _logger.LogInformation($"deleted asset {asset.Id}");
                    }
                }
            }
            _logger.LogInformation($"deleted {deletedCount} assets");
        }

        public async Task GetBynderCompleteReport()
        {
            string blankTemplate = $"{_sourceDirectory}BlankMetadataTemplate.xlsx";
            var batchBlobs = await _s3Storage.ListKeysAsync();
            _logger.LogInformation("Getting Meta Properties.");
            var metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            var metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);
            var dataTable = ExcelReader.LoadExcelWorksheetsToDataTables(new FileInfo(blankTemplate), true).FirstOrDefault() ?? new DataTable();
            List<string> errors = new();
            foreach (var assetBlob in batchBlobs)
            {
                string azurefileName = Path.GetFileName(assetBlob);
                int lastSlash = assetBlob.LastIndexOf('/');
                string folderName = lastSlash < 0 ? "/" : assetBlob.Substring(0, lastSlash) + "/";
                string originId = GetSha256Hash(assetBlob);
                try
                {
                    var foundAssets = await _bynderClient.GetAssetService().GetMediaListAsync(new MediaQuery()
                    {
                        MetaProperties = new Dictionary<string, IList<string>> { { "OriginId", [originId] } }
                    });
                    // preserve original best-effort behavior: only log count and continue
                    _logger.LogDebug("Found {Count} assets for {OriginId}", foundAssets.Count(), originId);
                }
                catch (Exception ex)
                {
                    errors.Add($"{azurefileName}: {ex.Message}");
                }
            }
            if (errors.Count > 0)
            {
                _logger.LogWarning("Completed report with {Count} errors", errors.Count);
            }
        }

        private string GetAssetSubType(string blobName, string extension)
        {
            string assetSubType = string.Empty;
            string ext = extension.ToLowerInvariant();
            string[] font = [ "ttf", "otf", "woff", "woff2", "eot", "svg" ];
            if (font.Contains(ext)) assetSubType = "Font";
            else if (ext == "ico") assetSubType = "Icon";
            else
            {
                string[] folderParts = blobName.Split('/');
                var metaPropertyOptions = _metaProperties["Asset_Sub-Type"].Options;
                Array.Reverse(folderParts);
                foreach (var part in folderParts)
                {
                    foreach (var metaProperty in metaPropertyOptions)
                    {
                        string label = metaProperty.Label;
                        string name = metaProperty.Name;
                        string cleanLabel = label.Replace("-", "");
                        string underscoreLabel = label.Replace("-", "_");
                        if (part.Contains(name, StringComparison.OrdinalIgnoreCase) || part.Contains(label, StringComparison.OrdinalIgnoreCase) || part.Contains(cleanLabel, StringComparison.OrdinalIgnoreCase) || part.Contains(underscoreLabel, StringComparison.OrdinalIgnoreCase))
                        {
                            assetSubType = metaProperty.Name;
                            break;
                        }
                    }
                    if (assetSubType.Equals(string.Empty))
                    {
                        foreach (var metaProperty in metaPropertyOptions)
                        {
                            string label = metaProperty.Label;
                            string cleanLabel = label.Replace("-", "");
                            string[] labelParts = cleanLabel.Split(" ");
                            var valueNames = labelParts.Where(o => Regex.IsMatch(blobName, $@"(^|[_\-\s/]){Regex.Escape(o)}([_\-\s/]|$)", RegexOptions.IgnoreCase) || (o.Contains("shot", StringComparison.OrdinalIgnoreCase) && part.Contains("shoot", StringComparison.OrdinalIgnoreCase))).ToList();
                            if (valueNames.Count > 0) { assetSubType = metaProperty.Name; break; }
                        }
                    }
                }
            }
            return assetSubType == string.Empty ? "Marketing_Asset" : assetSubType;
        }

        private string GetAssetType(string blobName, string extension)
        {
            string assetSubType = string.Empty;
            string ext = extension.ToLowerInvariant();
            string[] audio = [ "mp3", "wav", "aac", "flac", "aiff", "ogg" ];
            string[] graphics = [ "svg", "ai", "ait", "eps", "gif" ];
            string[] photography = [ "tiff", "tif", "heic", "heif", "jpeg", "jpg", "webp", "indd" ];
            string[] document = [ "doc", "docx", "odt", "pdf", "rtf", "txt", "htm", "html", "xls", "xlsx", "ods", "ppt", "pptx", "zip", "csv", "json", "xml", "srt" ];
            string[] video = [ "mp4", "mov", "avi", "wmv", "mkv", "webm", "flv" ];
            if (blobName.ToLowerInvariant().Contains("logo")) assetSubType = "Logos";
            else if (audio.Contains(ext)) assetSubType = "Audio";
            else if (graphics.Contains(ext)) assetSubType = "Graphics";
            else if (photography.Contains(ext)) assetSubType = "Photography";
            else if (document.Contains(ext)) assetSubType = "Documents";
            else if (video.Contains(ext)) assetSubType = "Videos";
            else assetSubType = "Photography";
            return assetSubType;
        }

        private string GetMatchingValue(string blobName, string metaPropertyName)
        {
            var metaPropertyOptions = _metaProperties[metaPropertyName].Options;
            var valueNames = metaPropertyOptions
                .Where(o => Regex.IsMatch(blobName, $@"(^|[_\-\s/]){Regex.Escape(o.Label)}([_\-\s/]|$)", RegexOptions.IgnoreCase))
                .Select(o => o.Name)
                .ToList();
            return GetFirstNotOnOrOnlyEntry(valueNames);
        }

        private string GetFirstNotOnOrOnlyEntry(List<string> items)
        {
            if (items == null || items.Count == 0) return string.Empty;
            if (items.Count == 1) return items[0];
            return items.FirstOrDefault(s => !string.Equals(s, "on", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        }

        private static string GetSha256Hash(string blobName)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(blobName);
            var hashBytes = sha256.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (var b in hashBytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
