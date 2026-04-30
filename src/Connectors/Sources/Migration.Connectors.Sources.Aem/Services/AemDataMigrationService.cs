using Migration.Connectors.Sources.Aem.Clients;
using Migration.Domain.Models;
using Migration.Shared.Storage;
using Migration.Connectors.Sources.Aem.Extensions;
using Migration.Connectors.Sources.Aem.Files;
using Migration.Connectors.Sources.Aem.Models;
using Migration.Shared.Workflows.AemToAprimo.Models;
using Migration.Connectors.Sources.Aem.Configuration;
using Migration.Shared.Configuration.Hosts.Aem;
using Migration.Shared.Configuration.Infrastructure;
using Migration.Connectors.Sources.Aem.Services;
using Migration.Manifest.Sql.Repositories;
using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
//using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
//using System.Threading.Tasks.Dataflow;
//using static System.Reflection.Metadata.BlobBuilder;
//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Migration.Connectors.Sources.Aem.Services
{
    public class AemDataMigrationService
    {

        #region Fields
        private readonly ILogger<AemDataMigrationService> _logger;
        private readonly IOptions<AemOptions> _aemOptions;
        private readonly IOptions<AzurePathOptions> _azureOptions;
        private readonly IOptions<ExportOptions> _exportOptions;
        private readonly IAemClient _aemClient;
        private readonly IAzureBlobWrapperFactory _azureFactory;
        private readonly ExecutionContextState _state;

        private string Dump;
        private string SourceDirectory;
        private string ImportsSourceDirectory;
        private string SuccessRetryFilename;
        private string LogFilename;

        private static readonly Regex MetadataFileRegex = new Regex(
            @"^[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}_metadata\.json$",
            RegexOptions.Compiled);

        private static readonly TimeSpan MaxRunTime = TimeSpan.FromHours(23);

        private AzureBlobWrapperAsync _assetsWrapper;
        private AzureBlobWrapperAsync _jobsWrapper;
        #endregion

        #region Constructors
        public AemDataMigrationService(
                    ExecutionContextState state,
                    ILogger<AemDataMigrationService> logger,
                    IOptions<AemOptions> aemOptions,
                    IOptions<ExportOptions> exportOptions,
                    IOptions<AzurePathOptions> azureOptions,
                    IOptions<AemHostOptions> hostOptions,
                    IAemClient aemClient,
                    IAzureBlobWrapperFactory azureFactory)
        {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aemOptions = aemOptions ?? throw new ArgumentNullException(nameof(aemOptions));
            _azureOptions = azureOptions ?? throw new ArgumentNullException(nameof(azureOptions));
            _exportOptions = exportOptions ?? throw new ArgumentNullException(nameof(exportOptions));
            _aemClient = aemClient ?? throw new ArgumentNullException(nameof(aemClient));
            _azureFactory = azureFactory ?? throw new ArgumentNullException(nameof(azureFactory));
            _assetsWrapper = _azureFactory.Get("assets");
            _jobsWrapper = _azureFactory.Get("jobs");
            _state = state;

            var paths = hostOptions?.Value.Paths;
            var files = hostOptions?.Value.Files;

            Dump                  = paths?.DumpDirectory           ?? @"C:\Workspace\Dump\";
            SourceDirectory       = paths?.SourceDirectory         ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Ntara", "Ashley Migration") + Path.DirectorySeparatorChar;
            ImportsSourceDirectory= paths?.ImportsSourceDirectory  ?? Path.Combine(SourceDirectory, "imports") + Path.DirectorySeparatorChar;
            SuccessRetryFilename  = files?.SuccessRetryFilename     ?? "successRetryMetadata.xlsx";
            LogFilename           = files?.LogFilename              ?? "aemMigration.log";
        }
        #endregion

        #region Main Processes
        public void CleanCSV()
        {
            TabChecker.CheckForTabs($"{Dump}allAssetsWithMetadata.csv");
            //CsvTabCleaner.RemoveTabsFromFile($"{Dump}allAssetsWithMetadata.csv", $"{Dump}allAssetsWithMetadataCleaned.csv");
        }

        public void CombineExcelFiles()
        {
            string tab = "Success";
            //string pathToExcel = @"C:\Workspace\dump\webimages\logs";
            string pathToExcel = $"{SourceDirectory}\\deltas3\\";
            var excelFiles = Directory.GetFiles(pathToExcel, "*.xlsx");


            if (excelFiles.Length == 0)
                throw new FileNotFoundException("No Excel files found in the source directory.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Combined");

                int outputRow = 1;
                bool headerWritten = false;
                //List<string> processedIds = new List<string>();
                foreach (var file in excelFiles)
                {
                    using (var inputPackage = new ExcelPackage(new FileInfo(file)))
                    {
                        try
                        {
                            var inputWorksheet = inputPackage.Workbook.Worksheets[0]; //tab // worksheet to pull
                            int rowCount = inputWorksheet.Dimension.Rows;
                            int colCount = inputWorksheet.Dimension.Columns;

                            _logger.LogInformation($" file {file} has {rowCount - 1} {tab}.");

                            int startRow = 1;
                            if (headerWritten)
                                startRow = 2; // Skip header row for subsequent files

                            // Write rows to output
                            for (int row = startRow; row <= rowCount; row++)
                            {
                                //bool bOutputRow = false;
                                for (int col = 1; col <= colCount; col++)
                                {
                                    //if (col == 2 && headerWritten)
                                    //{
                                    //    string uuid = inputWorksheet.Cells[row, col].Value.ToString()!;
                                    //    if (processedIds.Contains(uuid))
                                    //    {
                                    //        // skip
                                    //    }
                                    //    else
                                    //    {
                                    //        processedIds.Add(uuid);
                                    //        bOutputRow = true;
                                    //    }
                                    //}
                                    worksheet.Cells[outputRow, col].Value = inputWorksheet.Cells[row, col].Value;
                                }
                                if (headerWritten)
                                    outputRow++;
                            }



                            // After the first file, don't write header again
                            if (!headerWritten)
                                headerWritten = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($" file {file} has no rows! {tab}.");
                        }

                    }
                }

                // Save to output
                package.SaveAs(new FileInfo($"{pathToExcel}\\allDeltas3.xlsx"));

            }
        }

        public void CombineExcelFiles(int? maxRowsPerFile = null)
        {
            string tab = "Success";
            var excelFiles = Directory.GetFiles(SourceDirectory + "RerunFirstRun\\logs\\", "*.xlsx");

            if (excelFiles.Length == 0)
                throw new FileNotFoundException("No Excel files found in the source directory.");

            string outputDir = Path.Combine(SourceDirectory, "RerunFirstRun", "success");
            Directory.CreateDirectory(outputDir);

            string baseFileNameWithoutExt = "allSuccesses";

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            ExcelPackage? currentPackage = null;
            ExcelWorksheet? currentWorksheet = null;

            int currentOutputRow = 1;                 // row index in current sheet
            int currentFileDataRowCount = 0;          // data (non-header) rows in current file
            int globalDataRowIndex = 1;               // next logical data row index across all files
            int currentFileStartIndex = 1;            // first data row index for current output file

            // Header handling
            string[]? headerRowValues = null;
            int headerColCount = 0;
            bool headerCaptured = false;

            // Dedup across all files
            HashSet<string> processedIds = new HashSet<string>();

            void StartNewOutputFile()
            {
                currentPackage = new ExcelPackage();
                currentWorksheet = currentPackage.Workbook.Worksheets.Add("Combined");
                currentOutputRow = 1;
                currentFileDataRowCount = 0;
                currentFileStartIndex = globalDataRowIndex;

                // Write header row if we already captured it
                if (headerCaptured && headerRowValues != null)
                {
                    for (int col = 0; col < headerColCount; col++)
                    {
                        currentWorksheet.Cells[currentOutputRow, col + 1].Value = headerRowValues[col];
                    }
                    currentOutputRow++; // next row is for data
                }
            }

            void SaveAndDisposeCurrentFile()
            {
                if (currentPackage == null)
                    return;

                string suffix = string.Empty;
                if (maxRowsPerFile.HasValue && maxRowsPerFile.Value > 0)
                {
                    suffix = $"_{currentFileStartIndex}";
                }

                string outputPath = Path.Combine(outputDir, $"{baseFileNameWithoutExt}{suffix}.xlsx");
                currentPackage.SaveAs(new FileInfo(outputPath));

                currentPackage.Dispose();
                currentPackage = null;
                currentWorksheet = null;
            }

            try
            {
                foreach (var file in excelFiles)
                {
                    using (var inputPackage = new ExcelPackage(new FileInfo(file)))
                    {
                        var inputWorksheet = inputPackage.Workbook.Worksheets[tab];  // worksheet to pull
                        int rowCount = inputWorksheet.Dimension.Rows;
                        int colCount = inputWorksheet.Dimension.Columns;

                        _logger.LogInformation($" file {file} has {rowCount - 1} {tab}.");

                        // Capture header row from the first file only
                        if (!headerCaptured)
                        {
                            headerColCount = colCount;
                            headerRowValues = new string[colCount];
                            for (int col = 1; col <= colCount; col++)
                            {
                                headerRowValues[col - 1] = inputWorksheet.Cells[1, col].Value?.ToString();
                            }

                            headerCaptured = true;

                            // Ensure first output file exists and has header
                            if (currentPackage == null)
                            {
                                StartNewOutputFile();
                            }
                        }

                        // Skip header row in all source files (we already captured it)
                        int startRow = 2;

                        for (int row = startRow; row <= rowCount; row++)
                        {
                            // Get UUID from column 2 (adjust if different)
                            string uuid = inputWorksheet.Cells[row, 2].Value?.ToString() ?? string.Empty;

                            // Dedup: only output rows with new UUIDs
                            if (string.IsNullOrEmpty(uuid) || !processedIds.Add(uuid))
                            {
                                continue; // skip duplicates or empty ids
                            }

                            // If we have a max rows limit and current file is full, start a new file
                            if (maxRowsPerFile.HasValue && maxRowsPerFile.Value > 0 &&
                                currentFileDataRowCount >= maxRowsPerFile.Value)
                            {
                                SaveAndDisposeCurrentFile();
                                StartNewOutputFile();
                            }

                            // Ensure we have an output file (in case maxRowsPerFile was null)
                            if (currentPackage == null || currentWorksheet == null)
                            {
                                StartNewOutputFile();
                            }

                            // Copy row values to current output worksheet
                            for (int col = 1; col <= colCount; col++)
                            {
                                currentWorksheet.Cells[currentOutputRow, col].Value =
                                    inputWorksheet.Cells[row, col].Value;
                            }

                            currentOutputRow++;
                            currentFileDataRowCount++;
                            globalDataRowIndex++;
                        }
                    }
                }
            }
            finally
            {
                // Save the last file (if any)
                SaveAndDisposeCurrentFile();
            }
        }

        public void CombineExcelFilesToCsv(string[] excelFiles, string outputCsvPath, bool excludeZeroBytes = false)
        {
            if (excelFiles == null || excelFiles.Length == 0)
                throw new ArgumentException("No Excel files supplied.", nameof(excelFiles));

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var writer = new StreamWriter(outputCsvPath);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            });

            bool headerWritten = false;
            int recordCount = 0;
            foreach (var file in excelFiles)
            {
                using var package = new ExcelPackage(new FileInfo(file));
                var worksheet = package.Workbook.Worksheets[0];  // Use first worksheet
                if (worksheet == null)
                    continue;

                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;

                // Write header from the first file only
                if (!headerWritten)
                {
                    for (int col = 1; col <= colCount; col++)
                    {
                        csv.WriteField(worksheet.Cells[1, col].Text);
                    }
                    csv.NextRecord();
                    headerWritten = true;
                }

                // Write data rows from row 2 onwards
                for (int row = 2; row <= rowCount; row++)
                {
                    if (excludeZeroBytes)
                    {
                        // column 5 is SizeBytes
                        if (worksheet.Cells[row, 5].Text == "0")
                        {
                            _logger.LogInformation($"Skipping Imageset from output");
                            continue;
                        }
                    }
                    for (int col = 1; col <= colCount; col++)
                    {
                        csv.WriteField(worksheet.Cells[row, col].Text);
                    }
                    recordCount++;
                    csv.NextRecord();
                }
            }
            _logger.LogInformation($"Record count {recordCount}");
        }

        public void CombineMetadataFiles()
        {
            var excelFiles = Directory.GetFiles($"{SourceDirectory}RerunFirstRun\\allMetadata", "*.xlsx");

            CombineExcelFilesToCsv(excelFiles, $"{SourceDirectory}RerunFirstRun\\allMetadata\\allMetadata.csv");
        }

        public void CombineSuccessFiles()
        {
            //var excelFiles = Directory.GetFiles($"{SourceDirectory}RerunFirstRun\\allSuccessData", "*.xlsx");

            //CombineExcelFilesToCsv(excelFiles, $"{SourceDirectory}RerunFirstRun\\allSuccessData\\allAssets_success.csv");

            var excelFiles = Directory.GetFiles($"{SourceDirectory}RerunFirstRun", "*.xlsx");

            CombineExcelFilesToCsv(excelFiles, $"{SourceDirectory}RerunFirstRun\\allSuccessData\\allAssets_success.csv", true);

        }

        public static MemoryStream ConvertListToMemoryStream(List<string> lines)
        {
            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
            {
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }
                writer.Flush();               // Ensure all content is written
            }

            memoryStream.Position = 0;        // Rewind for reading/upload
            return memoryStream;
        }

        public async Task CreateAssetUploadSpreadsheets(CancellationToken cancellationToken)
        {
            foreach (var folder in _exportOptions.Value.Folders)
            {
                //await svc.ExportFolderAsync(folder, recursive, CancellationToken.None);
                var dt = await CreateImportSheetAsync(folder, _exportOptions.Value.Recursive, CancellationToken.None, true);

                // write out the file
                var stream = ExcelWriter.WriteDataTable(dt);
                string tableName = folder.Replace("/content/dam/", "").Replace("/", "_");
                var outputFileName = $"{SourceDirectory}deltas3\\{tableName}.xlsx";

                using var fsOut = new FileStream(outputFileName, FileMode.Create, FileAccess.Write);

                stream.WriteTo(fsOut);

                _logger.LogInformation($"Export completed for {folder} (recursive={_exportOptions.Value.Recursive}).");
            }
        }

        public void CreateBatchExcelFilesSimple()
        {
            var file = "allAssetsWithMetadata_300001.xlsx";
            var outputFolder = @"C:\Workspace\dump\allSuccessData\allAssetsWithMetadata\allAssetsWithMetadata_4"; // $"{ImportsSourceDirectory}Splits4";
            var inputFileName = $"{file}";
            string taxonomy = "allAssetsWithMetadata4_";
            ExcelSplitter.SplitExcelFile(
                $"{outputFolder}\\{inputFileName}",
                $"{outputFolder}",
                "aprimoimport",
                taxonomy,
                1000 // Optional, defaults to 500 rows per file
            );


        }

        public async Task CreateDeltaUploadSpreadsheets(CancellationToken cancellationToken)
        {
            foreach (var folder in _exportOptions.Value.Folders)
            {
                //await svc.ExportFolderAsync(folder, recursive, CancellationToken.None);
                var dt = await CreateImportSheetAsync(folder, _exportOptions.Value.Recursive, CancellationToken.None, true);

                // write out the file
                var stream = ExcelWriter.WriteDataTable(dt);
                string tableName = folder.Replace("/content/dam/", "").Replace("/", "_");
                var outputFileName = $"{SourceDirectory}deltas2\\{tableName}.xlsx";

                using var fsOut = new FileStream(outputFileName, FileMode.Create, FileAccess.Write);

                stream.WriteTo(fsOut);

                _logger.LogInformation($"Export completed for {folder} (recursive={_exportOptions.Value.Recursive}).");
            }
        }
        public void CreateExcelChunksFromDBCSV()
        {
            var chunker = new CsvToExcelChunker();

            chunker.SplitCsvToExcelChunks(
                @"C:\Workspace\dump\assetsNotInAprimo.csv",
                @"C:\Workspace\dump\allSuccessData\missingAssets",
                rowsPerFile: 150_000 // optional, default is 100k
            );
        }

        public async Task<DataTable> CreateImportSheetAsync(string aemFolderPath, bool recursive, CancellationToken ct = default, bool useLastModifiedOnly = false)
        {
            string tableName = aemFolderPath.Replace("/content/dam/", "").Replace("/", "_");
            var dataTable = new DataTable(tableName);

            dataTable.Columns.Add(nameof(AemAsset.Id));
            dataTable.Columns.Add(nameof(AemAsset.Name));
            dataTable.Columns.Add(nameof(AemAsset.Path));
            dataTable.Columns.Add(nameof(AemAsset.MimeType));
            dataTable.Columns.Add(nameof(AemAsset.SizeBytes));
            dataTable.Columns.Add(nameof(AemAsset.Created));
            dataTable.Columns.Add(nameof(AemAsset.LastModified));

            await foreach (var asset in _aemClient.EnumerateAssetsAsync(aemFolderPath, recursive, _logger, ct, useLastModifiedOnly))
            {
                //await ExportAssetAsync(asset, ct);
                DataRow taxDataRow = dataTable.NewRow();
                taxDataRow[nameof(AemAsset.Id)] = asset.Id;
                taxDataRow[nameof(AemAsset.Name)] = asset.Name;
                taxDataRow[nameof(AemAsset.Path)] = asset.Path;
                taxDataRow[nameof(AemAsset.MimeType)] = asset.MimeType;
                taxDataRow[nameof(AemAsset.SizeBytes)] = asset.SizeBytes;
                taxDataRow[nameof(AemAsset.Created)] = asset.Created;
                taxDataRow[nameof(AemAsset.LastModified)] = asset.LastModified;
                dataTable.Rows.Add(taxDataRow);
            }

            return dataTable;
        }

        public async Task CreateInitialProdMappingsFile(CancellationToken cancellationToken)
        {

            // total image set lookup
            ////var allOrigFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            //var allDeltaFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.DeltasRootPrefix);
            ////var allOrigImagesetFiles = allOrigFiles.Where(x => x.EndsWith("_related.json")).ToList();
            //var allDeltaImagesetFiles = allDeltaFiles.Where(x => x.EndsWith("_related.json")).ToList();
            ////var allFilesTogether = allOrigImagesetFiles.Concat(allDeltaImagesetFiles).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allDeltaImagesetFiles);
            //SaveStreamToFile(stream, SourceDirectory, "allDeltaImagesetBlobs.csv");
            //stream.Dispose();

            var allDelta2Files = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.Deltas2RootPrefix);
            var allDelta3Files = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.Deltas3RootPrefix);

            var allDelta2ImagesetFiles = allDelta2Files.Where(x => x.EndsWith("_related.json")).ToList();
            var allDelta3ImagesetFiles = allDelta3Files.Where(x => x.EndsWith("_related.json")).ToList();

            MemoryStream stream = ConvertListToMemoryStream(allDelta2ImagesetFiles);
            SaveStreamToFile(stream, SourceDirectory, "allDelta2ImagesetBlobs.csv");
            stream.Dispose();

            MemoryStream stream3 = ConvertListToMemoryStream(allDelta3ImagesetFiles);
            SaveStreamToFile(stream3, SourceDirectory, "allDelta3ImagesetBlobs.csv");
            stream3.Dispose();

            ;
            string locale = "en-US";
            string languageId = "00000000000000000000000000000000";

            ResetState();

            var logOutput = new List<string>();

            // find all imagesets

            // get everything so we can do lookups
            //var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            //var allImagesetFiles = allFiles.Where(x => x.EndsWith("_related.json")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allImagesetFiles);
            //SaveStreamToFile(stream, SourceDirectory, "allImagesetBlobs.csv");
            //stream.Dispose();

            //var allAssetFiles2 = allFiles.Where(x => !x.EndsWith(".json")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allAssetFiles2);
            //SaveStreamToFile(stream, SourceDirectory, "allAssetBlobs2.csv");
            //stream.Dispose();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null,
                MissingFieldFound = null
            };

            List<AemAsset> knownRecords = new List<AemAsset>();

            // @"C:\Workspace\dump\allAssets_success.csv"
            // $"{SourceDirectory}All_assets_in_aprimo_prod.txt"
            using (var reader = new StreamReader(@"C:\Workspace\dump\allAssets_success.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Context.RegisterClassMap<AemAssetMap>();

                knownRecords = csv.GetRecords<AemAsset>().ToList();
            }

                    ;
            _logger.LogInformation($"Found {knownRecords.Count()} known records");
            logOutput.Add($"Found {knownRecords.Count()} known records");

            var allImagesetFiles = File.ReadAllLines($"{SourceDirectory}\\allImagesetBlobs.csv");
            _logger.LogInformation($"Found {allImagesetFiles.Count()} image sets in Azure");
            logOutput.Add($"Found {allImagesetFiles.Count()} image sets in Azure");

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");

            //var allAssetFiles2 = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs2.csv");
            //_logger.LogInformation($"Found {allAssetFiles2.Count()} assets in Azure2");
            //logOutput.Add($"Found {allAssetFiles2.Count()} assets in Azure2");

            var allAssetFilesMinusKO = File.ReadAllLines($"{SourceDirectory}\\zallAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFilesMinusKO.Count()} assets in Azure minus knockouts");
            logOutput.Add($"Found {allAssetFilesMinusKO.Count()} assets in Azure minus knockouts");
            string fileName = "prodMappings.xlsx";

            Dictionary<string, MappingHelperObject> prodMappings = new Dictionary<string, MappingHelperObject>();
            foreach (var azureFilePath in allAssetFilesMinusKO)
            {
                string azureFileName = Path.GetFileName(azureFilePath);
                string[] azureFileNameParts = azureFileName.Split("_");
                string aemUUID = azureFileNameParts[0];
                string azureCleanName = azureFileName.Replace(aemUUID + "_", "");
                MappingHelperObject mho = new MappingHelperObject();

                mho.AemAssetId = aemUUID;

                mho.AzureAssetPath = azureFilePath;
                mho.AzureAssetName = azureCleanName;

                try
                {
                    var knownAsset = knownRecords.Where(x => x.Id == aemUUID).FirstOrDefault();

                    if (knownAsset != null)
                    {
                        mho.AemCreatedDate = knownAsset.Created;
                        mho.AemAssetPath = knownAsset.Path;
                        mho.AzureAssetName = Path.GetFileName(knownAsset.Path);

                        if (!prodMappings.ContainsKey(aemUUID))
                        {
                            prodMappings.Add(aemUUID, mho);
                        }
                        else
                        {
                            _logger.LogInformation($"{aemUUID} already exists in mappings!");
                            logOutput.Add($"{aemUUID} already exists in mappings!");
                        }

                    }
                    else
                    {

                        var aemRecord = await _aemClient.GetAssetByUUID(aemUUID, _logger);

                        if (aemRecord != null)
                        {
                            mho.AemCreatedDate = aemRecord.Created;
                            mho.AemAssetPath = aemRecord.Path;
                            mho.AzureAssetName = Path.GetFileName(aemRecord.Path);

                            if (!prodMappings.ContainsKey(aemUUID))
                            {
                                prodMappings.Add(aemUUID, mho);
                            }
                            else
                            {
                                _logger.LogInformation($"{aemUUID} already exists in mappings!");
                                logOutput.Add($"{aemUUID} already exists in mappings!");
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"dumb ass shit happend {aemUUID}: {ex.Message}");
                    logOutput.Add($"dumb ass shit happend {aemUUID}: {ex.Message}");
                }


            }
            await LogToAzure(fileName, logOutput);

            // Serialize to a JSON string 
            string jsonString = JsonConvert.SerializeObject(prodMappings, Formatting.None);
            File.WriteAllText($"{Dump}initialCreatedProdMappings.json", jsonString);


        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(sr))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(jsonReader);
            }
        }

        public async Task DetermineAemAssetsStatusFrommUploadSpreadsheet(CancellationToken cancellationToken)
        {
            ResetState();

            var logOutput = new List<string>();

            //string imageSetAssetsjsonString = File.ReadAllText($"{Dump}\\baseFiles\\allImageSetAssets.json");
            //Dictionary<string, AprimoImageSetAssets> allAssetImageSetAssets = JsonConvert.DeserializeObject<Dictionary<string, AprimoImageSetAssets>>(imageSetAssetsjsonString);
            //_logger.LogInformation($"Found {allAssetImageSetAssets.Count()} image set assets");

            //string aemjsonString = File.ReadAllText($"{Dump}\\baseFiles\\AllProdMappings.json");
            //Dictionary<string, MappingHelperObject> allProdMappings = JsonConvert.DeserializeObject<Dictionary<string, MappingHelperObject>>(aemjsonString);
            //_logger.LogInformation($"Found {allProdMappings.Count()} Prod Mappings");

            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository imagesetRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSets");
            MappingHelperObjectsRepository assetRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.AssetMetadata");
            MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjectsFlat");
            List<string> allImageSetKeys = imagesetRepo.GetAllDictKeys();
            List<string> allAssetKeys = assetRepo.GetAllDictKeys();

            string inputFileName = "allDeltas3.xlsx";

            var fileData = await ReadImportSpreadsheet(inputFileName, _azureOptions.Value.ImportsRootPrefix);

            _state.SuccessTable.Columns.Add("Status");
            _state.SuccessTable.Columns.Add("BinaryChanged");

            _logger.LogInformation($"Processing {fileData.Count()} rows from {inputFileName}");

            int rowCounter = 0;
            foreach (var rowData in fileData)
            {
                rowCounter++;
                var uuid = rowData["Id"];
                var path = rowData["Path"];
                var byteSize = rowData["SizeBytes"];
                long lngBytes = 0;

                var fileName = Path.GetFileName(path);
                var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{Path.GetDirectoryName(path)}";
                var azureFolder = assetFolder.Replace("\\", "/");
                string cleanedFilename = Regex.Replace(fileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

                // handle empty binaries
                bool bHasHashedUUID = false;
                if (string.IsNullOrEmpty(uuid))
                {
                    uuid = GetFolderHash(path);
                    bHasHashedUUID = true;
                }


                string azureFilename = $"{uuid}_{cleanedFilename}";


                // used to include this but we have some binaries that have no UUID:  string.IsNullOrEmpty(uuid) || 
                if (string.IsNullOrEmpty(byteSize) || byteSize == "0")
                {
                    string folderHash = GetFolderHash(Path.GetDirectoryName(path));

                    string uniqueId = $"{folderHash}_{cleanedFilename}";
                    azureFilename = $"{uniqueId}_metadata.json";
                    _logger.LogInformation($"{rowCounter}: Testing image set status for {azureFilename}");
                    if (allImageSetKeys.Contains(uniqueId))
                    {
                        rowData["Status"] = "Update";
                    } else
                    {
                        rowData["Status"] = "New";
                    }


                } else
                {
                    lngBytes = long.Parse(byteSize);
                    _logger.LogInformation($"{rowCounter}: Testing asset status for {azureFilename}");

                    if (allAssetKeys.Contains(uuid))
                    {
                        rowData["Status"] = "Update";
                    } else
                    {
                        rowData["Status"] = "New";
                    }
                }

                if (rowData["Status"] == "Update")
                {
                    try
                    {
                        bool blobExists = await _assetsWrapper.BlobExistsAsync($"{azureFilename}", azureFolder);
                        if (!blobExists)
                        {
                            assetFolder = $"{_azureOptions.Value.DeltasRootPrefix}{Path.GetDirectoryName(path)}";
                            azureFolder = assetFolder.Replace("\\", "/");
                            blobExists = await _assetsWrapper.BlobExistsAsync($"{azureFilename}", azureFolder);
                            ;
                        }

                        if (blobExists)
                        {
                            var blobLastModified = await _assetsWrapper.GetBlobLastModified($"{azureFilename}", azureFolder);

                            string dateString = rowData["LastModified"];

                            string status = blobLastModified > DateTimeOffset.Parse(
                                                dateString.Replace("GMT", ""),
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.AssumeUniversal)
                                            ? "Already Updated (blob is newer than last modified)"
                                            : "Needs Update";

                            rowData["Status"] = status;

                            if (lngBytes > 0)
                            {
                                var blobSize = await _assetsWrapper.GetBlobSize($"{azureFilename}", azureFolder);

                                if (blobSize != lngBytes)
                                {
                                    rowData["BinaryChanged"] = "True";
                                }
                                else
                                {
                                    rowData["BinaryChanged"] = "False";
                                }
                            }
                        } else
                        {
                            _logger.LogInformation($"{rowCounter}: Cannot find blob for {azureFilename}");
                        }


                    }
                    catch (Exception e)
                    {
                        _logger.LogInformation($"failed to get blob last modified: Exception {e.Message}");

                    }
                }


                LogRowData(true, rowData, $"Success");
            }

            SaveRowData();

            await LogToAzure(inputFileName, logOutput);


            //if (!fileName.Contains(".processed"))
            //{
            //    string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{fileName}";
            //    await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
            //}

        }

        public async Task DoMinuteTasks(CancellationToken cancellationToken)
        {
            int minutesToWait = 2;
            var importFolderFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.ImportsRootPrefix);
            var unprocessedImports = importFolderFiles.Where(b => !b.EndsWith(".processed")).ToList();
            bool isStillProcessing = false;

            // Run until app shuts down
            while (!cancellationToken.IsCancellationRequested && unprocessedImports.Count > 0)
            {
                _logger.LogInformation("Task running at: {time}", DateTimeOffset.Now);

                try
                {

                    string filename = Path.GetFileName(unprocessedImports[0]);
                    Stream blobStream = null;
                    try
                    {
                        blobStream = await _assetsWrapper.DownloadBlobAsync(filename, _azureOptions.Value.ImportsRootPrefix);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"can't find {filename} anymore.  must be processed now.  skip to next");
                        importFolderFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.ImportsRootPrefix);
                        unprocessedImports = importFolderFiles.Where(b => !b.EndsWith(".processed")).ToList();
                        continue;
                    }


                    ResetState();
                    var fileData = await ReadImportSpreadsheet(blobStream);
                    var firstRowData = fileData.FirstOrDefault();
                    var path = firstRowData["Path"];

                    var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var firstFive = string.Join("/", parts.Take(parts.Count() - 1));
                    var firstFivePath = path.StartsWith("/") ? "/" + firstFive : firstFive;
                    string folderToMonitor = $"{_azureOptions.Value.AssetRootPrefix}{firstFivePath}";
                    //var folderToMonitor = Path.GetDirectoryName(path);

                    if (!isStillProcessing)
                    {
                        Console.WriteLine($"Moving file to queuejobs: {filename}");
                        BlobClient src = await _assetsWrapper.GetBlobClientAsync(filename, _azureOptions.Value.ImportsRootPrefix);
                        var test = await _jobsWrapper.GetBlobListingAsync();
                        BlobClient trg = await _jobsWrapper.GetNewBlobClientAsync(filename);

                        await _jobsWrapper.MoveBlobAsync(src, trg, false);
                        ;

                    }
                    else
                    {
                        Console.WriteLine($"The file was still processing, don't queue it again");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(minutesToWait), cancellationToken);  // we've waited X minutes

                    // check to see if import count is still going up
                    var filesImportedToFolder = await _assetsWrapper.GetBlobListingAsync(folderToMonitor);

                    int initFolderCount = filesImportedToFolder.Count();
                    Console.WriteLine($"Files in Folder {folderToMonitor} after {minutesToWait} minutes: {initFolderCount}");

                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);  // we've waited 1 more minute

                    filesImportedToFolder = await _assetsWrapper.GetBlobListingAsync(folderToMonitor);
                    int laterFolderCount = filesImportedToFolder.Count();
                    Console.WriteLine($"Files in Folder {folderToMonitor} after another minute: {laterFolderCount}");

                    isStillProcessing = laterFolderCount > initFolderCount;

                    Console.WriteLine($"Is Still processing {filename} = {isStillProcessing}");

                    importFolderFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.ImportsRootPrefix);
                    unprocessedImports = importFolderFiles.Where(b => !b.EndsWith(".processed")).ToList();

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Oops! something bad happened: {ex.Message}");
                }

            }
        }

        public async Task FixProdMappingsFile(CancellationToken cancellationToken)
        {
            string connectionString = @"Server=(LocalDB)\MSSQLLocalDB;Integrated Security=true;Initial Catalog=ashley;";
            string allProdMappings = File.ReadAllText($"{Dump}\\baseFiles\\AllProdMappings.json");
            Dictionary<string, MappingHelperObject> allProdMappingObjects = JsonConvert.DeserializeObject<Dictionary<string, MappingHelperObject>>(allProdMappings);
            _logger.LogInformation($"Found {allProdMappingObjects.Count()} prod mappings");

            List<Dictionary<string, string>> allSuccessData = new List<Dictionary<string, string>>();
            //for(int i=1;i< 1200000;i+=100000)
            //{

            //    var stream = File.Open($"{Dump}\\allSuccessData\\allAssetsWithMetadata\\allAssetsWithMetadata_{i}.xlsx", FileMode.Open, FileAccess.Read);
                var stream = File.Open($"{Dump}\\allSuccessData\\missingAssets\\assetsNotInAprimo_1.xlsx", FileMode.Open, FileAccess.Read);
                var fileDataData = await ReadImportSpreadsheet(stream);
                allSuccessData.AddRange(fileDataData);
            //}

            //List<MappingHelperObject> mhos = allProdMappingObjects.Where(x => string.IsNullOrEmpty(x.Value.AemAssetPath)).Select(v => v.Value).ToList();
            List<string> mhos = allProdMappingObjects.Where(x => string.IsNullOrEmpty(x.Value.AemAssetPath)).Select(v => v.Key).ToList();
            MappingHelperObjectsRepository mhor = new MappingHelperObjectsRepository(connectionString);

            foreach (string key in mhos)
            {
                var mho = allProdMappingObjects[key];
                var aemRecord = await _aemClient.GetAssetByUUID(mho.AemAssetId, _logger);

                if (aemRecord != null)
                {
                    mho.AemCreatedDate = aemRecord.Created;
                    mho.AemAssetPath = aemRecord.Path;
                    mho.AzureAssetName = Path.GetFileName(aemRecord.Path);

                    string updatedJsonString = JsonConvert.SerializeObject(mho, Formatting.None);
                    mhor.UpdateJsonBody(key, updatedJsonString);
                } else
                {
                    var aemMappingFromSuccessData = allSuccessData.Where(x => x["Id"].Equals(mho.AemAssetId)).FirstOrDefault();
                    if (aemMappingFromSuccessData != null)
                    {
                        mho.AemCreatedDate = aemMappingFromSuccessData["Created"];
                        mho.AemAssetPath = aemMappingFromSuccessData["Path"];
                        mho.AzureAssetName = Path.GetFileName(aemMappingFromSuccessData["Path"]);

                        string updatedJsonString = JsonConvert.SerializeObject(mho, Formatting.None);
                        mhor.UpdateJsonBody(key, updatedJsonString);
                    }
                    else
                    {
                        _logger.LogInformation($"WHOA! WhAt? : {key}");

                    }
                }
            }

            // Serialize to a JSON string 
            string jsonString = JsonConvert.SerializeObject(allProdMappingObjects, Formatting.None);
            File.WriteAllText($"{Dump}updatedProdMappings.json", jsonString);

            ;
        }
        public async Task GetAllAssetMetadata(int? maxRowsPerFile = null)
        {
            List<AssetMetadata> allMetaData = new List<AssetMetadata>();

            var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            var allMetadataJsonFiles = allFiles
                .Where(x => x.EndsWith("_metadata.json") && MetadataFileRegex.IsMatch(Path.GetFileName(x)))
                .ToList();

            foreach (var file in allMetadataJsonFiles)
            {
                string fileName = Path.GetFileName(file);
                string directoryPath = Path.GetDirectoryName(file);
                string json = await ReadJsonFile(fileName, directoryPath);

                var metadata = JsonConvert.DeserializeObject<AssetMetadata>(json);
                string uuid = fileName.Replace("_metadata.json", "");
                metadata.UUID = uuid;
                allMetaData.Add(metadata);
            }

            // No max rows set: behave exactly as before
            if (!maxRowsPerFile.HasValue || maxRowsPerFile.Value <= 0)
            {
                var table = DataTableConverter.ToDataTable(allMetaData);

                await using var excelStream = ExcelWriter.WriteDataTable(table);
                excelStream.Position = 0;

                string excelFilename = "allMetadata.xlsx";
                await _assetsWrapper.UploadBlobAsync(excelFilename, excelStream, _azureOptions.Value.LogsRootPrefix);
                return;
            }

            // Split into multiple files when maxRowsPerFile is specified
            int limit = maxRowsPerFile.Value;
            int totalRows = allMetaData.Count;
            int startIndex = 0;                 // 0-based index in allMetaData
            int globalRowStartNumber = 1;       // 1-based row number across all data

            while (startIndex < totalRows)
            {
                // Take chunk
                var chunk = allMetaData
                    .Skip(startIndex)
                    .Take(limit)
                    .ToList();

                // Convert chunk to DataTable and write to Excel
                var tableChunk = DataTableConverter.ToDataTable(chunk);

                await using var excelStream = ExcelWriter.WriteDataTable(tableChunk);
                excelStream.Position = 0;

                // Suffix uses the starting row number for this chunk
                string excelFilename = $"allMetadata_{globalRowStartNumber}.xlsx";

                await _assetsWrapper.UploadBlobAsync(excelFilename, excelStream, _azureOptions.Value.LogsRootPrefix);

                // Move to next chunk
                startIndex += limit;
                globalRowStartNumber += chunk.Count;
            }
        }

        public async Task GetAllRelatedDataJson()
        {
            var logOutput = new List<string>();

            ////var importBaseFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.BaseRootPrefix);

            //var imageSetFiles = importBaseFiles.Where(x => x.EndsWith("_imagesets.xlsx")).ToList();
            //var baseAssetFiles = importBaseFiles.Where(x => !x.EndsWith("_imagesets.xlsx")).ToList();

            Dictionary<string, RelatedData> allRelatedData = new Dictionary<string, RelatedData>();

            //foreach (var file in imageSetFiles)
            //{
            //    string fileName = Path.GetFileName(file);
            //    var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.BaseRootPrefix);

            //    foreach (var rowData in fileData)
            //    {
            //        string imageSetPath = rowData["Path"];
            //        string imageSetFileName = Path.GetFileName(imageSetPath);
            //        string directoryPath = Path.GetDirectoryName(imageSetPath);

            //        try
            //        {

            //            var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{directoryPath}";
            //            //var assetMetadataFolder = $"{_azureOptions.Value.MetadataRootPrefix}/{Path.GetDirectoryName(path)}";
            //            string cleanedFilename = Regex.Replace(imageSetFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();
            //            string folderHash = GetFolderHash(directoryPath);
            //            //string azureMetadataFilename = $"{folderHash}_{cleanedFilename}_metadata.json";
            //            string azureRelatedFilename = $"{folderHash}_{cleanedFilename}_related.json";

            //            string json = await ReadJsonFile(azureRelatedFilename, assetFolder);
            //            var relatedData = JsonConvert.DeserializeObject<RelatedData>(json);
            //            allRelatedData.Add($"{imageSetPath}", relatedData);
            //        }
            //        catch (Exception ex)
            //        {
            //            logOutput.Add($"Error processing related json for {imageSetPath}: {ex.ToString()}");
            //        }

            //    }
            //}

            var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            var allRelatedJsonFiles = allFiles.Where(x => x.EndsWith("related.json")).ToList();

            foreach (var file in allRelatedJsonFiles)
            {
                string fileName = Path.GetFileName(file);

                if (fileName.StartsWith("IS_"))
                {
                    Console.WriteLine($"OH NOES!  Need to rename this one {fileName}");
                }

                string normalFileName = fileName.Substring(17).Replace("_related.json", "");

                string directoryPath = Path.GetDirectoryName(file);

                // Normalize path
                string normalized = directoryPath
                    .Replace("assets\\", "")   // remove "assets\"
                    .Replace("\\", "/");       // replace backslashes

                // Ensure it starts with "/"
                if (!normalized.StartsWith("/"))
                    normalized = "/" + normalized;

                // Append the extracted ID
                string finalPath = $"{normalized}/{normalFileName}";

                string json = await ReadJsonFile(fileName, directoryPath);
                var relatedData = JsonConvert.DeserializeObject<RelatedData>(json);

                allRelatedData.Add($"{finalPath}", relatedData);
            }

            string jsonRelated = JsonConvert.SerializeObject(allRelatedData);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonRelated));


            await _assetsWrapper.UploadBlobAsync("all-imagesets-related-data.json", stream, _azureOptions.Value.LogsRootPrefix);
        }

        public async Task ImportAemAssetRenditionsDirectlyFromAzure(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();

            //string startingUUID = "9e53be05-fd3a-4265-b6b5-7f5ea59412ec";
            string outputVersion = "2";
            bool hasStarted = true;  //set to false to use startingUUID above
            var stopwatch = Stopwatch.StartNew();

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");


            foreach (var file in allAssetFiles)
            {

                string fileName = Path.GetFileName(file);
                string assetFolder = Path.GetDirectoryName(file);

                string[] parts = fileName.Split('_');

                string uuid = parts[0];
                string cleanedFilename = parts[1];

                string renditionsFolder = $"{assetFolder}\\{uuid}_renditions";
                string cleanRenditionsFolder = renditionsFolder.Replace("\\", "/");

                bool hasRenditions = false;
                try
                {
                    var existingRenditions = await _assetsWrapper.GetBlobListingAsync(cleanRenditionsFolder, cancellationToken).ConfigureAwait(false);
                    if (existingRenditions.Length > 0)
                    {
                        hasRenditions = true;
                    }
                }
                catch (Exception ex)
                {
                    // nope
                }


                string azureJcrContentFilename = $"{uuid}_jcrcontent.json";

                // ⛔ Stop after 23 hours
                if (stopwatch.Elapsed >= MaxRunTime)
                {
                    Console.WriteLine($"⏱ 23-hour time limit reached. Next starting uuid is {uuid}");
                    logOutput.Add($"23-hour time limit reached. Next starting uuid is {uuid}");
                    break;
                }

                //if (uuid == startingUUID)
                //{
                //    hasStarted = true;
                //}

                if (hasStarted)
                {
                    logOutput.Add($"processing {uuid}");
                    Console.WriteLine($"processing {uuid}");
                    if (hasRenditions)
                    {
                        logOutput.Add($" {uuid} already has renditions.  skipping.");
                    }
                    else
                    {
                        try
                        {
                            var aemAsset = await _aemClient.GetAssetByUUID(uuid, _logger, cancellationToken);

                            if (aemAsset != null)
                            {
                                try
                                {
                                    await using var jcrStream = await _aemClient.GetJcrContentAsync(aemAsset.Path, cancellationToken);
                                    await _assetsWrapper.UploadBlobAsync(azureJcrContentFilename, jcrStream, assetFolder);

                                    // process renditions 
                                    //  don't use this.  sometimes jcrContent doesn't list the renditions but the folder still exists.
                                    //   additionally, the jcrContent renditions listing contains failed renditions and will cause 404s later
                                    //string json = await ReadJsonFile(azureJcrContentFilename, assetFolder);
                                    //var jcrContent = JsonConvert.DeserializeObject<AemJcrContent>(json);

                                    var renditionsStream = await _aemClient.GetRenditionsFolderAsync(aemAsset.Path, cancellationToken);
                                    var renditions = DeserializeFromStream<List<AemRendition>>(renditionsStream);

                                    //if (jcrContent.DamProcessingRenditions != null)
                                    //{
                                    //    logOutput.Add($"found {jcrContent.DamProcessingRenditions.Count} for {uuid}");
                                    //    foreach (string rendition in jcrContent.DamProcessingRenditions)
                                    //    {
                                    //        try
                                    //        {
                                    //            await using var renditionStream = await _aemClient.GetRenditionAsync(aemAsset.Path, rendition, cancellationToken);
                                    //            await _assetsWrapper.UploadBlobAsync(rendition, renditionStream, renditionsFolder);
                                    //        }
                                    //        catch (Exception ex)
                                    //        {
                                    //            logOutput.Add($"failed to get rendition {rendition} ex: {ex.Message}");
                                    //        }

                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    logOutput.Add($"no renditions found for {uuid}");
                                    //}

                                    if (renditions != null)
                                    {
                                        logOutput.Add($"found {renditions.Count} for {uuid}");
                                        foreach (var rendition in renditions)
                                        {
                                            try
                                            {
                                                if (rendition.Id != "original")
                                                {
                                                    await using var renditionStream = await _aemClient.GetRenditionsAsync(aemAsset.Path, rendition.Id, cancellationToken);
                                                    await _assetsWrapper.UploadBlobAsync(rendition.Id, renditionStream, renditionsFolder);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                logOutput.Add($"failed to get rendition {rendition.Id} ex: {ex.Message}");
                                            }

                                        }
                                    }
                                    else
                                    {
                                        logOutput.Add($"no renditions found for {uuid}");
                                    }

                                }
                                catch (Exception ex)
                                {
                                    logOutput.Add($"Error getting renditions for uuid: {uuid}. ex: {ex.Message}");
                                }


                            }
                            else
                            {
                                logOutput.Add($"Could not find asset for uuid: {uuid}");
                            }
                        }
                        catch (Exception ex)
                        {
                            logOutput.Add($"An error occurred for uuid: {uuid}. ex: {ex.Message}");
                        }
                    }


                }


            }
            await LogToAzure($"renditionsOutput{outputVersion}.xlsx", logOutput);

        }

        public async Task ImportAemAssetRenditionsDirectlyFromRetryFile(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();

            string outputVersion = "1_retry";
            bool hasStarted = true;
            var stopwatch = Stopwatch.StartNew();

            var allRetryUUIDs = File.ReadAllLines($"{SourceDirectory}\\renditions1_failed_guids.csv");

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");


            foreach (var guid in allRetryUUIDs)
            {
                var file = allAssetFiles.Where(x => x.Contains(guid)).FirstOrDefault();

                string fileName = Path.GetFileName(file);
                string assetFolder = Path.GetDirectoryName(file);

                string[] parts = fileName.Split('_');

                string uuid = parts[0];
                string cleanedFilename = parts[1];

                string renditionsFolder = $"{assetFolder}/{uuid}_renditions";

                string azureJcrContentFilename = $"{uuid}_jcrcontent.json";

                // ⛔ Stop after 23 hours
                if (stopwatch.Elapsed >= MaxRunTime)
                {
                    Console.WriteLine($"⏱ 23-hour time limit reached. Next starting uuid is {uuid}");
                    logOutput.Add($"23-hour time limit reached. Next starting uuid is {uuid}");
                    break;
                }

                if (hasStarted)
                {
                    logOutput.Add($"processing {uuid}");

                    try
                    {
                        var aemAsset = await _aemClient.GetAssetByUUID(uuid, _logger, cancellationToken);

                        if (aemAsset != null)
                        {
                            try
                            {
                                await using var jcrStream = await _aemClient.GetJcrContentAsync(aemAsset.Path, cancellationToken);
                                await _assetsWrapper.UploadBlobAsync(azureJcrContentFilename, jcrStream, assetFolder);

                                var renditionsStream = await _aemClient.GetRenditionsFolderAsync(aemAsset.Path, cancellationToken);
                                var renditions = DeserializeFromStream<List<AemRendition>>(renditionsStream);

                                //if (jcrContent.DamProcessingRenditions != null)
                                //{
                                //    logOutput.Add($"found {jcrContent.DamProcessingRenditions.Count} for {uuid}");
                                //    foreach (string rendition in jcrContent.DamProcessingRenditions)
                                //    {
                                //        try
                                //        {
                                //            await using var renditionStream = await _aemClient.GetRenditionAsync(aemAsset.Path, rendition, cancellationToken);
                                //            await _assetsWrapper.UploadBlobAsync(rendition, renditionStream, renditionsFolder);
                                //        }
                                //        catch (Exception ex)
                                //        {
                                //            logOutput.Add($"failed to get rendition {rendition} ex: {ex.Message}");
                                //        }

                                //    }
                                //}
                                //else
                                //{
                                //    logOutput.Add($"no renditions found for {uuid}");
                                //}

                                if (renditions != null)
                                {
                                    logOutput.Add($"found {renditions.Count} for {uuid}");
                                    foreach (var rendition in renditions)
                                    {
                                        try
                                        {
                                            if (rendition.Id != "original")
                                            {
                                                await using var renditionStream = await _aemClient.GetRenditionsAsync(aemAsset.Path, rendition.Id, cancellationToken);
                                                await _assetsWrapper.UploadBlobAsync(rendition.Id, renditionStream, renditionsFolder);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            logOutput.Add($"failed to get rendition {rendition.Id} ex: {ex.Message}");
                                        }

                                    }
                                }
                                else
                                {
                                    logOutput.Add($"no renditions found for {uuid}");
                                }

                            }
                            catch (Exception ex)
                            {
                                logOutput.Add($"Error getting renditions for uuid: {uuid}. ex: {ex.Message}");
                            }


                        }
                        else
                        {
                            logOutput.Add($"Could not find asset for uuid: {uuid}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logOutput.Add($"An error occurred for uuid: {uuid}. ex: {ex.Message}");
                    }
                }


            }
            await LogToAzure($"renditionsOutput{outputVersion}.xlsx", logOutput);

        }

        public async Task ImportAemAssetsFromAllUnprocessedSpreadsheets(CancellationToken cancellationToken)
        {

            var imports = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.ImportsRootPrefix);
            List<string> allUnProcessedImports = imports.Where(b => !b.Contains($".processed")).ToList();

            foreach (var import in allUnProcessedImports)
            {
                ResetState();

                string fileName = Path.GetFileName(import);
                var logOutput = new List<string>();

                var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.ImportsRootPrefix);

                _logger.LogInformation($"Processing {fileData.Count()} rows");
                logOutput.Add($"Processing {fileData.Count()} rows");

                await ProcessBatchesAsync(fileData, logOutput, 1);

                SaveRowData();

                await LogToAzure(fileName, logOutput);


                if (!fileName.Contains(".processed"))
                {
                    await _assetsWrapper.MoveBlobAsync(import, $"{import}.processed");
                }

            }


        }

        public async Task ImportAemAssetsFromUploadSpreadsheet(CancellationToken cancellationToken)
        {
            ResetState();

            var logOutput = new List<string>();

            string fileName = "ashley-furniture_webimages_swatches-500_missing.xlsx";

            var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.ImportsRootPrefix);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {fileName}");

            await ProcessBatchesAsync(fileData, logOutput, 1);

            SaveRowData();

            await LogToAzure(fileName, logOutput);


            if (!fileName.Contains(".processed"))
            {
                string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{fileName}";
                await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
            }

        }

        public async Task ImportDeltaAemAssetsFromUploadSpreadsheet(CancellationToken cancellationToken)
        {
            ResetState();

            var logOutput = new List<string>();

            string fileName = "allDeltas3_Metadata.xlsx";

            var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.ImportsRootPrefix);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {fileName}");

            string folderPrefix = _azureOptions.Value.Deltas3RootPrefix;

            foreach (var rowData in fileData)
            {
                var byteSize = rowData["SizeBytes"];
                //var path = rowData["Path"];



                // used to include this but we have some binaries that have no UUID:  string.IsNullOrEmpty(uuid) || 
                if (string.IsNullOrEmpty(byteSize) || byteSize == "0")
                {
                    // process image set
                    await ProcessDeltaImageSetsAsync(rowData, logOutput, folderPrefix);

                } else
                {
                    //process asset
                    await ProcessDeltaAssetAsync(rowData, logOutput, folderPrefix);
                }
                    
            }
            

            SaveRowData();

            await LogToAzure(fileName, logOutput);


            if (!fileName.Contains(".processed"))
            {
                string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{fileName}";
                await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
            }

        }


        public async Task MapAssetsToImageSets(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();

            var importBaseFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.BaseRootPrefix);
            //var baseAssetFiles = importBaseFiles.Where(x => !x.EndsWith("_imagesets.xlsx") && !x.EndsWith(".json")).ToList();
            var baseAssetFiles = importBaseFiles.Where(x => !x.EndsWith(".json")).ToList();

            Dictionary<string, RelatedData> allRelatedData = new Dictionary<string, RelatedData>();

            string json = await ReadJsonFile("all-imagesets-related-data.json", _azureOptions.Value.BaseRootPrefix);

            allRelatedData = JsonConvert.DeserializeObject<Dictionary<string, RelatedData>>(json);

            logOutput.Clear();
            int fileCounter = 0;
            foreach (var file in baseAssetFiles)
            {
                ResetState();
                string fileName = Path.GetFileName(file);
                var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.BaseRootPrefix, true);

                if (!_state.SuccessTable.Columns.Contains("ImageSets"))
                {
                    _state.SuccessTable.Columns.Add("ImageSetsCount");
                    _state.SuccessTable.Columns.Add("ImageSets");
                }

                _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");

                foreach (var rowData in fileData)
                {
                    var uuid = rowData["Id"];
                    var path = rowData["Path"];
                    var byteSize = rowData["SizeBytes"];

                    try
                    {

                        // this shouldnt be the case because they are already weeded out.
                        if (string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(byteSize))
                        {
                            continue;
                        }

                        var matchingKeys = allRelatedData
                                            .Where(kvp => kvp.Value?.Resources != null &&
                                                          kvp.Value.Resources.Contains(path, StringComparer.OrdinalIgnoreCase))
                                            .Select(kvp => kvp.Key)
                                            .ToList();
                        rowData["ImageSetsCount"] = matchingKeys.Count().ToString();
                        rowData["ImageSets"] = string.Join(",", matchingKeys);
                        LogRowData(true, rowData, $"{path} exists in image sets : {rowData["ImageSets"]}");
                    }
                    catch (Exception ex)
                    {
                        logOutput.Add($"Error processing row {uuid} : {ex.ToString()}");
                        LogRowData(false, rowData, $"Error processing row {uuid} : {ex.ToString()}");
                    }

                }
                fileCounter++;
                SaveRowData();
                await LogToAzure($"asset-to-imageset-mapping_{fileCounter}.xlsx", logOutput);
            }

        }

        public async Task MapImagesetsToAssetsCSV()
        {
            var input = $"{SourceDirectory}RerunFirstRun\\allSuccessData\\allAssets_success.csv";
            var output = $"{SourceDirectory}RerunFirstRun\\allSuccessData\\allAssets_success_mapped.csv";

            await ProcessCsvWithImageSetsAsync(input, output);

        }

        public async Task MergeExcelFiles()
        {
            DataTable assetsTable = new DataTable();
            DataTable metadataTable = new DataTable();

            using (Stream fileStream = new FileStream(SourceDirectory + "base//allAssets_success.xlsx", FileMode.Open, FileAccess.Read))
            {
                assetsTable = ExcelReader.LoadExcelWorksheetsToDataTables(fileStream).FirstOrDefault() ?? new DataTable();
            }

            using (Stream fileStream = new FileStream(SourceDirectory + "base//allMetadata.xlsx", FileMode.Open, FileAccess.Read))
            {
                assetsTable = ExcelReader.LoadExcelWorksheetsToDataTables(fileStream).FirstOrDefault() ?? new DataTable();
            }

            var result = (from a in assetsTable.AsEnumerable()
                          join b in metadataTable.AsEnumerable()
                          on a.Field<int>("Id") equals b.Field<int>("Id")
                          select new
                          {
                              A_Row = a,
                              B_Row = b
                          }).ToList();

            DataTable merged = assetsTable.Copy();

            foreach (var pair in result)
            {
                foreach (DataColumn col in metadataTable.Columns)
                {
                    if (col.ColumnName == "Id") continue; // Skip key

                    // If column doesn't exist in merged table, add it
                    if (!merged.Columns.Contains(col.ColumnName))
                        merged.Columns.Add(col.ColumnName, col.DataType);

                    pair.A_Row[col.ColumnName] = pair.B_Row[col.ColumnName];
                }
            }

            await using var excelStream = ExcelWriter.WriteDataTable(merged);
            excelStream.Position = 0;

            using (var outFileStream = new FileStream(SourceDirectory + "base//assetMigratedWithMetadata.xlsx", FileMode.Create, FileAccess.Write))
            {
                excelStream.CopyTo(outFileStream);
            }

        }

        public async Task OutputImageSetCounts(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();

            var importBaseFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.BaseRootPrefix);
            var imageSetFiles = importBaseFiles.Where(x => x.EndsWith("_imagesets.xlsx")).ToList();

            Dictionary<string, RelatedData> allRelatedData = new Dictionary<string, RelatedData>();

            string json = await ReadJsonFile("all-imagesets-related-data.json", _azureOptions.Value.BaseRootPrefix);

            allRelatedData = JsonConvert.DeserializeObject<Dictionary<string, RelatedData>>(json);

            var allDistinctPaths = allRelatedData
                .SelectMany(kvp => kvp.Value.Resources) // flatten all resource lists
                .Distinct()                              // remove duplicates
                .ToList();

            logOutput.Clear();
            foreach (var file in imageSetFiles)
            {
                string fileName = Path.GetFileName(file);
                var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.BaseRootPrefix, false);

                if (!_state.SuccessTable.Columns.Contains("RelatedCounts"))
                {
                    _state.SuccessTable.Columns.Add("RelatedCounts");
                    _state.SuccessTable.Columns.Add("RelatedAssets");
                }

                _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");

                foreach (var rowData in fileData)
                {
                    var path = rowData["Path"];

                    try
                    {
                        var resources = allRelatedData
                            .Where(kvp => kvp.Key == path)
                            .Select(kvp => kvp.Value.Resources)
                            .FirstOrDefault();

                        rowData["RelatedCounts"] = resources.Count().ToString();
                        rowData["RelatedAssets"] = string.Join(",", resources);

                        LogRowData(true, rowData, $"{path} in image sets has {rowData["RelatedCounts"]} assets");
                    }
                    catch (Exception ex)
                    {
                        logOutput.Add($"Error processing row {path} : {ex.ToString()}");
                        LogRowData(false, rowData, $"Error processing row {path} : {ex.ToString()}");
                    }

                }

            }
            SaveRowData();

            await LogToAzure("imageset-asset-counts.xlsx", logOutput);
        }

        public async Task ProcessAllAssets(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();

            var importBaseFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.BaseRootPrefix);
            var baseAssetFiles = importBaseFiles.Where(x => !x.EndsWith(".json")).ToList();

            Dictionary<string, RelatedData> allRelatedData = new Dictionary<string, RelatedData>();

            string json = await ReadJsonFile("all-imagesets-related-data.json", _azureOptions.Value.BaseRootPrefix);

            allRelatedData = JsonConvert.DeserializeObject<Dictionary<string, RelatedData>>(json);
            ResetState();
            logOutput.Clear();
            long totalByteSize = 0;
            foreach (var file in baseAssetFiles)
            {

                string fileName = Path.GetFileName(file);
                var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.BaseRootPrefix, true);

                if (!_state.SuccessTable.Columns.Contains("ImageSets"))
                {
                    _state.SuccessTable.Columns.Add("ImageSetsCount");
                    _state.SuccessTable.Columns.Add("ImageSets");
                    _state.SuccessTable.Columns.Add("Reason");

                    _state.RetryTable.Columns.Add("ImageSetsCount");
                    _state.RetryTable.Columns.Add("ImageSets");
                }

                _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");

                foreach (var rowData in fileData)
                {
                    var uuid = rowData["Id"];
                    var path = rowData["Path"];
                    var byteSize = rowData["SizeBytes"];
                    totalByteSize = totalByteSize + long.Parse(byteSize);
                    try
                    {
                        var matchingKeys = allRelatedData
                                            .Where(kvp => kvp.Value?.Resources != null &&
                                                          kvp.Value.Resources.Contains(path, StringComparer.OrdinalIgnoreCase))
                                            .Select(kvp => kvp.Key)
                                            .ToList();
                        rowData["ImageSetsCount"] = matchingKeys.Count().ToString();
                        rowData["ImageSets"] = string.Join(",", matchingKeys);

                        await ProcessAssetAsync(rowData, logOutput);

                    }
                    catch (Exception ex)
                    {
                        logOutput.Add($"Error processing row {uuid} : {ex.ToString()}");
                        LogRowData(false, rowData, $"Error processing row {uuid} : {ex.ToString()}");
                    }
                }

            }

            logOutput.Add($"Total ByteSize of Assets : {totalByteSize}");

            //SaveRowDataJson();

            SaveRowData();
            await LogToAzure("allAssets.xlsx", logOutput);
        }

        public async Task ProcessAllImageSets(CancellationToken cancellationToken)
        {
            Dictionary<string, RelatedData> allRelatedData = new Dictionary<string, RelatedData>();
            var logOutput = new List<string>();

            var importBaseFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.ImportsRootPrefix);

            foreach (var file in importBaseFiles)
            {
                ResetState();
                //logOutput.Clear();

                string fileName = Path.GetFileName(file);
                var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.ImportsRootPrefix, false, true);

                if (!_state.SuccessTable.Columns.Contains("RelatedCounts"))
                {
                    _state.SuccessTable.Columns.Add("RelatedCounts");
                    _state.SuccessTable.Columns.Add("RelatedAssets");
                    _state.RetryTable.Columns.Add("RelatedCounts");
                    _state.RetryTable.Columns.Add("RelatedAssets");
                }

                _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");
                logOutput.Add($"Processing {fileData.Count()} rows from {fileName}");

                await ProcessImageSetBatchesAsync(fileData, logOutput, allRelatedData, 1, false);

                SaveRowData();


                //if (!fileName.Contains(".processed"))
                //{
                //    string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{fileName}";
                //    await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
                //}
                //}
            }

            await LogToAzure("allWebimageImageSets", logOutput);

            string jsonRelated = JsonConvert.SerializeObject(allRelatedData);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonRelated));
            await _assetsWrapper.UploadBlobAsync("all-webiamges-imagesets-related-data.json", stream, _azureOptions.Value.BaseRootPrefix);
        }

        public async Task ProcessAllRetryAssets(CancellationToken cancellationToken)
        {
            ResetState();

            var logOutput = new List<string>();

            string fileName = "allAssets_retry.xlsx";

            var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.BaseRootPrefix);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {fileName}");


            Dictionary<string, RelatedData> allRelatedData = new Dictionary<string, RelatedData>();

            string json = await ReadJsonFile("all-imagesets-related-data.json", _azureOptions.Value.BaseRootPrefix);

            allRelatedData = JsonConvert.DeserializeObject<Dictionary<string, RelatedData>>(json);


            if (!_state.SuccessTable.Columns.Contains("ImageSets"))
            {
                _state.SuccessTable.Columns.Add("ImageSetsCount");
                _state.SuccessTable.Columns.Add("ImageSets");
                _state.SuccessTable.Columns.Add("Reason");

                _state.RetryTable.Columns.Add("ImageSetsCount");
                _state.RetryTable.Columns.Add("ImageSets");
            }

            _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");

            foreach (var rowData in fileData)
            {
                var uuid = rowData["Id"];
                var path = rowData["Path"];
                var byteSize = rowData["SizeBytes"];
                long lngByteSize = long.Parse(byteSize);
                if (lngByteSize > 0)
                {
                    try
                    {
                        var matchingKeys = allRelatedData
                                            .Where(kvp => kvp.Value?.Resources != null &&
                                                            kvp.Value.Resources.Contains(path, StringComparer.OrdinalIgnoreCase))
                                            .Select(kvp => kvp.Key)
                                            .ToList();
                        rowData["ImageSetsCount"] = matchingKeys.Count().ToString();
                        rowData["ImageSets"] = string.Join(",", matchingKeys);

                        await ProcessAssetAsyncLocal(rowData, logOutput);

                    }
                    catch (Exception ex)
                    {
                        logOutput.Add($"Error processing row {uuid} : {ex.ToString()}");
                        LogRowData(false, rowData, $"Error processing row {uuid} : {ex.ToString()}");
                    }
                }
                else
                {
                    logOutput.Add($"Skipping Imagesets");
                }

            }


            SaveRowDataJson();

            SaveRowData();
            await LogToAzure("allRetryAssets.xlsx", logOutput);
        }

        public async Task ProcessBatchesAsync(IEnumerable<Dictionary<string, string>> assets, List<string> logOutput, int take = 1)
        {
            foreach (var batch in assets.Batch(take))
            {
                // Process each batch of X assets
                var tasks = batch.Select(rowData => ProcessAssetAsync(rowData, logOutput));
                await Task.WhenAll(tasks.AsParallel());
            }
        }

        public async Task ProcessBatchesIntoAzureAsync(string fileName, Stream blobStream, int take = 1)
        {
            _logger.LogInformation("Invocation with state InstanceId: {Id}", _state.InstanceId);

            ResetState();

            var logOutput = new List<string>();

            var fileData = await ReadImportSpreadsheet(blobStream);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {fileName}");

            await ProcessBatchesAsync(fileData, logOutput, take);


            _logger.LogInformation($"Saving success/retry RowData to log");
            try
            {
                SaveRowData();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error Saving success/retry RowData to log: {ex.Message}");
            }


            _logger.LogInformation($"Saving log file to log folder");
            try
            {
                await LogToAzure(fileName, logOutput);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error Saving Log to log folder: {ex.Message}");
            }

            if (!fileName.Contains(".processed"))
            {
                _logger.LogInformation($"Saving {fileName} as .processed");
                string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{fileName}";
                await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
            }

            _logger.LogInformation($"Finished processing {fileName}");
        }

        public async Task ProcessBatchesIntoAzureAsyncFromQueueJobs(string fileName, Stream blobStream, int take = 1)
        {
            _logger.LogInformation("Invocation with state InstanceId: {Id}", _state.InstanceId);

            ResetState();

            var logOutput = new List<string>();

            var fileData = await ReadImportSpreadsheet(blobStream);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {fileName}");

            await ProcessBatchesAsync(fileData, logOutput, take);

            _logger.LogInformation($"Saving success/retry RowData to log");
            try
            {
                SaveRowData();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error Saving success/retry RowData to log: {ex.Message}");
            }


            _logger.LogInformation($"Saving log file to log folder");
            try
            {
                await LogToAzure(fileName, logOutput);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error Saving Log to log folder: {ex.Message}");
            }


            _logger.LogInformation($"Finished processing {fileName}");

        }

        public async Task ProcessCsvWithImageSetsAsync(
                    string inputCsvPath,
                    string outputCsvPath
                   )
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
                BadDataFound = null
            };

            using var reader = new StreamReader(inputCsvPath);
            using var csvReader = new CsvReader(reader, config);

            using var writer = new StreamWriter(outputCsvPath);
            using var csvWriter = new CsvWriter(writer, config);

            Dictionary<string, RelatedData> allRelatedData = new Dictionary<string, RelatedData>();

            string json = await ReadJsonFile("all-imagesets-related-data.json", _azureOptions.Value.BaseRootPrefix);

            allRelatedData = JsonConvert.DeserializeObject<Dictionary<string, RelatedData>>(json);

            // Read the header
            await csvReader.ReadAsync();
            csvReader.ReadHeader();

            // Get original header row
            var headerRow = csvReader.HeaderRecord.ToList();

            // Add your new columns
            headerRow.Add("ImageSetsCount");
            headerRow.Add("ImageSets");

            // Write the modified header
            foreach (var header in headerRow)
                csvWriter.WriteField(header);

            await csvWriter.NextRecordAsync();

            // Process rows one by one
            while (await csvReader.ReadAsync())
            {
                // --- STEP 1: Write all original row fields ---
                foreach (var header in csvReader.HeaderRecord)
                {
                    csvWriter.WriteField(csvReader.GetField(header));
                }

                // --- STEP 2: Check dictionary using Path ---
                string pathValue = csvReader.GetField("Path");


                var matchingKeys = allRelatedData
                                    .Where(kvp => kvp.Value?.Resources != null &&
                                                  kvp.Value.Resources.Contains(pathValue, StringComparer.OrdinalIgnoreCase))
                                    .Select(kvp => kvp.Key)
                                    .ToList();

                // Populate new columns
                csvWriter.WriteField(matchingKeys.Count().ToString());
                csvWriter.WriteField(string.Join(";", string.Join(",", matchingKeys)));


                await csvWriter.NextRecordAsync();
            }
        }

        public async Task ProcessImageSetBatchesAsync(IEnumerable<Dictionary<string, string>> assets, List<string> logOutput, Dictionary<string, RelatedData> allRelatedData, int take = 1, bool skipRelatedData = true)
        {
            foreach (var batch in assets.Batch(take))
            {
                // Process each batch of X assets
                var tasks = batch.Select(rowData => ProcessImageSetsAsync(rowData, logOutput, allRelatedData, skipRelatedData));
                await Task.WhenAll(tasks.AsParallel());
            }
        }

        public async Task ProcessImageSets(CancellationToken cancellationToken)
        {
            Dictionary<string, RelatedData> allRelatedData = new Dictionary<string, RelatedData>();
            var logOutput = new List<string>();

            var importBaseFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.ImportsRootPrefix);
            var onlyprocessfile = "retryreport-imagesets-import.xlsx";
            foreach (var file in importBaseFiles)
            {
                ResetState();
                logOutput.Clear();

                string fileName = Path.GetFileName(file);
                if (onlyprocessfile == fileName)
                {
                    var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.ImportsRootPrefix);

                    if (!_state.SuccessTable.Columns.Contains("RelatedCounts"))
                    {
                        _state.SuccessTable.Columns.Add("RelatedCounts");
                        _state.SuccessTable.Columns.Add("RelatedAssets");
                    }

                    _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");
                    logOutput.Add($"Processing {fileData.Count()} rows from {fileName}");

                    await ProcessImageSetBatchesAsync(fileData, logOutput, allRelatedData, 1);

                    SaveRowData();

                    await LogToAzure("imageset_" + fileName, logOutput);


                    //if (!fileName.Contains(".processed"))
                    //{
                    //    string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{fileName}";
                    //    await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
                    //}
                }

            }

        }

        public async Task ProcessRetryImageSets(CancellationToken cancellationToken)
        {

            Dictionary<string, RelatedData> allRelatedData = new Dictionary<string, RelatedData>();

            var logOutput = new List<string>();


            string file = "allAssets_retry.xlsx";

            var fileData = await ReadImportSpreadsheet(file, _azureOptions.Value.BaseRootPrefix);

            logOutput.Clear();

            string fileName = Path.GetFileName(file);

            if (!_state.SuccessTable.Columns.Contains("RelatedCounts"))
            {
                _state.SuccessTable.Columns.Add("RelatedCounts");
                _state.SuccessTable.Columns.Add("RelatedAssets");
            }

            _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {fileName}");

            await ProcessImageSetBatchesAsync(fileData, logOutput, allRelatedData, 1, false);

            SaveRowData();

            await LogToAzure("imageset_" + fileName, logOutput);

            string json = await ReadJsonFile("all-imagesets-related-data.json", _azureOptions.Value.BaseRootPrefix);

            var allRelatedDataBefore = JsonConvert.DeserializeObject<Dictionary<string, RelatedData>>(json);

            foreach (var ard in allRelatedData)
            {
                allRelatedDataBefore.Add(ard.Key, ard.Value);
            }

            string jsonRelated = JsonConvert.SerializeObject(allRelatedDataBefore);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonRelated));


            await _assetsWrapper.UploadBlobAsync("all-imagesets-related-data.json", stream, _azureOptions.Value.LogsRootPrefix);

            //if (!fileName.Contains(".processed"))
            //{
            //    string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{fileName}";
            //    await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
            //}


        }

        public async Task ReProcessAllAssets(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();

            var importBaseFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.BaseRootPrefix);
            var baseAssetFiles = importBaseFiles.Where(x => !x.EndsWith(".json")).ToList();

            Dictionary<string, RelatedData> allRelatedData = new Dictionary<string, RelatedData>();

            string json = await ReadJsonFile("all-imagesets-related-data.json", _azureOptions.Value.BaseRootPrefix);

            allRelatedData = JsonConvert.DeserializeObject<Dictionary<string, RelatedData>>(json);
            ResetState();
            logOutput.Clear();
            long totalByteSize = 0;
            long totalRows = 0;
            foreach (var file in baseAssetFiles)
            {

                string fileName = Path.GetFileName(file);
                var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.BaseRootPrefix, true);

                if (!_state.SuccessTable.Columns.Contains("ImageSets"))
                {
                    _state.SuccessTable.Columns.Add("ImageSetsCount");
                    _state.SuccessTable.Columns.Add("ImageSets");
                    _state.SuccessTable.Columns.Add("Reason");

                    _state.RetryTable.Columns.Add("ImageSetsCount");
                    _state.RetryTable.Columns.Add("ImageSets");
                }

                _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");

                var missedRows = fileData.Where(r => r["SizeBytes"] == "0" && Path.HasExtension(r["Path"])).ToList();

                _logger.LogInformation($"Found {missedRows.Count()} missing rows from {fileName}");

                foreach (var rowData in missedRows)
                {
                    var uuid = rowData["Id"];
                    var path = rowData["Path"];
                    var byteSize = rowData["SizeBytes"];
                    long lngByteSize = long.Parse(byteSize);
                    totalByteSize = totalByteSize + lngByteSize;


                    try
                    {
                        var matchingKeys = allRelatedData
                                            .Where(kvp => kvp.Value?.Resources != null &&
                                                          kvp.Value.Resources.Contains(path, StringComparer.OrdinalIgnoreCase))
                                            .Select(kvp => kvp.Key)
                                            .ToList();
                        rowData["ImageSetsCount"] = matchingKeys.Count().ToString();
                        rowData["ImageSets"] = string.Join(",", matchingKeys);
                        totalRows++;
                        LogRowData(true, rowData, $"Success {totalRows}");

                    }
                    catch (Exception ex)
                    {
                        logOutput.Add($"Error processing row {uuid} : {ex.ToString()}");
                        LogRowData(false, rowData, $"Error processing row {uuid} : {ex.ToString()}");
                    }


                }

            }

            logOutput.Add($"Total ByteSize of Assets : {totalByteSize}");

            //SaveRowDataJson();

            SaveRowData();
            await LogToAzure("allMissingAssets.xlsx", logOutput);
        }

        public async Task<IEnumerable<Dictionary<string, string>>> ReadImportSpreadsheet(Stream s)
        {

            var importDataTable = ExcelReader.LoadExcelWorksheetsToDataTables(s).FirstOrDefault() ?? new DataTable();

            // configure output tables
            if (_state.SuccessTable.Columns.Count == 0)
            {
                DataRow dr = importDataTable.Rows[0];
                foreach (var column in dr.Table.Columns)
                {
                    var columnName = $"{column}";
                    _state.SuccessTable.Columns.Add(columnName);
                    _state.RetryTable.Columns.Add(columnName);
                }
                _state.RetryTable.Columns.Add("Reason");
            }

            var fileData = new List<Dictionary<string, string>>();

            foreach (DataRow dataRow in importDataTable.Rows)
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

        public async Task<IEnumerable<Dictionary<string, string>>> ReadImportSpreadsheet(string fileName, string rootFolder, bool skipEmptyIds = false, bool skipUUIDs = false)
        {
            string importFile = $"{rootFolder}/{fileName}";
            var fileData = new List<Dictionary<string, string>>();
            try
            {
                // read file to stream
                _logger.LogInformation($"Reading {importFile} from Azure.");
                MemoryStream ms = new MemoryStream();
                var s = await _assetsWrapper.DownloadBlobAsync(importFile);
                s.CopyTo(ms);
                ms.Position = 0;

                var importDataTable = ExcelReader.LoadExcelWorksheetsToDataTables(ms).FirstOrDefault() ?? new DataTable();
                ms.Dispose();

                // configure output tables
                if (_state.SuccessTable.Columns.Count == 0)
                {
                    DataRow dr = importDataTable.Rows[0];
                    foreach (var column in dr.Table.Columns)
                    {
                        var columnName = $"{column}";
                        _state.SuccessTable.Columns.Add(columnName);
                        _state.RetryTable.Columns.Add(columnName);
                    }
                    _state.RetryTable.Columns.Add("Reason");
                }



                foreach (DataRow dataRow in importDataTable.Rows)
                {
                    var rowData = new Dictionary<string, string>();

                    foreach (var column in dataRow.Table.Columns)
                    {
                        var columnName = $"{column}";
                        var value = dataRow[columnName].ToString() ?? string.Empty;
                        rowData.Add(columnName, value.Trim());
                    }

                    if (skipEmptyIds)
                    {
                        if (string.IsNullOrEmpty(rowData["Id"]))
                        {
                            continue;
                        }
                        else
                        {
                            fileData.Add(rowData);
                        }
                    }
                    else if (skipUUIDs)
                    {
                        if (rowData["SizeBytes"] != "0")
                        {
                            continue;
                        }
                        else
                        {
                            fileData.Add(rowData);
                        }
                    }
                    else
                    {
                        fileData.Add(rowData);
                    }

                }


            }
            catch (Exception ex)
            {
                _logger.LogInformation($"error reading {fileName}: {ex.Message}");
            }

            return fileData;
        }

        public async Task<string> ReadJsonFile(string fileName, string rootFolder)
        {
            string importFile = $"{rootFolder}/{fileName}";

            // read file to stream
            _logger.LogInformation($"Reading {importFile} from Azure.");
            MemoryStream ms = new MemoryStream();
            var s = await _assetsWrapper.DownloadBlobAsync(importFile);
            s.CopyTo(ms);
            ms.Position = 0;

            string result = Encoding.UTF8.GetString(ms.ToArray());

            ms.Dispose();

            return result;
        }

        public async Task RenameImageSets(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();

            var importBaseFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.BaseRootPrefix);

            var imageSetFiles = importBaseFiles.Where(x => x.EndsWith("_imagesets.xlsx")).ToList();


            foreach (var file in imageSetFiles)
            {
                //string file = "ashley-furniture_webimages_package-knockouts_imagesets.xlsx";
                string fileName = Path.GetFileName(file);
                var fileData = await ReadImportSpreadsheet(fileName, _azureOptions.Value.BaseRootPrefix);

                foreach (var rowData in fileData)
                {
                    string imageSetPath = rowData["Path"];
                    string imageSetFileName = Path.GetFileName(imageSetPath);

                    string directoryPath = Path.GetDirectoryName(imageSetPath);

                    var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{directoryPath}";
                    //var assetAzureFolder = $"{_azureOptions.Value.AssetRootPrefix}/{directoryPath}";
                    string cleanedFilename = Regex.Replace(imageSetFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

                    string folderHash = GetFolderHash(directoryPath);

                    string azureMetadataFilename = $"IS_{cleanedFilename}_metadata.json";
                    string azureRelatedFilename = $"IS_{cleanedFilename}_related.json";

                    string azureMetadataNewFilename = $"{folderHash}_{cleanedFilename}_metadata.json";
                    string azureRelatedNewFilename = $"{folderHash}_{cleanedFilename}_related.json";

                    try
                    {
                        Console.WriteLine($"Renaming blob metadata file {azureMetadataFilename} to {azureMetadataNewFilename}");
                        BlobClient src = await _assetsWrapper.GetBlobClientAsync(azureMetadataFilename, assetFolder);

                        if (src != null)
                        {
                            BlobClient trg = await _assetsWrapper.GetNewBlobClientAsync(azureMetadataNewFilename, assetFolder);
                            await _assetsWrapper.MoveBlobAsync(src, trg, true);
                        }

                        BlobClient srcRelated = await _assetsWrapper.GetBlobClientAsync(azureRelatedFilename, assetFolder);

                        if (srcRelated != null)
                        {
                            BlobClient trg = await _assetsWrapper.GetNewBlobClientAsync(azureRelatedNewFilename, assetFolder);
                            await _assetsWrapper.MoveBlobAsync(srcRelated, trg, true);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR Renaming blob metadata file {azureMetadataFilename} to {azureMetadataNewFilename}: {ex.Message}");
                    }

                }
            }

        }

        public void ResetState()
        {
            _state.SuccessTable.Clear();
            _state.SuccessTable.Reset();
            _state.RetryTable.Clear();
            _state.RetryTable.Reset();
            _state.Successes.Clear();
            _state.Failures.Clear();
        }

        public static void SaveStreamToFile(Stream inputStream, string directoryPath, string fileName)
        {
            // Ensure the directory exists
            Directory.CreateDirectory(directoryPath);

            // Combine to get the full path
            string filePath = Path.Combine(directoryPath, fileName);

            // Create or overwrite the file and copy the stream
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                inputStream.CopyTo(fileStream);
            }
        }

        public void SplitLargeImportSpreadsheets()
        {
            //testing only...
            //var jobId = Guid.NewGuid().ToString();

            //var payload = new
            //{
            //    JobId = jobId,
            //    RequestedAt = DateTime.UtcNow,
            //    Input = "getrelateddata_execute"
            //};
            //string queueMessage = JsonConvert.SerializeObject(payload);


            string[] files = Directory.GetFiles($"{ImportsSourceDirectory}");

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string folderName = Path.GetFileNameWithoutExtension(fileName);
                Console.WriteLine($"Splitting {fileName}");

                var inputFileName = $"{file}";
                var outputFolder = $"{ImportsSourceDirectory}Splits4";
                ExcelSplitter.SplitByPathPart(inputFileName, outputFolder, 5, true);

            }

            //var inputFileName = $"{ImportsSourceDirectory}Splits/ashley-furniture_studiophotography/upholstery.xlsx";
            //var outputFolder = $"{ImportsSourceDirectory}Splits/ashley-furniture_studiophotography/Splits/upholstery";
            //ExcelSplitter.SplitByPathPart(inputFileName, outputFolder,6) ;
        }

        public async Task TagAllAssetsInAzure()
        {
            var logOutput = new List<string>();

            var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            var allAssetFiles = allFiles.Where(x => !x.EndsWith(".json")).ToList();

            _logger.LogInformation($"tagging {allAssetFiles.Count} assets");
            foreach (var file in allAssetFiles)
            {
                string fileName = Path.GetFileName(file);
                string[] parts = fileName.Split("_");

                string directoryPath = Path.GetDirectoryName(file);

                var actualFilename = ExtractActualFilename(file);
                var folderHash = FolderHash(directoryPath);

                var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["folderHash"] = folderHash,
                    ["originalFilename"] = ToTagSafeName(actualFilename)
                };

                await _assetsWrapper.SetTagsAsync(file, tags);

                logOutput.Add(parts[0]);
            }


            MemoryStream stream = ConvertListToMemoryStream(logOutput);
            SaveStreamToFile(stream, SourceDirectory, "uuids_in_azure.csv");
            stream.Dispose();
        }
        #endregion

        #region Helper Methods
        static string ExtractActualFilename(string blobName)
        {
            var fileName = blobName.Replace('\\', '/');
            var lastSlash = fileName.LastIndexOf('/');
            if (lastSlash >= 0) fileName = fileName.Substring(lastSlash + 1);

            var underscore = fileName.IndexOf('_');
            return underscore > 0 ? fileName.Substring(underscore + 1) : fileName;
        }

        static string FolderHash(string normalizedFolder, int hexChars = 16)
        {
            var bytes = Encoding.UTF8.GetBytes(normalizedFolder);
            var hash = SHA256.HashData(bytes);

            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));

            return sb.ToString(0, hexChars);
        }

        private static string GetFolderHash(string folderPath)
        {
            string uniquePart = folderPath.Replace("/content/dam/ashley-furniture/", "");
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(uniquePart);
            byte[] hash = sha256.ComputeHash(bytes);

            return Convert.ToHexString(hash).Substring(0, 16);
        }

        public async Task GetMissingMetadataAsync(CancellationToken cancellationToken)
        {

            string filePath = $"{SourceDirectory}missingMetadata.txt";
            string tableA = "dbo.MappingHelperObjects";
            string tableB= "dbo.AssetMetadata";
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";

            MappingHelperObjectsRepository mhorA = new MappingHelperObjectsRepository(connectionString, tableA);
            MappingHelperObjectsRepository mhorB = new MappingHelperObjectsRepository(connectionString, tableB);


            try
            {
                List<string> lines = new List<string>();
                using (var reader = new StreamReader(filePath))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
                _logger.LogInformation($"Found {lines.Count} missing metadata");
                foreach (var line in lines)
                {


                    string json = mhorA.GetJsonBodyByDictKey(line);
                    MappingHelperObject mho = JsonConvert.DeserializeObject<MappingHelperObject>(json);

                    if (mho != null)
                    {
                        _logger.LogInformation($"Found matching MappingHelperObject");

                        string azureAssetPath = mho.AzureAssetPath;
                        string assetFolder = Path.GetDirectoryName(azureAssetPath);
                        string azureMetadataFilename = $"{line}_metadata.json";
                        bool fileMetadataExists = await _assetsWrapper.BlobExistsAsync($"{azureMetadataFilename}", assetFolder);

                        if (fileMetadataExists)
                        {
                            _logger.LogInformation($"Found the metadata exists : {azureMetadataFilename}");
                            string metadatajson = await ReadJsonFile(azureMetadataFilename, assetFolder);
                            mhorB.UpsertJsonBody(line, metadatajson);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Oops! something bad happened: {ex.Message}");
            }

            return;
        }

        public async Task GetMissingMetadataSpecificAsync(CancellationToken cancellationToken)
        {
            string path = "/content/dam/ashley-furniture/studiophotography/upholstery/700s/73611-16-SIDE-SW.tif";
            string assetFolder = $"deltas2{Path.GetDirectoryName(path)}"; 
            string azureMetadataFilename = $"3ab4980b-05f4-4b63-ace8-9cd780506b95_metadata.json";

            try
            {
                await using var mdStream = await _aemClient.GetMetadataAsync(path);
                await _assetsWrapper.UploadBlobAsync(azureMetadataFilename, mdStream, assetFolder);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Oops! something bad happened: {ex.Message}");
            }

            return;
        }

        public async Task FindDeletedAemAssetsAsync(CancellationToken cancellationToken)
        {
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";

            MappingHelperObjectsRepository mhoFlatsView = new MappingHelperObjectsRepository(connectionString, "dbo.view_FlatsWithDuplicates");

            var allDuplicates = await mhoFlatsView.GetAllFlatsFromTableOrViewAsync(cancellationToken);
            List<string> assetsNoLongerInAEM = new List<string>();
            foreach (var mho in allDuplicates)
            {
                var aemAsset = await _aemClient.GetAssetByUUID(mho.AemAssetId,_logger,cancellationToken);

                if (aemAsset == null)
                {
                    assetsNoLongerInAEM.Add(mho.AemAssetId);
                    ;
                }

            }

            MemoryStream stream = ConvertListToMemoryStream(assetsNoLongerInAEM);
            SaveStreamToFile(stream, SourceDirectory, "assetsNoLongerInAEM.csv");
            stream.Dispose();

            return;
        }

        //public async Task GetImageSetDataTestsAsync(CancellationToken cancellationToken)
        //{
        //    var allImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allImagesetBlobs.csv");
        //    _logger.LogInformation($"Found {allImagesetFiles.Count()} image sets in Azure");

        //    var allDeltaImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allDeltaImagesetBlobs.csv");
        //    _logger.LogInformation($"Found {allDeltaImagesetFiles.Count()} delta image sets in Azure");

        //    string filePath = $"{SourceDirectory}missingRelations.txt";
        //    string tableA = "dbo.ImageSets";
        //    string tableB = "dbo.ImageSetsRelations";
        //    string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";

        //    MappingHelperObjectsRepository mhorA = new MappingHelperObjectsRepository(connectionString, tableA);
        //    MappingHelperObjectsRepository mhorB = new MappingHelperObjectsRepository(connectionString, tableB);
        //    MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjects");


        //    try
        //    {
        //        List<string> lines = new List<string>();
        //        using (var reader = new StreamReader(filePath))
        //        {
        //            string? line;
        //            while ((line = reader.ReadLine()) != null)
        //            {
        //                lines.Add(line);
        //            }
        //        }
        //        _logger.LogInformation($"Found {lines.Count} missing imagesets");

        //        //var lines = mhorB.GetAllDictKeys();
        //        //int missingRelationsCount = 0;
        //        //foreach (var line in lines)
        //        //{


        //        //    string json = mhorB.GetJsonBodyByDictKey(line);
        //        //    AprimoImageSetAssets aisa = JsonConvert.DeserializeObject<AprimoImageSetAssets>(json);

        //        //    if (aisa != null)
        //        //    {
        //        //        if (aisa.Resources.Count() == 0)
        //        //        {
        //        //            missingRelationsCount++;
        //        //        }

        //        //    }

        //        //}
        //        //_logger.LogInformation($"Finished testing ImageSet Resources.  {missingRelationsCount} had 0 Resources");

        //        ///*** FIX ***///
        //        foreach (var line in lines)
        //        {
        //            var origRelated = allImagesetFiles.Where(x => x.Contains(line)).ToList();
        //            var deltaRelated = allDeltaImagesetFiles.Where(x => x.Contains(line)).ToList();

        //            if (deltaRelated.Count() > 0)
        //            {
        //                _logger.LogInformation($"Found {line} in Delta ({deltaRelated.Count()}) at : {deltaRelated}");
        //                var assetFolder = $"{Path.GetDirectoryName(deltaRelated[0])}";
        //                var assetName = $"{Path.GetFileName(deltaRelated[0])}";
        //                string json = await ReadJsonFile(assetName,assetFolder);
        //                AprimoImageSetAssets aisa = JsonConvert.DeserializeObject<AprimoImageSetAssets>(json);

        //                foreach(var resource in aisa.Resources)
        //                {

        //                }

        //            }
        //            else if (origRelated.Count() > 0)
        //            {
        //                _logger.LogInformation($"Found {line} in Orig at : {origRelated}");
        //            } else
        //            {
        //                _logger.LogInformation($"Oops! cannot actually find: {line}");
        //            }

                    

        //        }


        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Oops! something bad happened: {ex.Message}");
        //    }

        //    return;
        //}

        public async Task GetImageSetPreviewsAsync(CancellationToken cancellationToken)
        {
            var allOrigImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allImagesetBlobs.csv");
            _logger.LogInformation($"Found {allOrigImagesetFiles.Count()} image sets in Azure");

            var allDeltaImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allDeltaImagesetBlobs.csv");
            _logger.LogInformation($"Found {allDeltaImagesetFiles.Count()} delta image sets in Azure");

            var allDelta2ImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allDelta2ImagesetBlobs.csv");
            _logger.LogInformation($"Found {allDelta2ImagesetFiles.Count()} delta2 image sets in Azure");

            var allDelta3ImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allDelta3ImagesetBlobs.csv");
            _logger.LogInformation($"Found {allDelta3ImagesetFiles.Count()} delta3 image sets in Azure");

            string renditionsFolder = "imagesetpreviews";

            try
            {
                var allImageSetFiles = allDelta2ImagesetFiles.Concat(allDelta3ImagesetFiles); //allOrigImagesetFiles.Concat(allDeltaImagesetFiles).Concat(allDelta2ImagesetFiles).Concat(allDelta3ImagesetFiles);

                foreach (var blob in allImageSetFiles)
                {
                    bool isOrig = blob.StartsWith("assets\\");

                    string azureISFilename = Path.GetFileName(blob);
                    string azurePath = Path.GetDirectoryName(blob);

                    string[] azureNameParts = azureISFilename.Split('_');
                    string uuid = azureNameParts[0];
                    string ISName = azureISFilename.Replace(uuid + "_", "").Replace("_related.json", "");
                    //  assets/content/dam/ashley-furniture/image_sets/AF48051A874DC71D_TestImageset_related.json
                    string aemPath = azurePath.Replace("\\", "/").Replace("assets/", "/").Replace("deltas/", "/");
                    string aemAssetPath = aemPath + "/" + ISName;
                    string aemISThumbnail = aemAssetPath + "/jcr:content/renditions/cq5dam.thumbnail.319.319.png";
                    string renditionName = uuid + "_" + ISName + "_preview.png";

                    string renditionBlob = $"{renditionsFolder}\\{renditionName}";
                    bool bExists = await _assetsWrapper.BlobExistsAsync(renditionBlob);

                    if (bExists) 
                    {
                        _logger.LogInformation($"Already have {renditionBlob} in Azure");
                    } else
                    {
                        try
                        {
                            await using var renditionStream = await _aemClient.GetRenditionsAsync(aemAssetPath, "cq5dam.thumbnail.319.319.png", cancellationToken);
                            await _assetsWrapper.UploadBlobAsync(renditionName, renditionStream, renditionsFolder);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Could not find rendition for: {aemAssetPath} : {ex.Message}");
                        }
                    }


                    ;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Oops! something bad happened: {ex.Message}");
            }

            return;
        }

        public async Task GetImageSetPreviewsByPathAsync(CancellationToken cancellationToken)
        {
            var allOrigImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allImagesetBlobs.csv");
            _logger.LogInformation($"Found {allOrigImagesetFiles.Count()} image sets in Azure");

            var allDeltaImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allDeltaImagesetBlobs.csv");
            _logger.LogInformation($"Found {allDeltaImagesetFiles.Count()} delta image sets in Azure");

            var missingFiles = File.ReadAllLines($"{SourceDirectory}pathtomissingpreviews.csv");
            _logger.LogInformation($"Found {missingFiles.Count()} missing previews");

            var allDelta2Files = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.Deltas2RootPrefix);
            var allDelta2ImagesetFiles = allDelta2Files.Where(x => x.EndsWith("_related.json")).ToList();

            string renditionsFolder = "imagesetpreviews";

            try
            {
                //var allImageSetFiles = allOrigImagesetFiles.Concat(allDeltaImagesetFiles);
                var allImageSetFiles = allDelta2ImagesetFiles;

                foreach (var blob in allImageSetFiles)
                {
                    bool isOrig = blob.StartsWith("assets\\");

                    string azureISFilename = Path.GetFileName(blob);
                    string azurePath = Path.GetDirectoryName(blob);

                    string[] azureNameParts = azureISFilename.Split('_');
                    string uuid = azureNameParts[0];
                    string ISName = azureISFilename.Replace(uuid + "_", "").Replace("_related.json", "");
                    //  assets/content/dam/ashley-furniture/image_sets/AF48051A874DC71D_TestImageset_related.json
                    string aemPath = azurePath.Replace("\\", "/").Replace("assets/", "/").Replace("deltas/", "/");
                    string aemAssetPath = aemPath + "/" + ISName;
                    string aemISThumbnail = aemAssetPath + "/jcr:content/renditions/cq5dam.thumbnail.319.319.png";
                    string renditionName = uuid + "_" + ISName + "_preview.png";
                    try
                    {
                        await using var renditionStream = await _aemClient.GetRenditionsAsync(aemAssetPath, "cq5dam.thumbnail.319.319.png", cancellationToken);
                        await _assetsWrapper.UploadBlobAsync(renditionName, renditionStream, renditionsFolder);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Could not find rendition for: {aemAssetPath} : {ex.Message}");
                    }

                    ;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Oops! something bad happened: {ex.Message}");
            }

            return;
        }

        public async Task GetMissingImageSetPreviewsByPathAsync(CancellationToken cancellationToken)
        {
            var missingFiles = File.ReadAllLines($"{SourceDirectory}pathtomissingpreviews.csv");
            _logger.LogInformation($"Found {missingFiles.Count()} missing previews");

            string renditionsFolder = "imagesetpreviews";

            try
            {
                var allImageSetFiles = missingFiles;

                foreach (var blob in allImageSetFiles)
                {
                    bool isOrig = blob.StartsWith("assets\\");

                    string azureISFilename = Path.GetFileName(blob);
                    string azurePath = Path.GetDirectoryName(blob);

                    string[] azureNameParts = azureISFilename.Split('_');
                    string uuid = azureNameParts[0];
                    string ISName = azureISFilename.Replace(uuid + "_", "").Replace("_related.json", "");
                    //  assets/content/dam/ashley-furniture/image_sets/AF48051A874DC71D_TestImageset_related.json
                    string aemPath = azurePath.Replace("\\", "/").Replace("assets/", "/").Replace("deltas/", "/");
                    string aemAssetPath = aemPath + "/" + ISName;
                    string aemISThumbnail = aemAssetPath + "/jcr:content/renditions/cq5dam.thumbnail.319.319.png";
                    string renditionName = uuid + "_" + ISName + "_preview.png";
                    try
                    {
                        await using var renditionStream = await _aemClient.GetRenditionsAsync(aemAssetPath, "cq5dam.thumbnail.319.319.png", cancellationToken);
                        await _assetsWrapper.UploadBlobAsync(renditionName, renditionStream, renditionsFolder);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Could not find rendition for: {aemAssetPath} : {ex.Message}");
                    }

                    ;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Oops! something bad happened: {ex.Message}");
            }

            return;
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
                var dataTables = new List<DataTable> { _state.SuccessTable, _state.RetryTable };
                await using var excelStream = ExcelWriter.WriteDataTables(dataTables);
                excelStream.Position = 0;

                string excelFilename = $"{prefix}{timestamp}_{SuccessRetryFilename}";
                await _assetsWrapper.UploadBlobAsync(excelFilename, excelStream, _azureOptions.Value.LogsRootPrefix);

            }
            catch (Exception ex)
            {
                logOutput.Add($"Failed to create success/retry output to Azure Blob Storage. {ex.Message}");
                _logger.LogError(ex, "Failed to log output to Azure Blob Storage.");
            }

            try
            {
                // Upload plain text log
                string logFilename = $"{prefix}{timestamp}_{LogFilename}";
                var logContent = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, logOutput));
                await using (var logStream = new MemoryStream(logContent))
                {
                    await _assetsWrapper.UploadBlobAsync(logFilename, logStream, _azureOptions.Value.LogsRootPrefix);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log output to Azure Blob Storage.");
                return false;
            }
        }

        private async Task ProcessAssetAsync(Dictionary<string, string> rowData, List<string> logOutput)
        {
            var uuid = rowData["Id"];
            var path = rowData["Path"];
            var byteSize = rowData["SizeBytes"];

            // used to include this but we have some binaries that have no UUID:  string.IsNullOrEmpty(uuid) || 
            if (string.IsNullOrEmpty(byteSize) || byteSize == "0")
            {
                logOutput.Add($"Skipping. Not Valid Binary Asset: Id={uuid} | Size={byteSize}");
                LogRowData(true, rowData, $"Skipping. Not Valid Binary Asset: Id={uuid} | Size={byteSize}");
                return;
            }

            // handle empty binaries
            bool bHasHashedUUID = false;
            if (string.IsNullOrEmpty(uuid))
            {
                uuid = GetFolderHash(path);
                bHasHashedUUID = true;
            }

            var fileName = Path.GetFileName(path);
            var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{Path.GetDirectoryName(path)}";
            //var assetMetadataFolder = $"{_azureOptions.Value.MetadataRootPrefix}/{Path.GetDirectoryName(path)}";
            string cleanedFilename = Regex.Replace(fileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();
            string azureFilename = $"{uuid}_{cleanedFilename}";
            string azureMetadataFilename = $"{uuid}_metadata.json";


            //string renditionsFolder = $"{assetFolder}/{uuid}_renditions";

            //string azureJcrContentFilename = $"{uuid}_jcrcontent.json";

            _logger.LogInformation($"Processing {uuid}:{path}");

            try
            {
                bool fileExists = await _assetsWrapper.BlobExistsAsync($"{azureFilename}", assetFolder);
                bool fileMetadataExists = await _assetsWrapper.BlobExistsAsync($"{azureMetadataFilename}", assetFolder);

                if (fileExists && fileMetadataExists)
                {
                    rowData["Id"] = uuid;
                    logOutput.Add($"Skipping. both binary and metadata exist");
                    LogRowData(true, rowData, $"Skipping. both binary and metadata exist");
                    return;
                }
                else
                {
                    //var progress = new Progress<(string BlobName, long BytesUploaded)>(tuple =>
                    //{
                    //    _logger.LogInformation($"Uploading {tuple.BlobName}: {tuple.BytesUploaded} bytes uploaded");
                    //});


                    await using var stream = await _aemClient.GetOriginalAsync(path);
                    await _assetsWrapper.UploadBlobAsync(azureFilename, stream, assetFolder);
                    await using var mdStream = await _aemClient.GetMetadataAsync(path);
                    await _assetsWrapper.UploadBlobAsync(azureMetadataFilename, mdStream, assetFolder);

                    //try
                    //{
                    //    var aemAsset = await _aemClient.GetAssetByUUID(uuid, _logger, CancellationToken.None);
                    //    if (bHasHashedUUID)
                    //    {
                    //        aemAsset = await _aemClient.GetAssetByPath(path, _logger, CancellationToken.None);
                    //    }

                    //    if (aemAsset != null)
                    //    {
                    //        try
                    //        {
                    //            //await using var jcrStream = await _aemClient.GetJcrContentAsync(aemAsset.Path, CancellationToken.None);
                    //            //await _assetsWrapper.UploadBlobAsync(azureJcrContentFilename, jcrStream, assetFolder);

                    //            // process renditions 
                    //            //  don't use this.  sometimes jcrContent doesn't list the renditions but the folder still exists.
                    //            //   additionally, the jcrContent renditions listing contains failed renditions and will cause 404s later
                    //            //string json = await ReadJsonFile(azureJcrContentFilename, assetFolder);
                    //            //var jcrContent = JsonConvert.DeserializeObject<AemJcrContent>(json);

                    //            //var renditionsStream = await _aemClient.GetRenditionsFolderAsync(aemAsset.Path, CancellationToken.None);
                    //            //var renditions = DeserializeFromStream<List<AemRendition>>(renditionsStream);

                    //            //if (jcrContent.DamProcessingRenditions != null)
                    //            //{
                    //            //    logOutput.Add($"found {jcrContent.DamProcessingRenditions.Count} for {uuid}");
                    //            //    foreach (string rendition in jcrContent.DamProcessingRenditions)
                    //            //    {
                    //            //        try
                    //            //        {
                    //            //            await using var renditionStream = await _aemClient.GetRenditionAsync(aemAsset.Path, rendition, cancellationToken);
                    //            //            await _assetsWrapper.UploadBlobAsync(rendition, renditionStream, renditionsFolder);
                    //            //        }
                    //            //        catch (Exception ex)
                    //            //        {
                    //            //            logOutput.Add($"failed to get rendition {rendition} ex: {ex.Message}");
                    //            //        }

                    //            //    }
                    //            //}
                    //            //else
                    //            //{
                    //            //    logOutput.Add($"no renditions found for {uuid}");
                    //            //}

                    //            //if (renditions != null)
                    //            //{
                    //            //    logOutput.Add($"found {renditions.Count} for {uuid}");
                    //            //    foreach (var rendition in renditions)
                    //            //    {
                    //            //        try
                    //            //        {
                    //            //            if (rendition.Id != "original")
                    //            //            {
                    //            //                await using var renditionStream = await _aemClient.GetRenditionsAsync(aemAsset.Path, rendition.Id, CancellationToken.None);
                    //            //                await _assetsWrapper.UploadBlobAsync(rendition.Id, renditionStream, renditionsFolder);
                    //            //            }
                    //            //        }
                    //            //        catch (Exception ex)
                    //            //        {
                    //            //            logOutput.Add($"failed to get rendition {rendition.Id} ex: {ex.Message}");
                    //            //        }

                    //            //    }
                    //            //}
                    //            //else
                    //            //{
                    //            //    logOutput.Add($"no renditions found for {uuid}");
                    //            //}

                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            logOutput.Add($"Error getting renditions for uuid: {uuid}. ex: {ex.Message}");
                    //        }


                    //    }
                    //    else
                    //    {
                    //        logOutput.Add($"Could not find asset for uuid: {uuid}");
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    logOutput.Add($"An error occurred for uuid: {uuid}. ex: {ex.Message}");
                    //}

                    LogRowData(true, rowData, $"Success!");


                }
            }
            catch (Exception e)
            {
                logOutput.Add($"processing failed: Exception {e.Message}");
                LogRowData(false, rowData, $"Processing failed: Exception {e.Message}");
                return;
            }

            return;
        }

        private async Task ProcessAssetAsyncLocal(Dictionary<string, string> rowData, List<string> logOutput)
        {

            long bytesIn2GB = 2L * 1024L * 1024L * 1024L;

            var uuid = rowData["Id"];
            var path = rowData["Path"];
            var byteSize = rowData["SizeBytes"];


            if (string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(byteSize))
            {
                logOutput.Add($"Skipping. Not Valid Binary Asset: Id={uuid} | Size={byteSize}");
                LogRowData(true, rowData, $"Skipping. Not Valid Binary Asset: Id={uuid} | Size={byteSize}");
                return;
            }

            var lngByteSize = long.Parse(byteSize);

            var fileName = Path.GetFileName(path);
            var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{Path.GetDirectoryName(path)}";
            //var assetMetadataFolder = $"{_azureOptions.Value.MetadataRootPrefix}/{Path.GetDirectoryName(path)}";
            string cleanedFilename = Regex.Replace(fileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();
            string azureFilename = $"{uuid}_{cleanedFilename}";
            string azureMetadataFilename = $"{uuid}_metadata.json";

            _logger.LogInformation($"Processing {uuid}:{path}");

            try
            {
                bool fileExists = await _assetsWrapper.BlobExistsAsync($"{azureFilename}", assetFolder);
                bool fileMetadataExists = await _assetsWrapper.BlobExistsAsync($"{azureMetadataFilename}", assetFolder);

                if (fileExists && fileMetadataExists)
                {
                    logOutput.Add($"Skipping. both binary and metadata exist");
                    LogRowData(true, rowData, $"Skipping. both binary and metadata exist");
                    return;
                }
                else
                {
                    //var progress = new Progress<(string BlobName, long BytesUploaded)>(tuple =>
                    //{
                    //    _logger.LogInformation($"Uploading {tuple.BlobName}: {tuple.BytesUploaded} bytes uploaded");
                    //});



                    if (lngByteSize >= bytesIn2GB)
                    {
                        using (Stream fileStream = new FileStream(Dump + fileName, FileMode.Open, FileAccess.Read))
                        {
                            await _assetsWrapper.UploadBlobAsync(azureFilename, fileStream, assetFolder); //, progress: progress
                        }
                    }
                    else
                    {
                        await using var stream = await _aemClient.GetOriginalAsync(path);
                        await _assetsWrapper.UploadBlobAsync(azureFilename, stream, assetFolder);
                    }

                    await using var mdStream = await _aemClient.GetMetadataAsync(path);
                    await _assetsWrapper.UploadBlobAsync(azureMetadataFilename, mdStream, assetFolder);

                    LogRowData(true, rowData, $"Success!");


                }
            }
            catch (Exception e)
            {
                logOutput.Add($"processing failed: Exception {e.Message}");
                LogRowData(false, rowData, $"Processing failed: Exception {e.Message}");
                return;
            }

            return;
        }


        private async Task ProcessImageSetsAsync(Dictionary<string, string> rowData, List<string> logOutput, Dictionary<string, RelatedData> allRelatedData, bool skipAllRelatedData = true)
        {
            var uuid = rowData["Id"];
            var path = rowData["Path"];
            var byteSize = rowData["SizeBytes"];

            if (byteSize != "0")
            {
                logOutput.Add($"Skipping. Not Valid ImageSet");
                return;
            }

            var fileName = Path.GetFileName(path);
            var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{Path.GetDirectoryName(path)}";
            //var assetMetadataFolder = $"{_azureOptions.Value.MetadataRootPrefix}/{Path.GetDirectoryName(path)}";
            string cleanedFilename = Regex.Replace(fileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

            string folderHash = GetFolderHash(Path.GetDirectoryName(path));

            string azureMetadataFilename = $"{folderHash}_{cleanedFilename}_metadata.json";
            string azureRelatedFilename = $"{folderHash}_{cleanedFilename}_related.json";


            _logger.LogInformation($"Processing {path}");

            try
            {
                bool fileExists = await _assetsWrapper.BlobExistsAsync($"{azureRelatedFilename}", assetFolder);
                bool fileMetadataExists = await _assetsWrapper.BlobExistsAsync($"{azureMetadataFilename}", assetFolder);

                if (fileExists && fileMetadataExists)
                {
                    logOutput.Add($"Skipping. both related binary and metadata exist");

                    if (!skipAllRelatedData)
                    {
                        string json = await ReadJsonFile(azureRelatedFilename, assetFolder);
                        var relatedData = JsonConvert.DeserializeObject<RelatedData>(json);
                        rowData["RelatedCounts"] = relatedData.Resources.Count().ToString();
                        rowData["RelatedAssets"] = string.Join(",", relatedData.Resources);

                        allRelatedData.Add($"{path}", relatedData);
                    }

                    LogRowData(true, rowData, $"Skipping. both related and metadata exist");
                    return;
                }
                else
                {
                    //var progress = new Progress<(string BlobName, long BytesUploaded)>(tuple =>
                    //{
                    //    _logger.LogInformation($"Uploading {tuple.BlobName}: {tuple.BytesUploaded} bytes uploaded");
                    //});

                    await using var stream = await _aemClient.GetRelatedAsync(path);
                    await _assetsWrapper.UploadBlobAsync(azureRelatedFilename, stream, assetFolder);

                    if (!skipAllRelatedData)
                    {
                        await using var stream2 = await _aemClient.GetRelatedAsync(path);
                        MemoryStream ms = new MemoryStream();
                        stream2.CopyTo(ms);
                        ms.Position = 0;
                        string relatedJson = Encoding.UTF8.GetString(ms.ToArray());
                        ms.Dispose();
                        var relatedData = JsonConvert.DeserializeObject<RelatedData>(relatedJson);
                        rowData["RelatedCounts"] = relatedData.Resources.Count().ToString();
                        rowData["RelatedAssets"] = string.Join(",", relatedData.Resources);

                        allRelatedData.Add($"{path}", relatedData);
                    }

                    await using var mdStream = await _aemClient.GetMetadataAsync(path);
                    await _assetsWrapper.UploadBlobAsync(azureMetadataFilename, mdStream, assetFolder);


                    LogRowData(true, rowData, $"Success!");
                }
            }
            catch (Exception e)
            {
                logOutput.Add($"processing failed: Exception {e.Message}");
                LogRowData(false, rowData, $"Processing failed: Exception {e.Message}");
                return;
            }

            return;
        }

        private async Task ProcessDeltaAssetAsync(Dictionary<string, string> rowData, List<string> logOutput, string prefix)
        {
            var uuid = rowData["Id"];
            var path = rowData["Path"];
            var byteSize = rowData["SizeBytes"];
            var status = rowData["Status"];
            var binaryChanged = rowData["BinaryChanged"];

            // handle empty binaries
            bool bHasHashedUUID = false;
            if (string.IsNullOrEmpty(uuid))
            {
                uuid = GetFolderHash(path);
                bHasHashedUUID = true;
            }

            var fileName = Path.GetFileName(path);
            var assetFolder = $"{prefix}{Path.GetDirectoryName(path)}";
            //var origAssetFolder = $"{_azureOptions.Value.AssetRootPrefix}{Path.GetDirectoryName(path)}";

            string cleanedFilename = Regex.Replace(fileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();
            string azureFilename = $"{uuid}_{cleanedFilename}";
            string azureMetadataFilename = $"{uuid}_metadata.json";


            _logger.LogInformation($"Processing {uuid}:{path}");

            try
            {
                bool fileExists = await _assetsWrapper.BlobExistsAsync($"{azureFilename}", assetFolder);
                bool fileMetadataExists = await _assetsWrapper.BlobExistsAsync($"{azureMetadataFilename}", assetFolder);

                if ((status.Equals("New") & fileExists && fileMetadataExists) || (status.Equals("Needs Update") && fileMetadataExists) || status.Contains("Already"))
                {
                    rowData["Id"] = uuid;
                    logOutput.Add($"Skipping. both binary and metadata exist");
                    LogRowData(true, rowData, $"Skipping. both binary and metadata exist");
                    return;
                }
                else
                {
                    //var progress = new Progress<(string BlobName, long BytesUploaded)>(tuple =>
                    //{
                    //    _logger.LogInformation($"Uploading {tuple.BlobName}: {tuple.BytesUploaded} bytes uploaded");
                    //});

                    if (status.Equals("New") || binaryChanged.Equals("True"))
                    {
                        await using var stream = await _aemClient.GetOriginalAsync(path);
                        await _assetsWrapper.UploadBlobAsync(azureFilename, stream, assetFolder);
                    }

                    await using var mdStream = await _aemClient.GetMetadataAsync(path);
                    await _assetsWrapper.UploadBlobAsync(azureMetadataFilename, mdStream, assetFolder);


                    LogRowData(true, rowData, $"Success!");


                }
            }
            catch (Exception e)
            {
                logOutput.Add($"processing failed: Exception {e.Message}");
                LogRowData(false, rowData, $"Processing failed: Exception {e.Message}");
                return;
            }

            return;
        }

        private async Task ProcessDeltaImageSetsAsync(Dictionary<string, string> rowData, List<string> logOutput, string prefix)
        {
            var uuid = rowData["Id"];
            var path = rowData["Path"];
            var byteSize = rowData["SizeBytes"];
            var status = rowData["Status"];
            var binaryChanged = rowData["BinaryChanged"];

            var fileName = Path.GetFileName(path);
            var assetFolder = $"{prefix}{Path.GetDirectoryName(path)}";
            string cleanedFilename = Regex.Replace(fileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

            string folderHash = GetFolderHash(Path.GetDirectoryName(path));

            string azureMetadataFilename = $"{folderHash}_{cleanedFilename}_metadata.json";
            string azureRelatedFilename = $"{folderHash}_{cleanedFilename}_related.json";

            _logger.LogInformation($"Processing {path}");

            try
            {
                bool fileExists = await _assetsWrapper.BlobExistsAsync($"{azureRelatedFilename}", assetFolder);
                bool fileMetadataExists = await _assetsWrapper.BlobExistsAsync($"{azureMetadataFilename}", assetFolder);

                if (fileExists && fileMetadataExists)
                {
                    logOutput.Add($"Skipping. both related binary and metadata exist");
                    LogRowData(true, rowData, $"Skipping. both related and metadata exist");
                    return;
                }
                else
                {
                    //var progress = new Progress<(string BlobName, long BytesUploaded)>(tuple =>
                    //{
                    //    _logger.LogInformation($"Uploading {tuple.BlobName}: {tuple.BytesUploaded} bytes uploaded");
                    //});

                    await using var stream = await _aemClient.GetRelatedAsync(path);
                    await _assetsWrapper.UploadBlobAsync(azureRelatedFilename, stream, assetFolder);

                    await using var mdStream = await _aemClient.GetMetadataAsync(path);
                    await _assetsWrapper.UploadBlobAsync(azureMetadataFilename, mdStream, assetFolder);


                    LogRowData(true, rowData, $"Success!");
                }
            }
            catch (Exception e)
            {
                logOutput.Add($"processing failed: Exception {e.Message}");
                LogRowData(false, rowData, $"Processing failed: Exception {e.Message}");
                return;
            }

            return;
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

        private void SaveRowDataJson()
        {
            string jsonStringSuccess = JsonConvert.SerializeObject(_state.Successes);
            string jsonStringRetry = JsonConvert.SerializeObject(_state.Failures);

            string filePathSuccess = $"{SourceDirectory}success.json";
            string filePathRetry = $"{SourceDirectory}retry.json";
            File.WriteAllText(filePathSuccess, jsonStringSuccess);
            File.WriteAllText(filePathRetry, jsonStringRetry);
        }

        private static string ToTagSafeName(string input)
        {
            // Practical approach: normalize unicode + strip/replace anything sketchy
            // Keep it stable, deterministic.
            var normalized = input.Normalize(NormalizationForm.FormKC);
            var sb = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                // allow alnum and a few safe separators
                if (char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '_')
                    sb.Append(ch);
                else
                    sb.Append('_'); // replace everything else
            }

            return sb.ToString();
        }
        #endregion

        #region Tests
        public static bool IsBlobUpdated(DateTimeOffset blobLastModified, string comparisonString)
        {
            if (string.IsNullOrWhiteSpace(comparisonString))
                return false;

            // Convert "GMT+0000" → "+0000" so .NET can parse it cleanly
            var cleaned = comparisonString.Replace("GMT", "").Trim();

            // Parse to DateTimeOffset
            var comparisonDate = DateTimeOffset.ParseExact(
                cleaned,
                "ddd MMM dd yyyy HH:mm:ss zzz",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal
            );

            return blobLastModified > comparisonDate;
        }
        #endregion
    }
}
