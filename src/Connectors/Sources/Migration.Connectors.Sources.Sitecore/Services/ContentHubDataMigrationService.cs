using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Azure.Storage.Blobs;

using Migration.Shared.Storage;
using Migration.Connectors.Targets.Bynder.Extensions;
using Migration.Shared.Files;
using Migration.Connectors.Sources.Sitecore.Models;

using CsvHelper;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Stylelabs.M.Base.Querying;
using Stylelabs.M.Base.Querying.Linq;
using Stylelabs.M.Sdk;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;

namespace Migration.Connectors.Sources.Sitecore.Services
{
    public class ContentHubDataMigrationService
    {
        private readonly IWebMClient _client;
        private readonly ILogger<ContentHubDataMigrationService> _logger;
        private readonly IAzureBlobWrapperFactory _azureFactory;

        private AzureBlobWrapperAsync _logsWrapper;
        private AzureBlobWrapperAsync _assetsWrapper;
        private AzureBlobWrapperAsync _metadataWrapper;

        private AzureBlobWrapperAsync _assetsBaseWrapper;
        private AzureBlobWrapperAsync _assetsLastModifiedWrapper;
        private AzureBlobWrapperAsync _metadatasBaseWrapper;
        private AzureBlobWrapperAsync _metadataLastModifiedWrapper;
        private readonly BlobServiceClient _blobServiceClient;


        public ContentHubDataMigrationService(BlobServiceClient blobServiceClient, IWebMClient client, IAzureBlobWrapperFactory azureFactory, ILogger<ContentHubDataMigrationService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _azureFactory = azureFactory ?? throw new ArgumentNullException(nameof(azureFactory));

            // get azure wrappers
            _logsWrapper = _azureFactory.Get("logs");
            _metadataWrapper = _azureFactory.Get("metadata");
            _assetsBaseWrapper = _azureFactory.Get("assets");

            _assetsLastModifiedWrapper = _azureFactory.Get("assets-lastmodified");
            _metadataLastModifiedWrapper = _azureFactory.Get("metadata-lastmodified");

            _assetsWrapper = _assetsBaseWrapper;
            _metadatasBaseWrapper = _metadataWrapper;

            _blobServiceClient = blobServiceClient;
        }


        public async Task ProcessBatchesIntoAzureAsync(Stream blobStream, int take = 50)
        {
            List<string> blobs = GetAllUnprocessedBatchesForTaxonomy(blobStream);
            _logger.LogInformation($"Found {blobs.Count} blobs in blobStream");
            if (blobs.Count > 0)
            { 
                int blobCounter = 0;
                foreach (var blob in blobs)
                {
                    blobCounter++;
                    string fileName = Path.GetFileName(blob);
                    string folderName = string.Empty;
                    string taxName = string.Empty;

                    _assetsWrapper = _assetsBaseWrapper;
                    if(fileName.Contains("_lastmodified_"))
                    {
                        _assetsWrapper = _assetsLastModifiedWrapper;
                        _metadataWrapper = _metadataLastModifiedWrapper;

                    }

                    if (fileName.Contains(".invalids."))
                    {
                        taxName = fileName.Split('.')[0] + "/";
                        folderName = "invalids/";

                    } else
                    {
                        folderName = blob.Substring(0, blob.LastIndexOf('/')) + "/";
                        taxName = blob.Substring(0, blob.IndexOf('/')) + "/";
                    }

                    _logger.LogWarning($"{blobCounter} : Processing Blob: {blob}");
                    await CreateAzureAssetsFromBatchAsync(fileName, folderName, taxName, true, take);

                    _logger.LogInformation("Process Complete.");
                }
            }
            else
            {
                _logger.LogInformation("Process Aborted. No blobs found.");
            }
        }

