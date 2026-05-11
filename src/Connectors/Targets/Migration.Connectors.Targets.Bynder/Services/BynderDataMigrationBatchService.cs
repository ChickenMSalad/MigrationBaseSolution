using Migration.Connectors.Targets.Bynder.Extensions;
using Migration.Connectors.Targets.Bynder.Configuration;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

using Migration.Shared.Storage;
using Migration.Connectors.Targets.Bynder.Clients;
using Migration.Shared.Files;
using Migration.Connectors.Targets.Bynder.Models;
using Bynder.Sdk.Query.Asset;
using Bynder.Sdk.Query.Collection;
using Bynder.Sdk.Query.Upload;
using Bynder.Sdk.Service;
using Bynder.Sdk.Service.Asset;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Shared.Configuration.Hosts.Bynder;


namespace Migration.Connectors.Targets.Bynder.Services
{
    public class BynderDataMigrationBatchService
    {
        private readonly ILogger<BynderDataMigrationBatchService> _logger;
        private readonly IOptions<BynderOptions> _bynderOptions;
        private readonly IOptions<BynderHostOptions> _hostOptions;
        private readonly IBynderClient _bynderClient;
        private readonly IMemoryCache _memoryCache;
        private readonly IAssetService _assetService;
        private readonly AssetResiliencyService _assetResiliencyService;
        private readonly ExecutionContextState _state;

        private DataTable _successTable = new DataTable("Success");
        private DataTable _retryTable = new DataTable("Retry");
        private List<string> _logOutput = new List<string>();
        private Dictionary<string, string> _allAssets = new Dictionary<string, string>();
        private Dictionary<string, string> _collectionIds = new Dictionary<string, string>();
        private List<Dictionary<string, string>> _successes = new List<Dictionary<string, string>>();
        private List<Dictionary<string, string>> _failures = new List<Dictionary<string, string>>();
        private static string[] _ignoreColumns = new string[] {  };
        private static string[] _ignoreValidationColumns = new string[] {  };
        private Dictionary<string, BynderMetaProperty> _metaProperties = new Dictionary<string, BynderMetaProperty>();

        private int _knownAssetPages = 0;
        private long _maxBytes = 800L * 1024 * 1024; // 800 MB

        private static string _logFilename;
        private static string _successRetryFilename;
        private static string _blankMetadataTemplate;
        private static string _metadataFilename;

        private string _sourceDirectory;
        private string _tempDirectory;

        private AzureBlobWrapperAsync _logsWrapper;
        private AzureBlobWrapperAsync _assetsWrapper;

        public BynderDataMigrationBatchService(
            ExecutionContextState state,
            ILogger<BynderDataMigrationBatchService> logger,
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
            _state = state;

            _tempDirectory = _hostOptions.Value.Paths.TempDirectory ?? Path.GetTempPath();
            _sourceDirectory = _hostOptions.Value.Paths.SourceDirectory ?? Path.GetTempPath();

            _ignoreColumns = _hostOptions.Value.Columns.IgnoreColumns.ToArray();
            _ignoreValidationColumns = _hostOptions.Value.Columns.IgnoreValidationColumns.ToArray();

            _blankMetadataTemplate = _hostOptions.Value.Files.BlankMetadataTemplate ?? "blank_metadata_template.xlsx";
            _metadataFilename = _hostOptions.Value.Files.MetadataFilename ?? "BynderWebDamImport_ntara.xlsx";
            _logFilename = _hostOptions.Value.Files.LogFilename ?? "bynder_migration_log.txt";
            _successRetryFilename = _hostOptions.Value.Files.SuccessRetryFilename ?? "successRetryMetadata.xlsx";

            _maxBytes = _hostOptions.Value.Batch.MaxBytes > 0 ? _hostOptions.Value.Batch.MaxBytes : _maxBytes;
            _knownAssetPages = _hostOptions.Value.Batch.KnownAssetPages > 0 ? _hostOptions.Value.Batch.KnownAssetPages : _knownAssetPages;

        }


