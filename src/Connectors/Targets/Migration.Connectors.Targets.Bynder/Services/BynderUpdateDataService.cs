using Azure.Data.Tables;
using Bynder.Sdk.Query.Asset;
using Bynder.Sdk.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Connectors.Sources.S3.Clients;
using Migration.Connectors.Targets.Bynder.Clients;
using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Connectors.Targets.Bynder.Extensions;
using Migration.Connectors.Targets.Bynder.Models;
using Migration.Shared.Configuration.Hosts.Bynder;
using Migration.Shared.Files;
using Migration.Shared.Storage;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace Migration.Connectors.Targets.Bynder.Services
{
    public class BynderUpdateDataService
    {

        private readonly ILogger<BynderUpdateDataService> _logger;
        private readonly IOptions<BynderOptions> _bynderOptions;
        private readonly IBynderClient _bynderClient;
        private readonly IMemoryCache _memoryCache;
        private readonly IAzureBlobWrapperFactory _azureFactory;
        private readonly AssetResiliencyService _assetResiliencyService;
        private readonly IOptions<BynderHostOptions> _hostOptions;

        private const string Filename = "Filename";

        private DataTable _successTable = new DataTable("Success");
        private DataTable _retryTable = new DataTable("Retry");
        private List<string> _logOutput = new List<string>();

        private List<Dictionary<string, string>> _successes = new List<Dictionary<string, string>>();
        private List<Dictionary<string, string>> _failures = new List<Dictionary<string, string>>();
        private static string[] _ignoreColumns = new string[] { };
        private static string[] _ignoreValidationColumns = new string[] { };
        private Dictionary<string, BynderMetaProperty> _metaProperties = new Dictionary<string, BynderMetaProperty>();

        private const string partitionKey = "IdPartition";

        private static string _logFilename;
        private static string _successRetryFilename;
        private static string _blankMetadataTemplate;
        private static string _metadataFilename;
        private static string _metadataPropertiesFilename;
        private static string _clientMetadataTemplateFilename;
        private static string _metadataTemplateFilename;

        private AzureBlobWrapperAsync _metadataWrapper;

        private string _sourceDirectory;
        private string _tempDirectory;

        public BynderUpdateDataService(
            ILogger<BynderUpdateDataService> logger,
            IOptions<BynderOptions> bynderOptions,
            IOptions<BynderHostOptions> hostOptions,
            IBynderClient bynderClient,
            IMemoryCache memoryCache,
            IAzureBlobWrapperFactory azureFactory,
            AssetResiliencyService assetResiliencyService)
        {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bynderOptions = bynderOptions ?? throw new ArgumentNullException(nameof(bynderOptions));
            _bynderClient = bynderClient ?? throw new ArgumentNullException(nameof(bynderClient));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _azureFactory = azureFactory ?? throw new ArgumentNullException(nameof(azureFactory));
            _assetResiliencyService = assetResiliencyService ?? throw new ArgumentNullException(nameof(assetResiliencyService));
            _hostOptions = hostOptions ?? throw new ArgumentNullException(nameof(hostOptions));

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

            _metadataWrapper = _azureFactory.Get("metadata");
        }

        public async Task UploadAssetsFromMetadata(CancellationToken cancellationToken)
        {
            // TODO : handle menu for containers and filenames

            //await PopulateCollectionIds();

            //await GetAllAssets();

            _logger.LogInformation("Getting Meta Properties.");
            var metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            _metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);

            var logOutput = new List<string>();

            var fileData = await GetDictionaryData();
            var metadataFilenames = new List<string>();

            _logger.LogInformation($"Processing {fileData.Count()} rows from {_metadataFilename}.");

            await ProcessBatchesAsync(fileData, metadataFilenames, logOutput);

            SaveRowData();

            //await LogOrphanedAssets(metadataFilenames, logOutput);
            await LogToAzure(_metadataFilename, logOutput);

        }

        public async Task UpdateAssetsFromMetadata(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting Meta Properties.");
            var metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            _metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);

            //for (int i = 13; i < 29; i++)
            //{
            //MetadataFilename = $"update_bynder_azureassets_productAngleImport{i}_take5.xlsx";

            //if (!string.IsNullOrEmpty(MetadataFilename))
            //{


            var logOutput = new List<string>();

            try
            {
                //var fileData = await GetDictionaryData();
                //List<string> includeColumns = new List<string>() { "Campaign_Name", "Campaign_Season", "Campaign_Year"};
                //List<string> includeColumns = new List<string>() { "Gender" };
                List<string> includeColumns = new List<string>() { "Project_Access" };
                var fileData = await GetDictionaryData("4182_None_HEYDUDE_Restricted.xlsx", 0);  // Restamps.xlsx // 0 = Campaign, 1 = Gender, 2 = Region
                var metadataFilenames = new List<string>();

                _logger.LogInformation($"Processing {fileData.Count()} rows from {_metadataFilename}.");

                //await ProcessBatchesAsync(fileData, metadataFilenames, logOutput);
                await ProcessBatchesAsync(fileData, includeColumns, logOutput);

                SaveRowData();

                await LogToAzure(logOutput);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Could not process {_metadataFilename}. Ex: {ex.Message}");
            }

            //}
            //await Task.Delay(TimeSpan.FromMinutes(3));
            //}



        }

        public async Task UpdateAssetsFromMetadata(string blobName, Stream blobStream, TableClient tableClient, int take = 50)
        {

            _logger.LogInformation("Getting Meta Properties.");
            var metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            _metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);


            var logOutput = new List<string>();

            try
            {
                var fileData = GetDictionaryData(blobStream);
                var metadataFilenames = new List<string>();

                _logger.LogInformation($"Processing {fileData.Count()} rows from Metadata.");

                await ProcessBatchesAsync(fileData, metadataFilenames, logOutput, tableClient, take);

            }
            catch (Exception ex)
            {
                logOutput.Add($"Could not process metadata. Ex: {ex.Message}");
                _logger.LogError($"Could not process metadata. Ex: {ex.Message}");
            }

            SaveRowData();
            await LogToAzure(blobName, logOutput);

        }


 
        
        public async Task ProcessBatchesAsync(IEnumerable<Dictionary<string, string>> assets, List<string> metadataFilenames, List<string> logOutput, TableClient tableClient, int take = 50)
        {
            foreach (var batch in assets.Batch(take))
            {
                // Process each batch of 10 assets
                var tasks = batch.Select(rowData => ProcessAssetAsync(rowData, metadataFilenames, tableClient, logOutput));
                await Task.WhenAll(tasks.AsParallel());
            }
        }
        public async Task ProcessBatchesAsync(IEnumerable<Dictionary<string, string>> assets, List<string> metadataFilenames, List<string> logOutput)
        {
            int totalCount = 0;
            DateTime windowStart = DateTime.UtcNow;
            int batchCount = 20;
            foreach (var batch in assets.Batch(batchCount))
            {
                // If we're past the 5-minute window, reset
                if ((DateTime.UtcNow - windowStart) > TimeSpan.FromMinutes(5))
                {
                    totalCount = 0;
                    windowStart = DateTime.UtcNow;
                }
                _logger.LogWarning($"processed {totalCount}");
                totalCount += batch.Count;

                await Task.Delay(TimeSpan.FromMicroseconds(50));

                // Check limit
                if (totalCount > 3000)
                {

                    await Task.Delay(TimeSpan.FromMinutes(2));
                }

                // Process each batch of "take" assets
                var tasks = batch.Select(rowData => ProcessAssetAsync(rowData, metadataFilenames, logOutput));
                await Task.WhenAll(tasks.AsParallel());


            }
        }
        private async Task ProcessAssetAsync(Dictionary<string, string> rowData, List<string> metadataFilenames, List<string> logOutput)
        {
            //string nonExpiredDate = new DateTime(2189, 12, 31, 0, 0, 0, DateTimeKind.Utc).ToString("o");
            var assetId = rowData["Id"].Trim();

            //if (foundAssets.Any())
            //{
            //    string assetId = foundAssets[0].Id;
            try
            {
                var mediaInformationQuery = new MediaInformationQuery
                {
                    MediaId = assetId
                };
                var mediaInfo = await _bynderClient.GetAssetService().GetMediaInfoAsync(mediaInformationQuery);
                if (mediaInfo != null)
                {
                    var filename = mediaInfo.Name;
                    string fileExt = string.Empty;
                    metadataFilenames.Add(filename);
                    var logLine = string.Join(",", rowData.Values);
                    logOutput.Add($"Processing: {logLine}");
                    _logger.LogInformation($"Updating {filename}");


                    // get existing metaproperty options
                    Dictionary<string, IList<string>> metaPropertyOptions = new Dictionary<string, IList<string>>();

                    if (mediaInfo.PropertyOptionsDictionary != null)
                    {
                        foreach (var propertyKey in mediaInfo.PropertyOptionsDictionary.Keys)
                        {
                            //_logger.LogInformation($"Property option in dictionary: {propertyKey}: {mediaInfo.PropertyOptionsDictionary[propertyKey].ToString()}");
                            var extmetaProperty = _metaProperties.FirstOrDefault(x => x.Key == propertyKey.Replace("property_", "")).Value;
                            metaPropertyOptions.Add(extmetaProperty.Id, new List<string>() { string.Join(',', mediaInfo.PropertyOptionsDictionary[propertyKey].Select(a => a.ToString())) });
                            ;
                        }
                    }


                    logOutput.Add($"Validating Options...");
                    var validationErrors = ValidateOptions(rowData);
                    if (validationErrors.Any())
                    {
                        logOutput.AddRange(validationErrors);
                        LogRowData(false, rowData, $"Processing failed: {assetId} : {filename} has invalid options: {string.Join(',',validationErrors)}");
                        return;
                    }

                    logOutput.Add($"Getting Updated Options...");
                    var updatedMetaPropertyOptions = GetMetapropertyOptions(rowData);

                    logOutput.Add($"Adding Updated Options...");
                    foreach (var metaPropertyKey in updatedMetaPropertyOptions.Keys)
                    {
                        if (metaPropertyOptions.ContainsKey(metaPropertyKey))
                        {
                            metaPropertyOptions[metaPropertyKey] = updatedMetaPropertyOptions[metaPropertyKey]; // set to new value
                        }
                        else
                        {
                            metaPropertyOptions.Add(metaPropertyKey, updatedMetaPropertyOptions[metaPropertyKey]);
                        }
                    }
                    ;
                    string keywords = string.Empty;
                    //logOutput.Add($"Updating Keywords...");
                    if (rowData["Keywords"] != null)
                    {
                        keywords = rowData["Keywords"];
                    }


                    logOutput.Add($"Modifying Media...");
                    var modifyMediaQuery = new ModifyMediaQuery(assetId)
                    {
                        MetapropertyOptions = metaPropertyOptions,
                        Tags = string.IsNullOrEmpty(keywords) ? new List<string>() : keywords.Split(',').Select(s => s.Trim()).ToList()
                    };
                    ;
                    var status = await _bynderClient.GetAssetService().ModifyMediaAsync(modifyMediaQuery);
                    bool success = status.StatusCode >= 200 && status.StatusCode < 300 ? true : false;
                    LogRowData(success, rowData, $"{assetId} : {filename} updated success: {success}");
                    logOutput.Add($"{assetId} : {filename} updated success: {success}");


                }
                else
                {
                    logOutput.Add($"processing failed: {assetId} does not exist in Bynder assets");
                    LogRowData(false, rowData, $"Processing failed: {assetId} does not exist in Bynder assets");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception {assetId}: {ex.Message}");
                LogRowData(false, rowData, $"Processing failed: {assetId} : {ex.Message}");
            }
            //}

            return;
        }

        private async Task ProcessAssetAsync(Dictionary<string, string> rowData, List<string> metadataFilenames, TableClient tableClient, List<string> logOutput)
        {
            var originId = rowData["OriginId"];

            // Don't do this lookup as it will cost against the rate limit (Bynder Rate limit is 4500 requests per 5 minute period)
            // https://bynder.docs.apiary.io/#introduction/limit-on-requests

            //var foundAssets = await _bynderClient.GetAssetService().GetMediaListAsync(new MediaQuery()
            //{
            //    MetaProperties = new Dictionary<string, IList<string>>
            //        {
            //            {
            //                "Sitecore_ID", [sitecoreId]
            //            }
            //        }
            //});

            // we already know in an Update scenario we HAVE the assetId
            var assetId = rowData["Id"];

            // now lets see if we've already processed it for this batch
            var entity = await tableClient.GetEntityIfExistsAsync<TableEntity>(partitionKey, assetId);
            if (entity.HasValue)
            {
                _logger.LogInformation($"assetId {assetId} already processed. Skipping.");
                LogRowData(true, rowData, $"{assetId} : already processed. updated success: true");
                return;
            }

            //if (foundAssets.Any())
            //{
            //    string assetId = foundAssets[0].Id;
            try
            {
                var mediaInformationQuery = new MediaInformationQuery
                {
                    MediaId = assetId
                };
                var mediaInfo = await _bynderClient.GetAssetService().GetMediaInfoAsync(mediaInformationQuery);
                if (mediaInfo != null)
                {
                    var filename = mediaInfo.Name;
                    string fileExt = string.Empty;
                    metadataFilenames.Add(filename);
                    var logLine = string.Join(",", rowData.Values);
                    logOutput.Add($"Processing: {logLine}");
                    _logger.LogInformation($"Updating {filename}");


                    // get existing metaproperty options
                    Dictionary<string, IList<string>> metaPropertyOptions = new Dictionary<string, IList<string>>();

                    if (mediaInfo.PropertyOptionsDictionary != null)
                    {
                        foreach (var propertyKey in mediaInfo.PropertyOptionsDictionary.Keys)
                        {
                            //_logger.LogInformation($"Property option in dictionary: {propertyKey}: {mediaInfo.PropertyOptionsDictionary[propertyKey].ToString()}");
                            var extmetaProperty = _metaProperties.FirstOrDefault(x => x.Key == propertyKey.Replace("property_", "")).Value;
                            metaPropertyOptions.Add(extmetaProperty.Id, new List<string>() { string.Join(',', mediaInfo.PropertyOptionsDictionary[propertyKey].Select(a => a.ToString())) });
                            ;
                        }
                    }
                    logOutput.Add($"Validating Options...");
                    var validationErrors = ValidateOptions(rowData);
                    if (validationErrors.Any())
                    {
                        logOutput.AddRange(validationErrors);
                        LogRowData(false, rowData, $"Processing failed: {assetId} : {filename} has invalid options.");
                        return;
                    }

                    logOutput.Add($"Getting Updated Options...");
                    var updatedMetaPropertyOptions = GetMetapropertyOptions(rowData);

                    logOutput.Add($"Adding Updated Options...");
                    foreach (var metaPropertyKey in updatedMetaPropertyOptions.Keys)
                    {
                        if (metaPropertyOptions.ContainsKey(metaPropertyKey))
                        {
                            metaPropertyOptions[metaPropertyKey] = updatedMetaPropertyOptions[metaPropertyKey]; // set to new value
                        }
                        else
                        {
                            metaPropertyOptions.Add(metaPropertyKey, updatedMetaPropertyOptions[metaPropertyKey]);
                        }
                    }
                    ;
                    string keywords = string.Empty;
                    logOutput.Add($"Updating Keywords...");
                    if (rowData["Keywords"] != null)
                    {
                        keywords = rowData["Keywords"];
                    }
                    try
                    {
                        // this will add to rate limits...
                        if (mediaInfo.Tags != null)
                        {
                            foreach (var tag in mediaInfo.Tags)
                            {
                                var matchingTags = await _bynderClient.GetAssetService().GetTagsAsync(new GetTagsQuery() { Keyword = tag });
                                if (matchingTags.Any())
                                {
                                    var tagToRemove = matchingTags.FirstOrDefault(t => t.TagName.Equals(tag, StringComparison.InvariantCultureIgnoreCase));
                                    await _bynderClient.GetAssetService().RemoveTagFromMediaAsync(tagToRemove.ID, [assetId]);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logOutput.Add($"Keyword Exception: {ex.Message}");
                    }

                    logOutput.Add($"Modifying Media...");
                    var modifyMediaQuery = new ModifyMediaQuery(assetId)
                    {
                        MetapropertyOptions = metaPropertyOptions,
                        Description = rowData["Description"],
                        Name = string.IsNullOrEmpty(rowData["Title"]) ? rowData["Filename"] : rowData["Title"],
                        Tags = string.IsNullOrEmpty(keywords) ? new List<string>() : keywords.Split(',').ToList()
                    };
                    ;
                    var status = await _bynderClient.GetAssetService().ModifyMediaAsync(modifyMediaQuery);
                    bool success = status.StatusCode >= 200 && status.StatusCode < 300 ? true : false;
                    LogRowData(success, rowData, $"{assetId} : {filename} updated success: {success}");
                    logOutput.Add($"{assetId} : {filename} updated success: {success}");

                    if (success)
                    {
                        var newEntity = new TableEntity(partitionKey, assetId)
                        {
                            { "ProcessedOn", DateTime.UtcNow }
                        };
                        await tableClient.AddEntityAsync(newEntity);
                        _logger.LogInformation($"Saved assetId {assetId} as processed in table.");
                    }

                }
                else
                {
                    logOutput.Add($"processing failed: {assetId} does not exist in Bynder assets");
                    LogRowData(false, rowData, $"Processing failed: {assetId} does not exist in Bynder assets");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception {assetId}: {ex.Message}");
                LogRowData(false, rowData, $"Processing failed: {assetId} : {ex.Message}");
            }
            //}



            return;
        }

        private async Task<IEnumerable<Dictionary<string, string>>> GetDictionaryData()
        {

            // read file to stream
            _logger.LogInformation($"Reading {_metadataFilename} from Azure.");
            MemoryStream ms = new MemoryStream();
            var s = await _metadataWrapper.DownloadBlobAsync(_metadataFilename);
            s.CopyTo(ms);
            ms.Position = 0;

            var metadataTable = ExcelReader.LoadExcelWorksheetsToDataTables(ms).FirstOrDefault() ?? new DataTable();

            // configure output tables
            DataRow dr = metadataTable.Rows[0];
            foreach (var column in dr.Table.Columns)
            {
                var columnName = $"{column}";
                _successTable.Columns.Add(columnName);
                _retryTable.Columns.Add(columnName);
            }

            var fileData = new List<Dictionary<string, string>>();

            foreach (DataRow dataRow in metadataTable.Rows)
            {
                var rowData = new Dictionary<string, string>();

                foreach (var column in dataRow.Table.Columns)
                {
                    var columnName = $"{column}";
                    var value = dataRow[columnName].ToString() ?? string.Empty;
                    rowData.Add(columnName, value.Trim());
                }

                fileData.Add(rowData);
            }
            ms.Dispose();
            return fileData;
        }

        private IEnumerable<Dictionary<string, string>> GetDictionaryData(Stream s)
        {

            // read file to stream
            _logger.LogInformation($"Reading metadata file from Azure.");
            MemoryStream ms = new MemoryStream();
            s.CopyTo(ms);
            ms.Position = 0;

            var metadataTable = ExcelReader.LoadExcelWorksheetsToDataTables(ms).FirstOrDefault() ?? new DataTable();

            ms.Dispose();

            // configure output tables
            DataRow dr = metadataTable.Rows[0];
            foreach (var column in dr.Table.Columns)
            {
                var columnName = $"{column}";
                _successTable.Columns.Add(columnName);
                _retryTable.Columns.Add(columnName);
            }

            var fileData = new List<Dictionary<string, string>>();

            foreach (DataRow dataRow in metadataTable.Rows)
            {
                var rowData = new Dictionary<string, string>();

                foreach (var column in dataRow.Table.Columns)
                {
                    var columnName = $"{column}";
                    var value = dataRow[columnName].ToString() ?? string.Empty;
                    rowData.Add(columnName, value.Trim());
                }

                fileData.Add(rowData);
            }

            return fileData;
        }

        private async Task<IEnumerable<Dictionary<string, string>>> GetDictionaryData(string metadataFilename, int worksheet)
        {

            // read file to stream
            _logger.LogInformation($"Reading {metadataFilename} from Azure.");
            MemoryStream ms = new MemoryStream();
            var s = await _metadataWrapper.DownloadBlobAsync(metadataFilename);
            s.CopyTo(ms);
            ms.Position = 0;

            var metadataTable = ExcelReader.LoadExcelWorksheetsToDataTables(ms) ?? new List<DataTable>();

            // configure output tables
            DataRow dr = metadataTable[worksheet].Rows[0];

            foreach (var column in dr.Table.Columns)
            {
                var columnName = $"{column}";
                _successTable.Columns.Add(columnName);
                _retryTable.Columns.Add(columnName);
            }

            var fileData = new List<Dictionary<string, string>>();

            foreach (DataRow dataRow in metadataTable[worksheet].Rows)
            {
                var rowData = new Dictionary<string, string>();

                foreach (var column in dataRow.Table.Columns)
                {
                    var columnName = $"{column}";
                    var value = dataRow[columnName].ToString() ?? string.Empty;
                    rowData.Add(columnName, value.Trim());
                }

                fileData.Add(rowData);
            }
            ms.Dispose();
            return fileData;
        }

        private List<string> ValidateOptions(Dictionary<string, string> rowData)
        {
            List<string> errors = new List<string>();
            var metapropertyOptions = new Dictionary<string, IList<string>>();

            foreach (var data in rowData)
            {
                var columnName = data.Key;
                var value = data.Value;

                if (_ignoreColumns.Contains(columnName) || _ignoreValidationColumns.Contains(columnName)) continue;

                var metaProperty = _metaProperties.FirstOrDefault(x => x.Key == columnName).Value;

                if (metaProperty != null)
                {

                    if (metaProperty.IsRequired && string.IsNullOrEmpty(value))
                    {
                        errors.Add($"processing failed: Meta Property {metaProperty.Name} is a Required data field.");
                    }
                    else
                    {
                        // check options match possible option values
                        if (metaProperty.Options.Count > 0)
                        {
                            // we've already checked against required fields,
                            // but if a value is present it must be validated
                            if (!String.IsNullOrEmpty(value))
                            {
                                // value may be comma delimited multi select
                                List<string> values = value.Split(',').ToList();
                                List<string> validValues = metaProperty.Options.Select(x => x.Name).ToList();
                                // if all values are in the valid values, it should be good, otherwise log it.
                                if (!ContainsAll(validValues, values))
                                {
                                    errors.Add($"processing failed: {value} is NOT a valid value for {columnName}.");
                                    // log invalid data value
                                }
                            }
                        }
                        else
                        {
                            // TODO: add possible other data types that should be checked
                            if (metaProperty.Type == "date" && !string.IsNullOrEmpty(value))
                            {
                                // check that value is of type Date
                                // date should use ISO8601 format: yyyy-mm-ddThh:mm:ssZ or equivalent
                                if (!IsISO8601(value))
                                {
                                    errors.Add($"processing failed: {value} is NOT a valid ISO Date value for {columnName}.");
                                }
                            }
                        }
                    }
                }
                else
                {
                    //log missing / invalid meta property
                    errors.Add($"processing failed: No Meta Property found for column {columnName} ");
                }


            }

            return errors;
        }

        #region |-- Loading Methods --|

        private async Task LoadMetaProperties()
        {
            _logger.LogInformation("Getting Meta Properties.");
            var metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            _metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);
        }

        private Dictionary<string, IList<string>> GetMetapropertyOptions(Dictionary<string, string> rowData)
        {
            var metapropertyOptions = new Dictionary<string, IList<string>>();

            foreach (var data in rowData)
            {
                var columnName = data.Key;
                var value = data.Value;

                if (_ignoreColumns.Contains(columnName)) continue;

                var metaProperty = _metaProperties.FirstOrDefault(x => x.Key == columnName).Value;

                metapropertyOptions.Add(metaProperty.Id, new List<string>() { value });
            }

            return metapropertyOptions;
        }
        #endregion |-- Loading Methods --|

        #region|-- Helper Methods --|
        private static bool ContainsAll<T>(IEnumerable<T> source, IEnumerable<T> values)
        {
            return values.All(value => source.Contains(value));
        }
        public static bool IsISO8601(string dateString)
        {
            // Validate if a string is a valid ISO date format
            Regex validateDateRegex = new Regex("^(?:\\d{4})-(?:\\d{2})-(?:\\d{2})T(?:\\d{2}):(?:\\d{2}):(?:\\d{2}(?:\\.\\d*)?)(?:(?:-(?:\\d{2}):(?:\\d{2})|Z)?)$");
            return validateDateRegex.IsMatch(dateString);
        }
        private void SaveRowData()
        {
            foreach (var rowData in _successes)
            {
                DataRow row = _successTable.NewRow();
                foreach (KeyValuePair<string, string> item in rowData)
                {
                    row[item.Key] = item.Value;
                }
                row.Table.Rows.Add(row);
            }
            foreach (var rowData in _failures)
            {
                DataRow row = _retryTable.NewRow();
                foreach (KeyValuePair<string, string> item in rowData)
                {
                    row[item.Key] = item.Value;
                }
                row.Table.Rows.Add(row);
            }
        }
        private void LogRowData(bool isSuccess, Dictionary<string, string> rowData, string message)
        {
            if (rowData != null)
            {
                if (isSuccess)
                {
                    _successes.Add(rowData);
                }
                else
                {
                    rowData["Reason"] = message;
                    _failures.Add(rowData);
                }

            }
            if (!string.IsNullOrEmpty(message))
            {
                _logger.LogWarning($"{message}");
            }
        }
        private async Task<bool> LogToAzure(string blobName, List<string> logOutput)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmss");
            var prefix = blobName.Replace(".xlsx", "_");
            try
            {
                // Upload success and retry Excel file
                var dataTables = new List<DataTable> { _successTable, _retryTable };
                await using var excelStream = ExcelWriter.WriteDataTables(dataTables);
                excelStream.Position = 0;

                string excelFilename = $"{prefix}{timestamp}_{_successRetryFilename}";
                //await _logsWrapper.UploadBlobAsync(excelFilename, excelStream, "logfolder");

            }
            catch (Exception ex)
            {
                logOutput.Add($"Failed to create success/retry output to Azure Blob Storage. {ex.Message}");
                _logger.LogError(ex, "Failed to log output to Azure Blob Storage.");
            }

            try
            {
                // Upload plain text log
                string logFilename = $"{prefix}{timestamp}_{_logFilename}";
                var logContent = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, logOutput));
                await using (var logStream = new MemoryStream(logContent))
                {
                    //await _logsWrapper.UploadBlobAsync(logFilename, logStream, "logfolder");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log output to Azure Blob Storage.");
                return false;
            }
        }
        private async Task<bool> LogToAzure(List<string> logOutput)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmss");

            try
            {
                // Upload plain text log
                string logFilename = $"{timestamp}_{_logFilename}";
                var logContent = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, logOutput));
                await using (var logStream = new MemoryStream(logContent))
                {
                    //await _logsWrapper.UploadBlobAsync(logFilename, logStream);
                }

                // Upload success and retry Excel file
                var dataTables = new List<DataTable> { _successTable, _retryTable };
                await using var excelStream = ExcelWriter.WriteDataTables(dataTables);
                excelStream.Position = 0;

                string excelFilename = $"{timestamp}_{_successRetryFilename}";
                //await _logsWrapper.UploadBlobAsync(excelFilename, excelStream);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log output to Azure Blob Storage.");
                return false;
            }
        }
        #endregion


    }
}