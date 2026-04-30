using Migration.Connectors.Targets.Bynder.Configuration;
using System.Data;


using Migration.Shared.Storage;
using Migration.Connectors.Targets.Bynder.Models;

using Bynder.Sdk.Query.Asset;
using Bynder.Sdk.Query.Collection;

using Bynder.Sdk.Service;
using Bynder.Sdk.Service.Asset;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OfficeOpenXml;
using Migration.Shared.Configuration.Hosts.Bynder;

namespace Migration.Connectors.Targets.Bynder.Services
{
    public class BynderReportingService
    {

        private readonly ILogger<BynderReportingService> _logger;
        private readonly IOptions<BynderOptions> _bynderOptions;
        private readonly IOptions<BynderHostOptions> _hostOptions;
        private readonly IBynderClient _bynderClient;
        private readonly IMemoryCache _memoryCache;
        private readonly IAssetService _assetService;
        private readonly AssetResiliencyService _assetResiliencyService;

        private DataTable _successTable = new DataTable("Success");
        private DataTable _retryTable = new DataTable("Retry");
        private List<string> _logOutput = new List<string>();
        private Dictionary<string, string> _allAssets = new Dictionary<string, string>();
        private List<string> _allAssetIds = new List<string>();
        private Dictionary<string, string> _collectionIds = new Dictionary<string, string>();
        private List<Dictionary<string, string>> _successes = new List<Dictionary<string, string>>();
        private List<Dictionary<string, string>> _failures = new List<Dictionary<string, string>>();
        private static string[] _ignoreColumns = new string[] { };
        private static string[] _ignoreValidationColumns = new string[] { };
        private Dictionary<string, BynderMetaProperty> _metaProperties = new Dictionary<string, BynderMetaProperty>();

        private int _knownAssetPages = 0;
        private long _maxBytes = 800L * 1024 * 1024; // 800 MB

        private static string _logFilename;
        private static string _successRetryFilename;
        private static string _blankMetadataTemplate;
        private static string _metadataFilename;
        private static string _metadataPropertiesFilename;
        private static string _clientMetadataTemplateFilename;
        private static string _metadataTemplateFilename;

        private string _sourceDirectory;
        private string _tempDirectory;

        private AzureBlobWrapperAsync _metadataWrapper;
        private AzureBlobWrapperAsync _logsWrapper;
        private AzureBlobWrapperAsync _assetsWrapper;

        public BynderReportingService(
            ILogger<BynderReportingService> logger,
            IOptions<BynderOptions> bynderOptions,
            IOptions<BynderHostOptions> hostOptions,
            IBynderClient bynderClient,
            IMemoryCache memoryCache,
            AssetResiliencyService assetResiliencyService)
        {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bynderOptions = bynderOptions ?? throw new ArgumentNullException(nameof(bynderOptions));
            _hostOptions = hostOptions ?? throw new ArgumentNullException(nameof(hostOptions));
            _bynderClient = bynderClient ?? throw new ArgumentNullException(nameof(bynderClient));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _assetResiliencyService = assetResiliencyService ?? throw new ArgumentNullException(nameof(assetResiliencyService));

            _assetService = _bynderClient.GetAssetService();

            _tempDirectory = _hostOptions.Value.Paths.TempDirectory ?? Path.GetTempPath();
            _sourceDirectory = _hostOptions.Value.Paths.SourceDirectory ?? Path.GetTempPath();

            _blankMetadataTemplate = _hostOptions.Value.Files.BlankMetadataTemplate ?? "blank_metadata_template.xlsx";
            _metadataFilename = _hostOptions.Value.Files.MetadataFilename ?? "BynderWebDamImport_ntara.xlsx";
            _logFilename = _hostOptions.Value.Files.LogFilename ?? "bynder_migration_log.txt";
            _successRetryFilename = _hostOptions.Value.Files.SuccessRetryFilename ?? "successRetryMetadata.xlsx";
            _metadataPropertiesFilename = _hostOptions.Value.Files.MetadataPropertiesFile ?? "Bynder_metadata_properties.xlsx";
            _clientMetadataTemplateFilename = _hostOptions.Value.Files.ClientMetadataTemplateFile ?? "client-metadata-template.xlsx";
            _metadataTemplateFilename = _hostOptions.Value.Files.MetadataTemplateFile ?? "BynderWebDamImport_template.xlsx";


            _ignoreColumns = _hostOptions.Value.Columns.IgnoreColumns.ToArray();
            _ignoreValidationColumns = _hostOptions.Value.Columns.IgnoreValidationColumns.ToArray();

            _maxBytes = _hostOptions.Value.Batch.MaxBytes > 0 ? _hostOptions.Value.Batch.MaxBytes : _maxBytes;
            _knownAssetPages = _hostOptions.Value.Batch.KnownAssetPages > 0 ? _hostOptions.Value.Batch.KnownAssetPages : _knownAssetPages;

        }