        private async Task CreateAzureAssetsFromBatchAsync(string batchName, string folderName, string assetFolder, bool skipExisting = false, int take = 50)
        {
            List<string> errorLog = new List<string>();

            string logFilename = $"{folderName.Replace("/", "-")}{batchName.Replace(".json", "")}.log";

            bool logExists = await _logsWrapper.BlobExistsAsync(logFilename, "import_errors");

            if (!logExists)
            {
                List<CustomAsset> assets = new List<CustomAsset>();

                try
                {
                    bool isModifiedData = batchName.Contains("_lastmodified_");
                    using (MemoryStream ms = new MemoryStream())
                    {
                        var s = await _metadataWrapper.DownloadBlobAsync(batchName, folderName);
                        s.CopyTo(ms);
                        string jsonString = System.Text.Encoding.ASCII.GetString(ms.ToArray());
                        assets = JsonConvert.DeserializeObject<List<CustomAsset>>(jsonString);
                    }

                    foreach (var batch in assets.Batch(take))
                    {
                        var assetsToUpload = new List<(string, Func<Task<MemoryStream>>, IDictionary<string, string>?)>();
                        foreach (var asset in batch)
                        {

                            try
                            {
                                string cleanedFilename = Regex.Replace(asset.Properties.FileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

                                bool existsInBase = false;
                                if (isModifiedData)
                                {
                                    bool baseFileExists = skipExisting ? await _assetsBaseWrapper.BlobExistsAsync($"{asset.Id}_{cleanedFilename}", assetFolder) : false;
                                    string[] basePrefixAssets = await _assetsBaseWrapper.SearchBlobListingByPrefixAsync($"{assetFolder}{asset.Id}");
                                    bool basePrefixExists = skipExisting ? basePrefixAssets.Any() : false;
                                    existsInBase = baseFileExists || basePrefixExists;

                                    // TODO: potentially check if filesize is different. not sure if the original download would be modified
                                }

                                bool fileExists = skipExisting ? await _assetsWrapper.BlobExistsAsync($"{asset.Id}_{cleanedFilename}", assetFolder) : false;
                                string[] prefixAssets = await _assetsWrapper.SearchBlobListingByPrefixAsync($"{assetFolder}{asset.Id}");
                                bool prefixExists = skipExisting ? prefixAssets.Any() : false;

                                if (!fileExists && !prefixExists && !existsInBase)
                                {
                                    var entity = await _client.Entities.GetAsync(asset.Id);

                                    var original = entity.GetRendition("downloadOriginal");
                                    var rendition = original.Items.FirstOrDefault();
                                    if (rendition != null)
                                    {
                                        try
                                        {
                                            //var filename = entity.GetPropertyValue<string>(Constants.Asset.FileName);
                                            //var s = async () => await rendition.GetStreamAsync();
                                            var s = async () =>
                                            {
                                                using var source = await rendition.GetStreamAsync();

                                                var mem = new MemoryStream();
                                                await source.CopyToAsync(mem);
                                                mem.Position = 0;
                                                return mem;
                                            };
                                            string uploadFilename = $"{entity.Id}_{cleanedFilename}";
                                            assetsToUpload.Add(new(uploadFilename, s, null));
                                        }
                                        catch (Exception ex)
                                        {
                                            errorLog.Add(asset.Id.ToString());
                                        }
                                    }
                                } else
                                {
                                    _logger.LogInformation($"{asset.Id}_{cleanedFilename} already exists in base assets.");
                                }
                            }
                            catch (Exception ex)
                            {
                                errorLog.Add($"{asset.Id}_{asset.Properties.FileName} failed with exception: {ex.Message}");
                            }

                        }

                        //var progress = new Progress<(string BlobName, long BytesUploaded)>(tuple =>
                        //{
                        //    _logger.LogInformation($"Uploading {tuple.BlobName}: {tuple.BytesUploaded} bytes uploaded");
                        //});

                        try
                        {
                            var results = await _assetsWrapper.UploadBlobsAsync(assetsToUpload, folderPath: assetFolder, overwrite: false); //, progress: progress
                            foreach (var result in results)
                            {
                                if (!result.Success)
                                {
                                    errorLog.Add($"{result.FileName} failed with exception: {result.Exception}");
                                }
                            }
                        }
                        catch (AggregateException ae)
                        {
                            foreach (var ex in ae.InnerExceptions)
                            {
                                _logger.LogInformation($"Upload error: {ex.Message}");
                                errorLog.Add(ex.Message);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"Unexpected error: {ex.Message}");
                            errorLog.Add(ex.Message);
                        }
                    }

                    if (errorLog.Count > 0)
                    {
                        await LogBatchErrorsAsync(errorLog, logFilename);
                    }
                    else
                    {
                        if (!batchName.Contains(".processed"))
                        {
                            await _metadataWrapper.MoveBlobAsync($"{folderName}{batchName}", $"{folderName}{batchName}.processed");
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"{folderName}{batchName} not valid or contains already processed assets");
                }
            } else
            {
                _logger.LogWarning($"{logFilename} already exists for this batch.  Skipping reprocessing. Delete log to reprocess.");
            }






        }

        private List<string> GetAllUnprocessedBatchesForTaxonomy(Stream blobStream)
        {
            List<string> allFolderBlobs = new List<string>();
            if (blobStream != null)
            {

                    try
                    {
                        var metadataTable = ExcelReader.LoadExcelWorksheetsToDataTables(blobStream).FirstOrDefault() ?? new DataTable();
                        
                        foreach (DataRow dataRow in metadataTable.Rows)
                        {

                            var value = dataRow["Blobname"].ToString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(value))
                            {
                                allFolderBlobs.Add(value.Trim());
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"Error with file: {ex.Message}");
                    }
            } else
            {
                _logger.LogInformation($"Error: blobStream was null!");
            }

            return allFolderBlobs;

        }

        private async Task<bool> LogBatchErrorsAsync(List<string> logOutput, string logFilename)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmss");
            //var azureBlobLogger = new AzureBlobWrapper(azureConfiguration.Value.ConnectionString, azureConfiguration.Value.LogContainer, "import_errors");

            try
            {
                // Upload plain text log
                var logContent = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, logOutput));
                await using (var logStream = new MemoryStream(logContent))
                {
                    await _logsWrapper.UploadBlobAsync(logFilename, logStream, "import_errors");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log output to Azure Blob Storage.");
                return false;
            }
        }