        public async Task UploadAssetsFromMetadata(string blobName, Stream blobStream, int take = 50)
        {

            //await PopulateCollectionIds();
            //await GetAllAssets();

            await LoadMetaProperties();

            var logOutput = new List<string>();

            var fileData = GetDictionaryData(blobStream);
            var metadataFilenames = new List<string>();

            _logger.LogInformation($"Processing {fileData.Count()} rows");
            logOutput.Add($"Processing {fileData.Count()} rows");

            await ProcessBatchesAsync(fileData, metadataFilenames, logOutput, take);

            SaveRowData();

            //await LogOrphanedAssets(metadataFilenames, logOutput);
            await LogToAzure(blobName, logOutput);

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


        public async Task UploadAssetsFromLocal(CancellationToken cancellationToken)
        {
            ResetState();

            _logger.LogInformation("Getting Meta Properties.");
            var metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            _metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);

            var logOutput = new List<string>();

            var fileData = await GetDictionaryData();

            _logger.LogInformation($"Processing {fileData.Count()} rows from {_metadataFilename}.");


            foreach (var rowData in fileData)
            {
                //await ProcessAssetAsyncFromResourceProFile(rowData,logOutput);

                await ProcessSharePointAssetAsync(rowData,new List<string>(),logOutput);

                ;
            }

            //await ProcessBatchesAsync(fileData, metadataFilenames, logOutput);

            SaveRowData();

            await LogToLocal("resourcePro_bynder_upload.xlsx", logOutput);

        }

        public async Task ProcessBatchesAsync(IEnumerable<Dictionary<string, string>> assets, List<string> metadataFilenames, List<string> logOutput, int take = 50)
        {
            foreach (var batch in assets.Batch(take))
            {
                // Process each batch of 10 assets
                var tasks = batch.Select(rowData => ProcessAssetAsync(rowData, metadataFilenames, logOutput));
                await Task.WhenAll(tasks.AsParallel());
            }
        }

        private async Task ProcessAssetAsync(Dictionary<string, string> rowData, List<string> metadataFilenames, List<string> logOutput)
        {
            var filename = rowData["Filename"].Trim();
            var azureFilename = $"{rowData["AzureFilename"]}";

            var originId = rowData["OriginId"];

            metadataFilenames.Add(filename);
            var logLine = string.Join(",", rowData.Values);
            logOutput.Add($"Processing: {logLine}");
            _logger.LogInformation($"Processing {filename}");

            logOutput.Add($"Azure Filename : {azureFilename}");

            var foundAssets = await _bynderClient.GetAssetService().GetMediaListAsync(new MediaQuery()
            {
                MetaProperties = new Dictionary<string, IList<string>>
                    {
                        {
                            "OriginId", [originId]
                        }
                    }
            });

            if (foundAssets.Any())
            {
                rowData["Id"] = foundAssets[0].Id;
                LogRowData(true, rowData, $"{filename} creation success: true (already created)");

                // retry add to collections
                //await AddToCollection(rowData["Collection"], rowData["Id"]);
            }
            else
            {
                try
                {
                    var bExists = await _assetsWrapper.BlobExistsAsync(azureFilename, $"{rowData["AssetFolder"]}/");
                    logOutput.Add($"Azure Filename Exists at : {rowData["AssetFolder"]}/{azureFilename} : {bExists}");
                    if (!bExists)
                    {
                        logOutput.Add($"processing failed: {filename} does not exist in Azure assets");
                        LogRowData(false, rowData, $"Processing failed: {filename} does not exist in Azure assets");
                        return;
                    }
                }
                catch (Exception e)
                {
                    logOutput.Add($"processing failed: Exception {e.Message}");
                    LogRowData(false, rowData, $"Processing failed: Exception {e.Message}");
                    return;
                }


                var validationErrors = ValidateOptions(rowData);
                if (validationErrors.Any())
                {
                    logOutput.AddRange(validationErrors);
                    LogRowData(false, rowData, $"Processing failed: {filename} has invalid options.");
                    return;
                }

                try
                {
                    QueryStream queryStream = await GetQueryStream(rowData);
                    var response = await _assetResiliencyService.UploadFileAsync(queryStream.Stream, queryStream.Query);
                    rowData["Id"] = response.MediaId;
                    LogRowData(response.IsSuccessful, rowData, $"{filename} creation success: {response.IsSuccessful}");
                    //if (response.IsSuccessful)
                    //{
                    //    await AddToCollection(rowData["Collection"], rowData["Id"]);
                    //}

                }
                catch (Exception ex)
                {
                    LogRowData(false, rowData, $"Processing failed: {filename} create/update failure: {ex.Message}");
                    logOutput.Add($"Oh GEEZ:  {ex.Message}");
                }
            }

            return;
        }