        public void CombineRetryExcelFiles()
        {

            var excelFiles = Directory.GetFiles(_sourceDirectory + "restamp\\updates\\logs", "*.xlsx");
            if (excelFiles.Length == 0)
                throw new FileNotFoundException("No Excel files found in the source directory.");

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Combined");

                int outputRow = 1;
                bool headerWritten = false;

                foreach (var file in excelFiles)
                {
                    using (var inputPackage = new ExcelPackage(new FileInfo(file)))
                    {
                        var inputWorksheet = inputPackage.Workbook.Worksheets["Retry"];
                        int rowCount = inputWorksheet.Dimension.Rows;
                        int colCount = inputWorksheet.Dimension.Columns;

                        _logger.LogInformation($" file {file} has {rowCount - 1} Retries.");

                        int startRow = 1;
                        if (headerWritten)
                            startRow = 2; // Skip header row for subsequent files

                        // Write rows to output
                        for (int row = startRow; row <= rowCount; row++)
                        {
                            for (int col = 1; col <= colCount; col++)
                            {
                                worksheet.Cells[outputRow, col].Value = inputWorksheet.Cells[row, col].Value;
                            }
                            outputRow++;
                        }



                        // After the first file, don't write header again
                        if (!headerWritten)
                            headerWritten = true;
                    }
                }

                // Save to output
                package.SaveAs(new FileInfo(_sourceDirectory + "restamp\\updates\\logs\\combined-retry-import.xlsx"));
            }
        }

        public async Task DoReportingTasks(CancellationToken cancellationToken)
        {
            //// TODO : handle menu for containers and filenames

            //await PopulateCollectionIds();

            //await GetAllAssetIds();

            //_logger.LogInformation($"Found {_allAssetIds.Count} Assets");
            ;
            //_logger.LogInformation("Getting Meta Properties.");
            //var metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            //_metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);

            var assets = await _bynderClient.GetAssetService().GetMediaListAsync(new MediaQuery()
            {
                MetaProperties = new Dictionary<string, IList<string>>
                    {
                        {
                            "Sitecore_ID", ["10000008"]
                        }
                    }
            });

            ;
            string assetId = "127C680E-856B-4D37-9C362115B40231FE";

            var mediaInformationQuery = new MediaInformationQuery
            {
                MediaId = assetId
            };

            var mediaInfo = await _bynderClient.GetAssetService().GetMediaInfoAsync(mediaInformationQuery);
            if (mediaInfo != null)
            {

                var filename = mediaInfo.Name;
                string fileExt = string.Empty;

                try
                {
                    fileExt = mediaInfo.Extension[0];
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"could not get Extension from mediaInfo: {ex.Message}");
                }

                // get existing metaproperty options
                Dictionary<string, IList<string>> metaPropertyOptions = new Dictionary<string, IList<string>>();

                if (mediaInfo.PropertyOptionsDictionary != null)
                {
                    foreach (var propertyKey in mediaInfo.PropertyOptionsDictionary.Keys)
                    {
                        _logger.LogInformation($"Property option in dictionary: {propertyKey}: {mediaInfo.PropertyOptionsDictionary[propertyKey].ToString()}");
                        var extmetaProperty = _metaProperties.FirstOrDefault(x => x.Key == propertyKey.Replace("property_", "")).Value;
                        metaPropertyOptions.Add(extmetaProperty.Id, new List<string>() { string.Join(',', mediaInfo.PropertyOptionsDictionary[propertyKey].Select(a => a.ToString())) });
                        ;
                    }
                }

                ;
            }
            else
            {
                _logger.LogInformation($"processing failed: {assetId} does not exist in Bynder assets");
            }
        }

        public async Task FindDuplicatesInBynder()
        {
            string file = $"{_sourceDirectory}everything.txt";

            string[] lines = File.ReadAllLines(file);

            foreach (var line in lines)
            {
                string uniqueId = line;
                //_logger.LogInformation($"Processing {uniqueId}");
                try
                {
                    var foundAssets = await _bynderClient.GetAssetService().GetMediaListAsync(new MediaQuery()
                    {
                        MetaProperties = new Dictionary<string, IList<string>>
                        {
                            {
                                "OriginId", [uniqueId]
                            }
                        }
                    });

                    if (foundAssets.Count() > 1)
                    {
                        if (DateTime.Parse(foundAssets[0].DateModified.ToString()) > DateTime.Parse(foundAssets[1].DateModified.ToString()))
                        {
                            await _bynderClient.GetAssetService().DeleteAssetAsync(foundAssets[1].Id);
                        }
                        else
                        {
                            await _bynderClient.GetAssetService().DeleteAssetAsync(foundAssets[0].Id);
                        }
                        _logger.LogInformation("Removed Dupe for " + line);

                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"Oh No! : {e.Message}");
                }
            }


        }

        #region |-- Loading Methods --|
        private async Task PopulateCollectionIds()
        {
            var collections = await _bynderClient.GetCollectionService().GetCollectionsAsync(new GetCollectionsQuery
            {
                Limit = 100
            });

            foreach (var collection in collections)
            {
                if (!_collectionIds.ContainsKey(collection.Name))
                {
                    _collectionIds.Add(collection.Name, collection.Id);
                }
            }
            ;
        }

        private async Task GetAllAssets()
        {

            int pages = _knownAssetPages;
            for (var i = 0; i < pages; i++)
            {
                var assets = await _bynderClient.GetAssetService().GetMediaListAsync(new MediaQuery()
                {
                    Limit = 1000,
                    Page = i + 1,
                });
                foreach (var asset in assets)
                {
                    _allAssets[asset.Name] = asset.Id;
                }
            }
        ;
        }

        private async Task GetAllAssetIds()
        {
            int pages = _knownAssetPages;
            for (var i = 0; i < pages; i++)
            {
                var assets = await _bynderClient.GetAssetService().GetMediaListAsync(new MediaQuery()
                {
                    Limit = 1000,
                    Page = i + 1,
                });
                foreach (var asset in assets)
                {
                    _allAssetIds.Add(asset.Id);
                }
            }
        ;
        }

        #endregion |-- Loading Methods --|


    }
}