        public async Task<int> GetLastModifiedAssetIdsFromContentHub(int take = 1)
        {
            int batchesCreated = 0;
            try
            {
                var yesterday = DateTime.UtcNow.AddDays(-take);

                _logger.LogInformation($"Finding Last Modified Asset Ids in ContentHub - {DateTime.Now}");

                List<string> assetTypeTaxonomies = new List<string>();

                var blobStream = await _metadataLastModifiedWrapper.DownloadBlobAsync("child-taxonomies.txt");
                using (StreamReader reader = new StreamReader(blobStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (line.Contains("M.AssetType"))
                        {
                            assetTypeTaxonomies.Add(line.Replace("M.AssetType\\", ""));
                        }
                    }
                }

                foreach (var assetTypeName in assetTypeTaxonomies)
                {
                    var assetTypeEntity = await _client.Entities.GetAsync(assetTypeName);
                    var queryAssetType = Query.CreateQuery(entities => from e in entities where e.DefinitionName == "M.Asset" && e.Parent("AssetTypeToAsset") == assetTypeEntity.Id.Value && e.ModifiedOn >= yesterday select e);
                    var queryAssetTypeResult = await _client.Querying.QueryIdsAsync(queryAssetType).ConfigureAwait(false);
                    //_logOutput.Add($"Found {queryAssetTypeResult.TotalNumberOfResults} in taxonomy {assetTypeName}");
                    _logger.LogInformation($"Found {queryAssetTypeResult.TotalNumberOfResults} in taxonomy {assetTypeName}");

                    if (queryAssetTypeResult.TotalNumberOfResults > 0)
                    {
                        string folderName = assetTypeName.Replace(".", "-");
                        //IEntityIterator iterator = _client.Querying.CreateEntityIterator(queryAssetType);   // this bombs at 10,000
                        var result = _client.Querying.CreateEntityScroller(queryAssetType, TimeSpan.FromMinutes(5));
                        int batchCounter = 0;
                        while (await result.MoveNextAsync().ConfigureAwait(false))
                        {
                            batchCounter++;

                            //_logOutput.Add($"Processing batch {batchCounter} of files in taxonomy {assetTypeName}");
                            _logger.LogInformation($"Processing batch {batchCounter} of files in taxonomy {assetTypeName}");
                            var entities = result.Current.Items;
                            ;
                            try
                            {
                                // Do something with the entities
                                await SaveEntityMetadataToAzureAsync(entities, batchCounter, folderName, "metadata-lastmodified");
                                _logger.LogInformation($"Saved json metadata for {batchCounter} to log folder {folderName}");
                                batchesCreated++;
                            }
                            catch (Exception ex)
                            {
                                //_logOutput.Add($"Iteration Failure in batch {batchCounter}: {ex.Message}");
                                _logger.LogInformation($"Iteration Failure in batch {batchCounter}: {ex.Message}");
                            }
                            ;
                        }
                    }
                }

            }
            catch (Exception ex)
            {

                _logger.LogError($"Application Failure {ex.Message}");
            }
            return batchesCreated;
        }