        private async Task ProcessAssetAsyncFromResourceProFile(Dictionary<string, string> rowData, List<string> logOutput)
        {
            string windowsFile = string.Empty;
            var filename = rowData["Filename"].Trim();
            var originId = rowData["OriginId"];
            var path = rowData["Path"].Trim();

            windowsFile = $"{_tempDirectory}{path.Replace('/', Path.DirectorySeparatorChar)}/{filename}";

            var logLine = string.Join(",", rowData.Values);
            logOutput.Add($"Processing: {logLine}");
            _logger.LogInformation($"Processing {windowsFile}");

            var foundAssets = await _bynderClient.GetAssetService().GetMediaListAsync(new MediaQuery()
            {
                MetaProperties = new Dictionary<string, IList<string>>
                    {
                        {
                            "OriginId", [originId]
                        }
                    }
            });

            if (foundAssets.Any())
            {
                rowData["Id"] = foundAssets[0].Id;
                LogRowData(true, rowData, $"{filename} creation success: true (already created)");

                // retry add to collections
                //await AddToCollection(rowData["Collection"], rowData["Id"]);
            }
            else
            {

                try
                {
                    if (!string.IsNullOrEmpty(windowsFile))
                    {
                        var bExists = File.Exists(windowsFile);
                        logOutput.Add($"Filename Exists at : {windowsFile} : {bExists}");
                        _logger.LogInformation($"Filename Exists at : {windowsFile} : {bExists}");
                        if (!bExists)
                        {
                            logOutput.Add($"processing failed: {filename} does not exist in windows assets");
                            LogRowData(false, rowData, $"Processing failed: {filename} does not exist in windows assets");
                            return;
                        }

                    }
                    else
                    {

                        _logger.LogInformation($"WHOA! Cannot find file for : {filename}.  going to try to match without extension...");
                        logOutput.Add($"processing failed: {filename} does not exist in windows assets");
                        LogRowData(false, rowData, $"Processing failed: {filename} does not exist in windows assets");
                        return;

                    }

                }
                catch (Exception e)
                {
                    logOutput.Add($"processing failed: Exception {e.Message}");
                    LogRowData(false, rowData, $"Processing failed: Exception {e.Message}");
                    return;
                }


                var validationErrors = ValidateOptions(rowData);
                if (validationErrors.Any())
                {
                    logOutput.AddRange(validationErrors);
                    LogRowData(false, rowData, $"Processing failed: {filename} has invalid options.");
                    return;
                }

                try
                {
                    QueryStream queryStream = await GetQueryStreamFromFile(rowData, windowsFile);
                    var response = await _assetResiliencyService.UploadFileAsync(queryStream.Stream, queryStream.Query);
                    rowData["Id"] = response.MediaId;
                    LogRowData(response.IsSuccessful, rowData, $"{filename} creation success: {response.IsSuccessful}");

                    ;
                    // for test run only
                    //LogRowData(true, rowData, $"{filename} creation success: true");

                    //if (response.IsSuccessful)
                    //{
                    //    await AddToCollection(rowData["Collection"], rowData["Id"]);
                    //}

                }
                catch (Exception ex)
                {
                    LogRowData(false, rowData, $"Processing failed: {filename} create/update failure: {ex.Message}");
                    logOutput.Add($"Oh GEEZ:  {ex.Message}");
                }

                //if (File.Exists(SourceDirectory+azureFilename))
                //{
                //    File.Delete(SourceDirectory + azureFilename);
                //    _logger.LogInformation($"{SourceDirectory}{azureFilename} deleted");
                //}
            }

            return;
        }

        private async Task ProcessSharePointAssetAsync(Dictionary<string, string> rowData, List<string> includeColumns, List<string> logOutput)
        {
            string bynderId = rowData["Id"];
            string sharePointTypeRowData = rowData["Sharepoint_Folder_Path"];
            try
            {
                if (!string.IsNullOrEmpty(bynderId))
                {
                    var assetId = bynderId;

                    var mediaInformationQuery = new MediaInformationQuery
                    {
                        MediaId = assetId
                    };
                    var mediaInfo = await _bynderClient.GetAssetService().GetMediaInfoAsync(mediaInformationQuery);
                    if (mediaInfo != null)
                    {
                        bool shouldUpdate = false;

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

                        // get asset type
                        var metaPropertyAssetType = _metaProperties.FirstOrDefault(x => x.Key == "Sharepoint_Folder_Path").Value;
                        string sharePointType = string.Empty;
                        if (metaPropertyOptions.ContainsKey(metaPropertyAssetType.Id))
                        {
                            sharePointType = metaPropertyOptions[metaPropertyAssetType.Id].FirstOrDefault() ?? string.Empty;
                        }

                        //if (!string.IsNullOrEmpty(sharePointType)) // this should never be empty as it is required
                        //{
                            if (sharePointType != sharePointTypeRowData)
                            {
                                shouldUpdate = true;
                                metaPropertyOptions[metaPropertyAssetType.Id] = new List<string>() { sharePointTypeRowData };
                                _logger.LogInformation($"{assetId} assetType needed updating");
                                _logger.LogInformation($"{assetId} changing Sharepoint_Folder_Path from {sharePointType} to {sharePointTypeRowData}");
                            }


                        //}

                        if (shouldUpdate)
                        {

                            _logger.LogInformation($"{bynderId} updating asset. Sharepoint_Folder_Path property value is: {string.Join(',', metaPropertyOptions[metaPropertyAssetType.Id])}");

                            //_logger.LogInformation($"Modifying Media...");
                            var modifyMediaQuery = new ModifyMediaQuery(assetId)
                            {
                                Name = rowData["Filename"],
                                MetapropertyOptions = metaPropertyOptions,
                            };
                            ;
                            var status = await _bynderClient.GetAssetService().ModifyMediaAsync(modifyMediaQuery);
                            bool success = status.StatusCode >= 200 && status.StatusCode < 300 ? true : false;
                            _logger.LogInformation($"{bynderId} updated success: {success}");
                            logOutput.Add(bynderId + $",updated,{success}");
                            LogRowData(success, rowData, $"{assetId} : {bynderId} updated success: {success}");
                        }
                        else
                        {
                            _logger.LogInformation($"{bynderId} already correct. skip updated");
                            logOutput.Add(bynderId + ",already_correct,true");
                            LogRowData(true, rowData, $"{assetId} : already correct. skip updated");
                        }

                    }
                    else
                    {
                        _logger.LogInformation($"{assetId} no longer found in Bynder");
                        logOutput.Add(assetId + ",not found,false");
                        LogRowData(false, rowData, $"{assetId} : no longer found in Bynder");
                    }
                }
                else
                {
                    _logger.LogInformation($"{bynderId} Not Found in Bynder");
                    logOutput.Add(bynderId + ",not in bynder,false");
                    LogRowData(false, rowData, $"{bynderId} : Not Found in Bynder");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception {bynderId}: {ex.Message}");
                logOutput.Add($"Exception {bynderId}: {ex.Message}");
                LogRowData(false, rowData, $"Processing failed: {bynderId} : {ex.Message}");
            }
            //}

            return;
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
        private async Task<IEnumerable<Dictionary<string, string>>> GetDictionaryData()
        {

            //var metadataTable = CsvUtils.GetDictionaryDataFromCSV(SourceDirectory + MetadataFilename);

            var metadataTable = ExcelReader.LoadExcelWorksheetsToDataTables(new FileInfo(_sourceDirectory + _metadataFilename)).FirstOrDefault() ?? new DataTable();


            //const string DateFormat = "MM/dd/yyyy";

            //if (!metadataTable.Columns.Contains("DateAdded_DT"))
            //{
            //    metadataTable.Columns.Add("DateAdded_DT", typeof(DateTime));

            //    foreach (DataRow row in metadataTable.Rows)
            //    {
            //        var raw = row["Date_Added_to_Widen"]?.ToString();

            //        //if (!string.IsNullOrWhiteSpace(raw) &&
            //        //    DateTime.TryParseExact(
            //        //        raw,
            //        //        DateFormat,
            //        //        CultureInfo.InvariantCulture,
            //        //        DateTimeStyles.None,
            //        //        out var dt))
            //        //{
            //        //    row["DateAdded_DT"] = dt;
            //        //}
            //        //else
            //        //{
            //        //    row["DateAdded_DT"] = DBNull.Value;
            //        //}

            //        if (DateTime.TryParse(row["Date_Added_to_Widen"]?.ToString(), out var dt))
            //            row["DateAdded_DT"] = dt;
            //        else
            //            row["DateAdded_DT"] = DBNull.Value;
            //    }
            //}

            //var view = new DataView(metadataTable)
            //{
            //    Sort = "DateAdded_DT ASC"
            //};

            //var sortedTable = view.ToTable();

            //await using var excelStream = ExcelWriter.WriteDataTable(sortedTable);
            //excelStream.Position = 0;

            //var fileName = $"{SourceDirectory}sorted{MetadataFilename}";

            //using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            //excelStream.WriteTo(fs);

            ;
            // configure output tables
            if (_state.SuccessTable.Columns.Count == 0)
            {
                DataRow dr = metadataTable.Rows[0];
                foreach (var column in dr.Table.Columns)
                {
                    var columnName = $"{column}";
                    _state.SuccessTable.Columns.Add(columnName);
                    _state.RetryTable.Columns.Add(columnName);
                }
                _state.RetryTable.Columns.Add("Reason");
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
        private async Task<IEnumerable<Dictionary<string, string>>> GetValidFileData(IEnumerable<Dictionary<string, string>> assets)
        {
            var validRows = new List<Dictionary<string, string>>();
            foreach (Dictionary<string, string> rowData in assets)
            {

                var filename = rowData["Filename"].Trim();
                var logLine = string.Join(",", rowData.Values);
                _logOutput.Add($"Processing: {logLine}");
                _logger.LogInformation($"Processing {filename}");

                if (_allAssets.Keys.Contains(filename))
                {
                    rowData["Id"] = _allAssets[filename];
                    LogRowData(true, rowData, $"{filename} creation success: true (already created)");
                }
                else
                {

                    try
                    {
                        var bExists = await _assetsWrapper.BlobExistsAsync(filename);
                        if (!bExists)
                        {
                            _logOutput.Add($"processing failed: {filename} does not exist in Azure assets");
                            LogRowData(false, rowData, $"Processing failed: {filename} does not exist in Azure assets");
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        _logOutput.Add($"processing failed: Exception {e.Message}");
                        LogRowData(false, rowData, $"Processing failed: Exception {e.Message}");
                        continue;
                    }


                    var validationErrors = ValidateOptions(rowData);
                    if (validationErrors.Any())
                    {
                        _logOutput.AddRange(validationErrors);
                        LogRowData(false, rowData, $"Processing failed: {filename} has invalid options.");
                    }
                    else
                    {
                        validRows.Add(rowData);
                    }

                }
            }

            return validRows;
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

        private async Task LoadMetaProperties()
        {
            _logger.LogInformation("Getting Meta Properties.");
            var metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            _metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);
        }

        private async Task AddToCollection(string collectionName, string mediaId)
        {

            if (_collectionIds.Keys.Contains(collectionName))
            {
                var addMediaQuery = new AddMediaQuery(_collectionIds[collectionName], new List<string> { mediaId });
                try
                {
                    var status = await _bynderClient.GetCollectionService().AddMediaAsync(addMediaQuery);
                    _logger.LogInformation($"media added to collection {collectionName} : {status.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Oh No! ex: {ex.Message}");
                }


            }
            else
            {
                var statusCreate = await _bynderClient.GetCollectionService().CreateCollectionAsync(new CreateCollectionQuery(collectionName));
                if (statusCreate.StatusCode == 200 || statusCreate.StatusCode == 202)
                {
                    await PopulateCollectionIds();
                    var addMediaQuery = new AddMediaQuery(_collectionIds[collectionName], new List<string> { mediaId });
                    var status = await _bynderClient.GetCollectionService().AddMediaAsync(addMediaQuery);
                    _logger.LogInformation($"media added to collection {collectionName} : {status.Message}");
                }
            }
    ;
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

        public void ResetState()
        {
            _state.SuccessTable.Clear();
            _state.SuccessTable.Reset();
            _state.RetryTable.Clear();
            _state.RetryTable.Reset();
            _state.Successes.Clear();
            _state.Failures.Clear();
        }
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
            foreach (var rowData in _state.Successes)
            {
                DataRow row = _state.SuccessTable.NewRow();
                foreach (KeyValuePair<string, string> item in rowData)
                {
                    row[item.Key] = item.Value;
                }
                row.Table.Rows.Add(row);
            }
            foreach (var rowData in _state.Failures)
            {
                DataRow row = _state.RetryTable.NewRow();
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
                    _state.Successes.Add(rowData);
                }
                else
                {
                    rowData["Reason"] = message;
                    _state.Failures.Add(rowData);
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
                await _logsWrapper.UploadBlobAsync(excelFilename, excelStream, "logfolder");

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
                    await _logsWrapper.UploadBlobAsync(logFilename, logStream, "logfolder");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log output to Azure Blob Storage.");
                return false;
            }
        }

        private async Task<bool> LogToLocal(string blobName, List<string> logOutput)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmss");
            var prefix = blobName.Replace(".xlsx", "_");
            try
            {
                // Upload success and retry Excel file
                var dataTables = new List<DataTable> { _state.SuccessTable, _state.RetryTable };
                await using var excelStream = ExcelWriter.WriteDataTables(dataTables);
                excelStream.Position = 0;

                string excelFilename = $"{_sourceDirectory}{prefix}{timestamp}_{_successRetryFilename}";
                //await _logsWrapper.UploadBlobAsync(excelFilename, excelStream, "logfolder");
                using (var fs = new FileStream(excelFilename, FileMode.Create, FileAccess.Write))
                {
                    excelStream.CopyTo(fs);
                }

            }
            catch (Exception ex)
            {
                logOutput.Add($"Failed to create success/retry output to Azure Blob Storage. {ex.Message}");
                _logger.LogError(ex, "Failed to log output to Azure Blob Storage.");
            }

            try
            {
                // Upload plain text log
                string logFilename = $"{_sourceDirectory}{prefix}{timestamp}_{_logFilename}";
                var logContent = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, logOutput));
                await using (var logStream = new MemoryStream(logContent))
                {
                    //await _logsWrapper.UploadBlobAsync(logFilename, logStream, "logfolder");
                    using (var fs = new FileStream(logFilename, FileMode.Create, FileAccess.Write))
                    {
                        logStream.CopyTo(fs);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log output to Azure Blob Storage.");
                return false;
            }
        }

        private async Task LogOrphanedAssets(List<string> metadataFilenames, List<string> logOutput)
        {
            var allBlobs = await _assetsWrapper.GetBlobListingAsync();
            var allAssets = allBlobs.ToList();
            var orphanedAssets = allAssets.Except(metadataFilenames).ToList();

            if (orphanedAssets.Any())
            {
                logOutput.Add("The following assets were found in Azure but were not listed in the metadata file:");
                logOutput.AddRange(orphanedAssets);
                _logger.LogInformation($"Unlisted assets detected in Azure storage. See log.");
            }
        }

        private async Task<QueryStream> GetQueryStream(Dictionary<string, string> rowData)
        {
            QueryStream queryStream = new QueryStream();
            var filename = rowData["Filename"];
            var azureFilename = $"{rowData["AzureFilename"]}";
            var folderName = $"{rowData["AssetFolder"]}/";
            var stream = await _assetsWrapper.DownloadBlobAsync(azureFilename, folderName);
            var metapropertyOptions = GetMetapropertyOptions(rowData);
            var description = rowData["Description"];
            var mediaId = rowData["Id"];

            UploadQuery query = new()
            {
                Filepath = filename,
                Description = description,
                BrandId = _bynderOptions.Value.BrandStoreId,
                Name = filename,
                OriginalFileName = filename,
                MetapropertyOptions = metapropertyOptions
            };

            if (!string.IsNullOrWhiteSpace(mediaId))
            {
                var mediaInfo = _assetService.GetMediaInfoAsync(new MediaInformationQuery { MediaId = mediaId }).ConfigureAwait(false).GetAwaiter().GetResult();
                var modifyQuery = new ModifyMediaQuery(mediaId)
                {
                    Name = filename,
                    Description = description,
                    MetapropertyOptions = metapropertyOptions
                };

                await _assetService.ModifyMediaAsync(modifyQuery);

                query = new UploadQuery
                {
                    MediaId = mediaId,
                    Filepath = filename,
                    BrandId = _bynderOptions.Value.BrandStoreId,
                    Name = filename,
                    OriginalFileName = mediaInfo.Name
                };
            }
            queryStream.Query = query;
            queryStream.Stream = stream;
            queryStream.RowData = rowData;

            return queryStream;
        }
        private async Task<QueryStream> GetQueryStreamFromFile(Dictionary<string, string> rowData)
        {
            QueryStream queryStream = new QueryStream();
            var filename = rowData["Filename"];
            var azureFilename = $"{rowData["AzureFilename"]}";
            var folderName = $"{rowData["AssetFolder"]}/";

            var stream = new FileStream($"{_sourceDirectory}{azureFilename}", FileMode.Open, FileAccess.Read);//await _assetsWrapper.DownloadBlobAsync(azureFilename, folderName);
            var metapropertyOptions = GetMetapropertyOptions(rowData);
            var description = rowData["Description"];
            var mediaId = rowData["Id"];

            UploadQuery query = new()
            {
                Filepath = filename,
                Description = description,
                BrandId = _bynderOptions.Value.BrandStoreId,
                Name = filename,
                OriginalFileName = filename,
                MetapropertyOptions = metapropertyOptions
            };

            if (!string.IsNullOrWhiteSpace(mediaId))
            {
                var mediaInfo = _assetService.GetMediaInfoAsync(new MediaInformationQuery { MediaId = mediaId }).ConfigureAwait(false).GetAwaiter().GetResult();
                var modifyQuery = new ModifyMediaQuery(mediaId)
                {
                    Name = filename,
                    Description = description,
                    MetapropertyOptions = metapropertyOptions
                };

                await _assetService.ModifyMediaAsync(modifyQuery);

                query = new UploadQuery
                {
                    MediaId = mediaId,
                    Filepath = filename,
                    BrandId = _bynderOptions.Value.BrandStoreId,
                    Name = filename,
                    OriginalFileName = mediaInfo.Name
                };
            }
            queryStream.Query = query;
            queryStream.Stream = stream;
            queryStream.RowData = rowData;

            return queryStream;
        }

        private async Task<QueryStream> GetQueryStreamFromFile(Dictionary<string, string> rowData, string windowsFile)
        {
            QueryStream queryStream = new QueryStream();
            var filename = rowData["Filename"];

            var stream = new FileStream(windowsFile, FileMode.Open, FileAccess.Read);//await _assetsWrapper.DownloadBlobAsync(azureFilename, folderName);
            var metapropertyOptions = GetMetapropertyOptions(rowData);
            var description = filename;
            var mediaId = rowData["Id"];

            UploadQuery query = new()
            {
                Filepath = filename,
                Description = description,
                BrandId = _bynderOptions.Value.BrandStoreId,
                Name = filename,
                OriginalFileName = filename,
                MetapropertyOptions = metapropertyOptions
            };

            if (!string.IsNullOrWhiteSpace(mediaId))
            {
                var mediaInfo = _assetService.GetMediaInfoAsync(new MediaInformationQuery { MediaId = mediaId }).ConfigureAwait(false).GetAwaiter().GetResult();
                var modifyQuery = new ModifyMediaQuery(mediaId)
                {
                    Name = filename,
                    Description = description,
                    MetapropertyOptions = metapropertyOptions
                };

                await _assetService.ModifyMediaAsync(modifyQuery);

                query = new UploadQuery
                {
                    MediaId = mediaId,
                    Filepath = filename,
                    BrandId = _bynderOptions.Value.BrandStoreId,
                    Name = filename,
                    OriginalFileName = mediaInfo.Name
                };
            }
            queryStream.Query = query;
            queryStream.Stream = stream;
            queryStream.RowData = rowData;

            return queryStream;
        }
        public async Task SaveStreamToFileAsync(Stream inputStream, string filePath)
        {
            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            using (FileStream outputFileStream = File.Create(filePath)) // File.Create is a shortcut for new FileStream(..., FileMode.Create, FileAccess.Write)
            {
                await inputStream.CopyToAsync(outputFileStream);
            }
        }

        #endregion


    }
}