        private async Task SaveEntityMetadataToAzureAsync(IList<IEntity> entities, int batch, string folderName, string container = "logs")
        {
            CultureInfo defaultCulture = await _client.Cultures.GetDefaultCultureAsync();
            var entityDtos = entities.Select(e => new
            {
                Id = e.Id,
                DefinitionName = e.DefinitionName,
                Properties = e.Properties.ToDictionary(p => p.Name, p => p.IsMultiLanguage ? e.GetPropertyValue(p.Name, defaultCulture) : e.GetPropertyValue(p.Name))
            });

            string json = JsonConvert.SerializeObject(entityDtos, Formatting.Indented);
            string today = DateTime.Now.ToString("yy-M-d");
            string logFilename = $"_lastmodified_{today}_batch_{batch}.json";
            var logContent = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, json));

            //await System.IO.File.WriteAllBytesAsync($"{SourceDirectory}lastmodified/{folderName}_{logFilename}", logContent);
            await using (var logStream = new MemoryStream(logContent))
            {
                if (container.Equals("metadata-lastmodified"))
                {
                    await _metadataLastModifiedWrapper.UploadBlobAsync($"{folderName}/{logFilename}", logStream);
                }
                else
                {
                    await _logsWrapper.UploadBlobAsync(logFilename, logStream);
                }

            }
        }

        private async Task<List<string>> GetAllUnProcessedCountsModified()
        {
            string[] folderBlobs = await _metadataLastModifiedWrapper.GetBlobListingAsync();
            var folderBlobsList = folderBlobs.ToList();
            List<string> outputLines = new List<string>();
            List<string> allUnProcessedBlobs = folderBlobsList.Where(b => !b.Contains($".processed") && !b.Contains($"invalid")).ToList();

            _logger.LogInformation($"Found {allUnProcessedBlobs.Count} UnProcessed Blobs.");

            return allUnProcessedBlobs;
        }

        public async Task CreateBatchExcelFiles()
        {
            string queueJobsContainerName = "queuejobs";
            var queueFilesContainer = _blobServiceClient.GetBlobContainerClient(queueJobsContainerName);
            List<string> unprocessedModifiedBlobs = await GetAllUnProcessedCountsModified();

            _logger.LogInformation($"Found {unprocessedModifiedBlobs.Count()} blob records");

            var distinctBlobPrefixes = unprocessedModifiedBlobs
            .Select(r => r?.Split('/')[0]) // safely get first part
            .Where(prefix => !string.IsNullOrEmpty(prefix)) // filter out null/empty
            .Distinct()
            .ToList();

            string[] batchArray = Enumerable.Range(0, 10)
                                            .Select(i => $"batch_{i}")
                                            .ToArray();


            foreach (var distinctBlobPrefix in distinctBlobPrefixes)
            {

                foreach (var batch in batchArray)
                {
                    var batchRecords = unprocessedModifiedBlobs.Where(r => r.Contains(distinctBlobPrefix + "/") && r.Contains(batch)).Select(b => b).Distinct().ToList();
                    if (batchRecords.Any())
                    {
                        var dataTable = new DataTable("BatchOfBlobs");
                        dataTable.Columns.Add("Blobname");

                        foreach (var batchRecord in batchRecords)
                        {
                            DataRow dataRow = dataTable.NewRow();
                            dataRow["Blobname"] = batchRecord;
                            dataTable.Rows.Add(dataRow);
                        }

                        if (dataTable.Rows.Count > 0)
                        {
                            var stream = ExcelWriter.WriteDataTable(dataTable);
                            var outputFileName = $"{distinctBlobPrefix}_{batch}_take1.xlsx";
                            await queueFilesContainer.UploadBlobAsync(outputFileName,stream);
                            _logger.LogInformation($"{outputFileName} has been queued");
                            await Task.Delay(TimeSpan.FromSeconds(30));

                        }
                    }

                }

            }
            return;

        }
    }
}
