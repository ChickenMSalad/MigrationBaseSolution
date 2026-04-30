using Migration.Shared.Workflows.AemToAprimo.Models;
using Migration.Connectors.Targets.Aprimo.Clients;
using Migration.Domain.Models;
using Migration.Connectors.Targets.Aprimo.Extensions;
using Migration.Shared.Storage;
using Migration.Connectors.Targets.Aprimo.Configuration;
using Migration.Shared.Configuration.Hosts.Aprimo;
using Migration.Shared.Configuration.Infrastructure;
using Migration.Connectors.Targets.Aprimo.Files;
using Migration.Connectors.Targets.Aprimo.Models;
using Migration.Connectors.Targets.Aprimo.Models.Aprimo;
using Migration.Connectors.Targets.Aprimo.Utilities;
using Migration.Manifest.Sql.Repositories;
using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static OfficeOpenXml.ExcelErrorValue;



namespace Migration.Connectors.Targets.Aprimo.Services
{
    public class AprimoDataMigrationService
    {
        #region Fields
        private readonly ILogger<AprimoDataMigrationService> _logger;
        private readonly IOptions<AprimoOptions> _aprimoOptions;
        private readonly IOptions<AzurePathOptions> _azureOptions;
        private readonly IOptions<ExportOptions> _exportOptions;
        private readonly IAprimoAssetClient _aprimoClient;
        private readonly IAzureBlobWrapperFactory _azureFactory;
        private readonly ExecutionContextState _state;
        private readonly BlobServiceClient _blobServiceClient;

        private string Dump;
        private string SourceDirectory;
        private string ImportsSourceDirectory;
        private string SuccessRetryFilename;
        private string LogFilename;
        private string BlenderExecutablePath;
        private string BlenderThumbnailScriptPath;

        private static readonly Regex MetadataFileRegex = new Regex(
            @"^[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}_metadata\.json$",
            RegexOptions.Compiled);

        private AzureBlobWrapperAsync _assetsWrapper;
        private AzureBlobWrapperAsync _jobsWrapper;

        private bool hasPrimedRecord = false;

        private string locale;
        private string languageId;
        #endregion

        #region Constructors
        public AprimoDataMigrationService(
            BlobServiceClient blobServiceClient,
            ExecutionContextState state,
            ILogger<AprimoDataMigrationService> logger,
            IOptions<AprimoOptions> aprimoOptions,
            IOptions<ExportOptions> exportOptions,
            IOptions<AzurePathOptions> azureOptions,
            IOptions<AprimoHostOptions> hostOptions,
            IAprimoAssetClient aprimoClient,
            IAzureBlobWrapperFactory azureFactory)
        {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aprimoOptions = aprimoOptions ?? throw new ArgumentNullException(nameof(aprimoOptions));
            _azureOptions = azureOptions ?? throw new ArgumentNullException(nameof(azureOptions));
            _exportOptions = exportOptions ?? throw new ArgumentNullException(nameof(exportOptions));
            _aprimoClient = aprimoClient ?? throw new ArgumentNullException(nameof(aprimoClient));
            _azureFactory = azureFactory ?? throw new ArgumentNullException(nameof(azureFactory));
            _assetsWrapper = _azureFactory.Get("assets");
            _jobsWrapper = _azureFactory.Get("jobs");
            _state = state;
            _blobServiceClient = blobServiceClient;

            var paths   = hostOptions?.Value.Paths;
            var files   = hostOptions?.Value.Files;
            var runtime = hostOptions?.Value.Runtime;
            var tools   = hostOptions?.Value.Tools;

            Dump                       = paths?.DumpDirectory           ?? @"C:\Workspace\Dump\";
            SourceDirectory            = paths?.SourceDirectory          ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Ntara", "Ashley Migration") + Path.DirectorySeparatorChar;
            ImportsSourceDirectory     = paths?.ImportsSourceDirectory   ?? Path.Combine(SourceDirectory, "imports") + Path.DirectorySeparatorChar;
            SuccessRetryFilename       = files?.SuccessRetryFilename     ?? "successRetryMetadata.xlsx";
            LogFilename                = files?.LogFilename              ?? "aprimoMigration.log";
            locale                     = runtime?.Locale                 ?? "en-US";
            languageId                 = runtime?.LanguageId > 0
                                             ? runtime.LanguageId.ToString()
                                             : "00000000000000000000000000000000";
            BlenderExecutablePath      = tools?.BlenderExecutablePath    ?? @"C:\Program Files\Blender Foundation\Blender 5.0\blender.exe";
            BlenderThumbnailScriptPath = tools?.BlenderThumbnailScriptPath ?? @"C:\Workspace\Ashley\src\Ashley.Core\Python\render_glb_thumbnail.py";
        }
        #endregion

        #region TESTS
        public async Task Tests(CancellationToken cancellationToken)
        {

            var definitions = await _aprimoClient.GetAllDefinitionsAsync(cancellationToken);
            _logger.LogInformation($"found {definitions.Count()} definitions");

            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {definitions.Count()} classifications");

            string defId = "118c9854-e1db-4cee-a256-b3ee018409ba";

            var def = definitions.Where(d => d.Id == defId).FirstOrDefault();


            ;



            ResetState();

            var logOutput = new List<string>();

            // get everything so we can do lookups
            var allDeltaFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.DeltasRootPrefix);
            var allDeltaAssetFiles = allDeltaFiles.Where(x => !x.EndsWith(".json") && !x.Contains("_renditions/")).ToList();
            MemoryStream stream = ConvertListToMemoryStream(allDeltaAssetFiles);
            SaveStreamToFile(stream, SourceDirectory, "allDeltaAssetBlobs.csv");
            stream.Dispose();
            _logger.LogInformation($"Found {allDeltaAssetFiles.Count()} Delta Assets in Azure now");
            ;


            var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);

            //var allImagesetFiles = allFiles.Where(x => x.EndsWith("_related.json")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allImagesetFiles);
            //SaveStreamToFile(stream, SourceDirectory, "allImagesetBlobs.csv");
            //stream.Dispose();

            ;

            //var allAssetFiles2 = allFiles.Where(x => !x.EndsWith(".json") && !x.Contains("_renditions/")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allAssetFiles2);
            //SaveStreamToFile(stream, SourceDirectory, "allAssetBlobs2.csv");
            //stream.Dispose();
            //_logger.LogInformation($"Found {allAssetFiles2.Count()} assets in Azure now");

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


            using var reader = new StreamReader($"{SourceDirectory}\\AllAprimoAssets.csv");
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            });
            int dupeCount = 0;
            // Read header once
            csv.Read();
            csv.ReadHeader();
            Dictionary<string, string> aemToAprimoId = new Dictionary<string, string>();
            while (csv.Read())
            {
                var col2 = csv.GetField(1).Trim().Replace("?", ""); // second column (0-based)
                var col3 = csv.GetField(2).Trim(); // third column

                if (!aemToAprimoId.ContainsKey(col2))
                {
                    aemToAprimoId.Add(col2, col3);
                }
                else
                {
                    dupeCount++;
                    _logger.LogInformation($"duplicate {dupeCount}");
                }


            }

            //var allAssetFiles3 = allAssetFiles2.Where(x => !x.Contains("_renditions/")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allAssetFiles3);
            //SaveStreamToFile(stream, SourceDirectory, "allAssetBlobs3.csv");
            //stream.Dispose();

            //string relatedAsset = "/content/dam/ashley-furniture/webimages/knockout-images/p1-ko_upholstery/p1-ko_700s/77412-65-SIDE-ALT-SW-P1-KO.tif";
            //var azureFile = allAssetFiles.Where(f => ContainsPathIgnoringGuid(f, relatedAsset)).FirstOrDefault();
            //var azureFileTest = allAssetFiles.Where(f => f.IndexOf("/content/dam/ashley-furniture/webimages/knockout-images/p1-ko_upholstery/p1-ko_700s") > 0).FirstOrDefault();
            //a8e6dd00 - ca8a - 41f9 - 885a - 3a6a984cfb89

            //var azureFile3 = allAssetFiles3.Where(f => ContainsPathIgnoringGuid(f, relatedAsset)).FirstOrDefault();
            //var azureFileTest3 = allAssetFiles3.Where(f => f.IndexOf("a8e6dd00-ca8a-41f9-885a-3a6a984cfb89") > 0).FirstOrDefault();

            Dictionary<string, MappingHelperObject> prodMappings = new Dictionary<string, MappingHelperObject>();
            foreach (var azureFilePath in allAssetFilesMinusKO)
            {
                string azureFileName = Path.GetFileName(azureFilePath);
                string[] azureFileNameParts = azureFileName.Split("_");
                string aemUUID = azureFileNameParts[0];
                string azureCleanName = azureFileName.Replace(aemUUID + "_", "");
                MappingHelperObject mho = new MappingHelperObject();

                mho.AemAssetId = aemUUID;
                //mho.AemAssetName = ""; // TODO
                //mho.AemAssetPath
                //mho.AemCreatedDate

                mho.AzureAssetPath = azureFilePath;
                mho.AzureAssetName = azureCleanName;

                if (aemToAprimoId.ContainsKey(aemUUID))
                {
                    mho.AprimoId = aemToAprimoId[aemUUID];
                }
                else
                {
                    try
                    {

                        var aprimoRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(aemUUID, cancellationToken);

                        if (aprimoRecord != null)
                        {
                            mho.AprimoId = aprimoRecord.Id;
                        }


                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"Ph mnes!");
                        logOutput.Add($"dumb ass shit happend: {ex.Message}");
                    }
                }


                if (!prodMappings.ContainsKey(aemUUID))
                {
                    prodMappings.Add(aemUUID, mho);
                }



            }


            // Serialize to a JSON string 
            string jsonString = JsonConvert.SerializeObject(prodMappings, Formatting.None);
            File.WriteAllText($"{Dump}initialProdMappings.json", jsonString);


        }
        public async Task<string> FindDuplicatesInAprimo(CancellationToken cancellationToken)
        {

            //var allAssets = await _aprimoClient.GetAllAssetsAsync();

            //var duplicateGroups =
            //    allAssets
            //    .Where(r => !string.IsNullOrWhiteSpace(r.GetSingleValue("productsAEMAssetID")))
            //    .GroupBy(r => r.GetSingleValue("productsAEMAssetID"), StringComparer.OrdinalIgnoreCase)
            //    .Where(g => g.Count() > 1)
            //    .Select(g => new
            //    {
            //        AemAssetId = g.Key,
            //        DuplicateRecords = g.Count()
            //        //Records = g.ToList()
            //    })
            //    .ToList();

            List<string> allAssets = await _aprimoClient.GetAllAssetIdsAsync();

            MemoryStream stream = ConvertListToMemoryStream(allAssets);
            SaveStreamToFile(stream, SourceDirectory, "all_assets_in_aprimo_prod.csv");
            stream.Dispose();

            var duplicateGroups =
                allAssets
                .Where(r => !string.IsNullOrWhiteSpace(r.Split(",")[0]))
                .GroupBy(r => r.Split(",")[0], StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    AemAssetId = g.Key,
                    DuplicateRecords = g.Count()
                })
                .ToList();


            Console.WriteLine($"Retrieved {allAssets.Count} assets. with {duplicateGroups.Count} duplicates");

            string jsonString = JsonConvert.SerializeObject(duplicateGroups, Formatting.Indented);

            string filePath = $"{SourceDirectory}duplicates_in_aprimo_prod.json";
            File.WriteAllText(filePath, jsonString);

            Console.WriteLine($"Successfully wrote JSON data to {filePath}");


            ;
            return string.Empty;
        }

        public async Task FindBadParenthesesAemAssetsAsync(CancellationToken cancellationToken)
        {
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";

            MappingHelperObjectsRepository mhoFlatsView = new MappingHelperObjectsRepository(connectionString, "dbo.view_allAssetsWithParentheses");

            var allDuplicates = await mhoFlatsView.GetAllFlatsFromParenthesesViewAsync(cancellationToken);
            List<string> assetsMissingParenAprimo = new List<string>();
            foreach (var mho in allDuplicates)
            {
                try
                {
                    var aprimoAsset = await _aprimoClient.GetAssetByAprimoIdAsync(mho.AprimoId, cancellationToken);

                    if (aprimoAsset != null)
                    {
                        var currentValues = GetLocalizedValuesForFieldName(aprimoAsset, "DisplayTitle");
                        string displayTitle = currentValues[0];

                        if (!displayTitle.Contains('('))
                        {
                            assetsMissingParenAprimo.Add(aprimoAsset.Id + "," + aprimoAsset.Title + "," + mho.AemAssetId + "," + mho.AemAssetPath);
                        }
                        ;
                    }
                } catch (Exception e)
                {
                    _logger.LogInformation($"Error: {e.Message}");
                }


            }
            _logger.LogInformation($"Found {assetsMissingParenAprimo.Count} assets in Aprimo missing Parentheses");
            MemoryStream stream = ConvertListToMemoryStream(assetsMissingParenAprimo);
            SaveStreamToFile(stream, SourceDirectory, "assetsMissingParenAprimo.csv");
            stream.Dispose();

            return;
        }

        public async Task FixBadParenthesesAemAssetsAsync(CancellationToken cancellationToken)
        {

            //testing sb2
            //var aprimoAsset = await _aprimoClient.GetAssetByAprimoIdAsync("cab5ba360e22414cbfe2b3ea001788d5", cancellationToken);
            //if (aprimoAsset != null)
            //{
            //    string updatedFileName = "80505-20-14-10X8-CROP-(Test)-dean.tif";
            //    string rrid = "38c1b123-1830-4203-b36c-b3f7014a8578";
            //    string noid = "5ca3bb19-3b20-4ac9-b9db-b3f7014ac623";
            //    var dl = await _aprimoClient.RestampMasterFileNameAsync(aprimoAsset.Id, updatedFileName, rrid, noid, cancellationToken);

            //    ;
            //}


            string rrid = "cdb6efe1-c9a8-4596-8f37-b40d01101eb0";
            string noid = "6e6bd810-8a64-458c-8f9a-b40d01107118";
            var allBadFiles = File.ReadAllLines($"{SourceDirectory}\\assetsMissingParenAprimo.csv");

            foreach (var badFile in allBadFiles.Skip(2))
            {
                string[] parts = badFile.Split(',');
                string aprimoId = parts[0];
                string aemId = parts[2];
                string aemPath = parts[3];
                string updatedFileName = Path.GetFileName(aemPath);
                try
                {
                    var aprimoAsset = await _aprimoClient.GetAssetByAprimoIdAsync(aprimoId, cancellationToken);
                    if (aprimoAsset != null)
                    {
                        _logger.LogInformation($"Updating AprimoId {aprimoId} filename from {aprimoAsset.Embedded.MasterfileLatestVersion.FileName} to {updatedFileName}");

                        var dl = await _aprimoClient.RestampMasterFileNameAsync(aprimoAsset.Id, updatedFileName, rrid, noid, cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"Error Updating AprimoId {aprimoId} filename. Err: {e.Message}");
                }


            }
        }

        public async Task SearchAprimo(CancellationToken cancellationToken)
        {

            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository mhoFlatRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjectsFlat");

            List<string> output = new List<string>();
            string searchExp = "FieldName('DisplayTitle') = '*X*' AND FieldName('productsAEMAssetID') != ''";
            string header = "AemId,AprimoId,DisplayTitle,MasterFileName";
            var searchResults = await _aprimoClient.GetAssetsBySearchAsync(searchExp, cancellationToken);
            output.Add(header);
            foreach (var result in searchResults)
            {
                var mho = await mhoFlatRepo.GetByAprimoIdAsync(result.Id);

                var currentTitleValues = GetLocalizedValuesForFieldName(result, "DisplayTitle");
                string displayTitle = currentTitleValues[0];
                string aemId = string.Empty;
                try
                {
                    var currentAEMIDValues = GetLocalizedValuesForFieldName(result, "productsAEMAssetID");
                    aemId = currentAEMIDValues[0];
                } catch (Exception ex)
                {
                    _logger.LogInformation($"Error: {ex.Message}");
                }

                if (string.IsNullOrEmpty(aemId))
                {
                    aemId = mho.AemAssetId;
                } else
                {
                    if (aemId != mho.AemAssetId)
                    {
                        _logger.LogInformation($"whoa: aemId was expected to be {mho.AemAssetId}");
                    }
                }



                string rowData = $"{aemId},{result.Id},{displayTitle},{result.Embedded.MasterfileLatestVersion.FileName}";

                
                output.Add(rowData);
            }

            _logger.LogInformation($"Found {output.Count - 1} assets from search {searchExp}");
            MemoryStream stream = ConvertListToMemoryStream(output);
            SaveStreamToFile(stream, SourceDirectory, "assetsContainingX.csv");
            stream.Dispose();
        }
        public async Task TestAprimoEndpoint(CancellationToken cancellationToken)
        {
            var baseUri = new Uri("https://ashleyfurniture.aprimo.com");
            var token = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjY0NzVERjZDODQ1NjJDMzQwMEM2ODUyRDlEOEMzMDU1NzA0RDg1NTBSUzI1NiIsIng1dCI6IlpIWGZiSVJXTERRQXhvVXRuWXd3VlhCTmhWQSIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL2FzaGxleWZ1cm5pdHVyZS5hcHJpbW8uY29tL2xvZ2luIiwibmJmIjoxNzc1MTQ1OTgyLCJpYXQiOjE3NzUxNDU5ODIsImV4cCI6MTc3NTE0OTU4MiwiYXVkIjpbImFwaSIsImludHJvc3BlY3Rpb24iLCJodHRwczovL2FzaGxleWZ1cm5pdHVyZS5hcHJpbW8uY29tL2xvZ2luL3Jlc291cmNlcyJdLCJzY29wZSI6WyJhcGkiXSwiY2xpZW50X2lkIjoiTzRKOVZYR1gtNU1WNyIsInN1YiI6IjIxOSIsImF1dGhfdGltZSI6MTc3NTE0NTk4MiwiZW1haWwiOiJkdGF5bG9yQG50YXJhLmNvbSIsImhvc3QiOiJhc2hsZXlmdXJuaXR1cmUuYXByaW1vLmNvbSIsIkRTTiI6ImFzaGxleWZ1cm5pdHVyZS0wMDEiLCJDSUQiOiIxIiwiREIiOiIyIiwiRG9tYWluSWQiOiIxIiwiZGFtLXRlbmFudCI6ImFzaGxleWZ1cm5pdHVyZSIsIkdNIjoiOSwyMCIsIlVzZXJSaWdodHMiOiIxLDUsNiw3LDgsOSwxMywxNSwxNiwxNywxOSwyMCwyMiwyMywyNSwyNiwyOCwyOSwzNywzOCw1MCw1MSw1NCw1NSw1OSw2MCw2Nyw3Miw3Niw4Nyw4OCw5Myw5NCw5OCw5OSwxMDAsMTAxLDEwMiwxMTgsMTE5LDEyOSwxMzAsMTMxLDEzMywxNDAsMTQxLDE0MiwxNDMsMTQ5LDE1MSwxNTIsMTU3LDE1OCwxNTksMTYwLDE2MSwxNjIsMTYzLDE2NCwxNjUsMTY2LDE2NywxNzAsMTcxLDE3NSwxNzgsMTc5LDE4MCwxODEsMTgyLDE4MywxODQsMTg1LDE4NywxODgsMTg5LDIzMCwzMDAsMzAxLDMwMiwzMDMsMzA0LDMwNiwzMDcsMzA4LDMwOSwzMTAsMzExLDMxMiwzMTMsMzE0LDMxNSwzMTYsMzE3LDMxOCwzMTksMzIwLDMyMSwzMjQsMzI1LDMyNywzMjgsMzI5LDMzMCwzMzEsMzMyLDMzMywzMzUsMzM2LDMzNywzMzgsMzM5LDM0MCwzNDEsMzQ0LDM0NSwzNDYsMzQ3LDM0OCwzNDksMzUwLDM1MSwzNTIsMzUzLDM1NCw2MzUsNjM2LDYzNyw2MzgsNjM5LDY0Miw2NDMsNjQ2LDY1MCw2NTEsNjUyLDY1Myw2NTQsNjU2LDY1Nyw2NTgsNjU5LDY2MCw2NjEsNjYyLDY2Myw2NjQsNjY1LDY2Niw2NjcsNjY4LDY2OSw2NzAsNjcxLDY3Miw2NzMsNjc0LDY3NSw2NzcsNjc4LDY3OSw2ODAsNjgxLDY4Miw2ODMsNjg0LDY5MCw2OTEsNjkyLDY5Myw2OTQsNjk1LDY5Niw2OTgsNjk5LDcwMCw3MDEsNzAyLDcwMyw3MDQsNzA1LDcwNiw3MDcsNzA4LDcwOSw3MTAsNzExLDcxMiw3MTMsNzE0LDcxNSw3MTYsNzE3LDcxOCw3MTksNzIwLDcyMSw3MjIsNzI0LDcyNSw3MjYsNzI3LDcyOCw3MjksNzMwLDczNCw3MzYsNzM3LDczOCw3MzksNzQwLDc0Miw3NDMsNzQ0LDc0NSw3NDYsNzQ3LDc0OCw3NTAsNzUxLDc1Miw3NTMsNzU0LDc1NSw3NTYsNzU3LDc1OCw3NTksNzYwLDc2MSw3NjMsMTAwNiwxMjAwLDEyMDEsMTIwMiwxMjAzLDEyMDQsMTIwNiwxMjA3IiwidXNlcm5hbWUiOiJkdGF5bG9yQG50YXJhLmNvbSIsImRhbS1sb2dpbiI6ImR0YXlsb3JAbnRhcmEuY29tIiwiVUlEIjoiMjE5IiwiZGFtLXVpZCI6IjlmODYzZmQ1LTU4ZmItNDcwYi1hZTk5LWIzYmEwMDEzZTY2NiIsIlRJRCI6IjQzIiwiTElEIjoiMSIsIlVUIjoiMSIsIkxvY2FsZSI6ImVuLVVTIiwiZmFtaWx5X25hbWUiOiJUYXlsb3IiLCJnaXZlbl9uYW1lIjoiRGVhbiIsInZlciI6IjIuMC4wIiwianRpIjoiNDdDN0JGQTgyQkM5OURERkY3ODQxNjFGN0JDNEQ2RjMifQ.qvLjpybPqg2kc4E3Rlahf620GNh0TxjrYjE31N9hQKJ-aBj-N2QufyFaffAN0SwIZZn7biiJRG15OtCFZnuCHQhitNqdjBTQWIGlKqJ5kbt0z3qxsuUUiK3ohILqf_Hh50NhHj4ndhZ7OHiGvYc79EQ0VewqoIs5kZJFn7eic9tMG02-Bl0zAyHdIV7xLcwIX8J31Br0Gajb15n12O5z9SrSI0DkS_AMLHIPPWbRSHNXWo2q4Ja5pPIWPF2K-Sp2dqtWrG4Glb86ZMwys1NLhPmAhmFWaP-L8AeuJnkpp-ZGXKRn6BroD_foXTSAWOdeIOrzpn51AdiCsmUSEfgkhw";

            using var http = new HttpClient();

            var methods = await AprimoEndpointInspector.GetAllowedMethodsAsync(
                http,
                baseUri,
                token,
                "/api/notifications");

            Console.WriteLine(methods.Length == 0
                ? "No Allow header returned."
                : $"Allowed methods: {string.Join(", ", methods)}");


            //using var http2 = new HttpClient();
            //await AprimoMethodProbe.ProbeGetAsync(
            //    http2,
            //    baseUri,
            //    token,
            //    "/api/notifications");


            using var http3 = new HttpClient();

            await AprimoEndpointProbe.ProbeAsync(
                http3, baseUri, token, HttpMethod.Get, "/api/notifications");

            await AprimoEndpointProbe.ProbeAsync(
                http3, baseUri, token, HttpMethod.Options, "/api/notifications");

            await AprimoEndpointProbe.ProbeAsync(
                http3, baseUri, token, HttpMethod.Post, "/api/notifications");

            await AprimoEndpointProbe.ProbeAsync(
                http3, baseUri, token, HttpMethod.Post, "/api/notifications", "{}");

            await AprimoEndpointProbe.ProbeAsync(
                http3, baseUri, token, HttpMethod.Post, "/api/notifications/search");

            await AprimoEndpointProbe.ProbeAsync(
                http3, baseUri, token, HttpMethod.Post, "/api/notifications/search", "{}");

            await AprimoEndpointProbe.ProbeAsync(
                http3, baseUri, token, HttpMethod.Post, "/api/notifications/query");

            await AprimoEndpointProbe.ProbeAsync(
                http3, baseUri, token, HttpMethod.Post, "/api/notifications/query", "{}");


        }

        public async Task<string> TestImportBinariesToAprimo(CancellationToken cancellationToken)
        {
            var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            var allAssetFiles = allFiles.Where(x => !x.EndsWith(".json")).ToList();
            List<string> allAssetFilesWithUUID = new List<string>();
            foreach (var file in allAssetFiles)
            {
                string fileName = Path.GetFileName(file);
                string[] parts = fileName.Split('_');
                string output = $"{parts[0]},{file}";
                allAssetFilesWithUUID.Add(output);
            }
            MemoryStream stream = ConvertListToMemoryStream(allAssetFilesWithUUID);
            SaveStreamToFile(stream, SourceDirectory, "allAssetBlobsWithUUID.csv");
            stream.Dispose();

            //var allAssets = await _aprimoClient.GetAllAssetIdsAsync();

            //Console.WriteLine($"Retrieved {allAssets.Count} assets.");

            //MemoryStream stream = ConvertListToMemoryStream(allAssets);
            //SaveStreamToFile(stream, SourceDirectory, "allAprimoAssets.csv");
            //stream.Dispose();

            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");



            //var test1 =  await _aprimoClient.AssetExistsAsync("123456", cancellationToken);
            ;

            //var asset = await _aprimoClient.FindAssetByAEMAssetIdAsync("123456");

            //if (asset != null)
            //{
            //    Console.WriteLine($"Found Aprimo asset: {asset.Id}");
            //}

            var test1 = await _aprimoClient.GetAssetsByAemAssetIdAsync("8ad27ad7-8dac-4fa2-a12a-66b856751707", cancellationToken);
            Console.WriteLine($"Found Aprimo asset: {test1.Count} assets with Aem Asset Id 8ad27ad7-8dac-4fa2-a12a-66b856751707");
            ;


            //var test2 = await _aprimoClient.GetAssetByAprimoIdAsync("3c2658920c0f46fa8cd6b3c4013a4331", cancellationToken);
            //Console.WriteLine($"Found Aprimo asset: {test2.Id} with Aem Asset Id {test2.GetSingleValue("productsAEMAssetID")}");

            var test3 = await _aprimoClient.GetAssetByAprimoIdAsync("94287fd379ff4fbb902db3a5012d7d86", cancellationToken);
            Console.WriteLine($"Found Aprimo asset: {test3.Id} with Aem Asset Id {test3.GetSingleValue("productsAEMAssetID")}");

            if (FieldHasMoreThanOneLocalizedValue(test3, "AssetType"))
            {
                var inlineProductImagery = classifications.Values
                .FirstOrDefault(c =>
                    c.Labels.Any(l =>
                        string.Equals(l.Value, "Inline Product Imagery",
                                      StringComparison.OrdinalIgnoreCase)));

                string classificationId = inlineProductImagery.Id;
            }


    ;
            return string.Empty;
        }

        public async Task TestGLB(CancellationToken cancellationToken)
        {
            string fileName = "A4000225-3D";
            using var glbStream = File.OpenRead($"{SourceDirectory}{fileName}.glb");
            string recordId = "21f3fc7e085748eb98d8b3c4014699cd";
            //var images = GlbPreviewExtractor.ExtractImagesFromGlbStream(glbStream);
            //var preview = GlbPreviewExtractor.PickBestPreview(images);

            //if (preview == null)
            //{
            //    // No embedded images – you may need to render a thumbnail via a renderer instead.
            //    ;
            //}
            //else
            //{
            //    File.WriteAllBytes($"{SourceDirectory}{preview.FileName}", preview.Bytes);
            //}

            string blenderExePath = BlenderExecutablePath;
            string blenderScriptPath = BlenderThumbnailScriptPath;
            //var pngBytes = await GlbThumbnailRenderer.RenderGlbThumbnailAsync(glbStream, blenderExePath,blenderScriptPath, 1024, cancellationToken);

            try
            {
                var pngBytes = await GlbThumbnailRenderer.RenderGlbThumbnailAsync(glbStream, blenderExePath, blenderScriptPath, 1024, cancellationToken);
                using var pngStream = new MemoryStream(pngBytes);
                await _aprimoClient.UploadFileToRecordAsync(
                    recordId: recordId,
                    fileStream: pngStream,
                    fileName: $"{fileName}_preview.png",
                    contentType: "image/png",
                    setAsPreview: true,
                    cancellationToken);

                //File.WriteAllBytes($"{SourceDirectory}test_thumb.png", pngBytes);
            }
            catch (GlbThumbnailRenderer.BlenderRenderException ex)
            {
                Console.WriteLine("TEMP DIR: " + ex.TempDir);
                Console.WriteLine("CMD: " + ex.CommandLine);
                Console.WriteLine("EXIT: " + ex.ExitCode);
                Console.WriteLine("STDOUT:\n" + ex.StdOut);
                Console.WriteLine("STDERR:\n" + ex.StdErr);

                // Now you can open ex.TempDir and see if model.glb exists, etc.
                throw;
            }


    ;
            //var model = ModelRoot.ReadGLB(fs);

            //// Access scene info
            ////var scenes = model.Scenes;
            //var meshes = model.LogicalMeshes;
            //var materials = model.LogicalMaterials;
            //var images = model.LogicalImages;

            //// Save embedded images
            //foreach (var image in images)
            //{
            //    var imageBytes = image.Content.Content.ToArray();
            //    File.WriteAllBytes($"{SourceDirectory}{image.Name ?? image.LogicalIndex.ToString()}.png", imageBytes);
            //}

            //// Save buffers
            //int i = 0;
            //foreach (var buffer in model.LogicalBuffers)
            //{
            //    File.WriteAllBytes($"{SourceDirectory}buffer_{i++}.bin", buffer.Content.ToArray());
            //}
        }

        public async Task TestAdditionalFileGLB(CancellationToken cancellationToken)
        {
            string blenderExePath = BlenderExecutablePath;
            string blenderScriptPath = BlenderThumbnailScriptPath;

            string fileName = "A4000225-3D.glb";
            
            string recordId = "21f3fc7e085748eb98d8b3c4014699cd";
            //var images = GlbPreviewExtractor.ExtractImagesFromGlbStream(glbStream);
            //var preview = GlbPreviewExtractor.PickBestPreview(images);

            //if (preview == null)
            //{
            //    // No embedded images – you may need to render a thumbnail via a renderer instead.
            //    ;
            //}
            //else
            //{
            //    File.WriteAllBytes($"{SourceDirectory}{preview.FileName}", preview.Bytes);
            //}


            try
            {
                using FileStream glbStream = new FileStream($"{SourceDirectory}{fileName}", FileMode.Open);
                var pngBytes = await GlbThumbnailRenderer.RenderGlbThumbnailAsync(glbStream, blenderExePath, blenderScriptPath, 2000, cancellationToken);
                using var pngStream = new MemoryStream(pngBytes);

                string zipFileName = fileName.Replace(".glb", "_3dpackage.zip");

                await using var zipStream = await _aprimoClient.Build3dPackageZipFromExistingRecordAsync(glbStream, fileName,pngStream, "preview.png", cancellationToken);

                await _aprimoClient.UploadNewVersionFileToRecordAsync(
                    recordId,
                    zipStream,
                    zipFileName,          // must match your ZIP-identification package rule
                    "application/zip",
                    cancellationToken);

                //File.WriteAllBytes($"{SourceDirectory}test_thumb.png", pngBytes);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Oh Noes! {ex.Message}");
            }


    ;

        }
        #endregion TESTS

        #region MAIN_PROCESSES
        public async Task CreateAssetsToImageSets(CancellationToken cancellationToken)
        {

            var logOutput = new List<string>();

            // find all imagesets

            // get everything so we can do lookups
            //var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            //var allImagesetFiles = allFiles.Where(x => x.EndsWith("_related.json")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allImagesetFiles);
            //SaveStreamToFile(stream, SourceDirectory, "allImagesetBlobs.csv");
            //stream.Dispose();

            var allImagesetFiles = File.ReadAllLines($"{SourceDirectory}\\allImagesetBlobs.csv");
            _logger.LogInformation($"Found {allImagesetFiles.Count()} image sets in Azure");
            logOutput.Add($"Found {allImagesetFiles.Count()} image sets in Azure");

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");

            int blobCounter = 0;
            int batchCounter = 0;

            Dictionary<string, List<string>> allAssetImageSets = new Dictionary<string, List<string>>();

            foreach (var blob in allImagesetFiles)
            {
                //batchCounter++;
                //Dictionary<string, AprimoImageSet> allImageSets = new Dictionary<string, AprimoImageSet>();
                //foreach (var blob in batch)
                //{
                blobCounter++;
                string imageSetPath = blob.Replace("\\", "/").Replace("_related.json", "").Replace($"{_azureOptions.Value.AssetRootPrefix}/", "/");
                try
                {
                    // find the image set
                    var imageSetFileName = Path.GetFileName(imageSetPath);
                    var imageSetPathOnly = Path.GetDirectoryName(imageSetPath);
                    string folderHash = GetFolderHash(imageSetPathOnly);

                    var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{imageSetPathOnly}";
                    string uniqueId = $"{imageSetFileName}";


                    string azureMetadataFilename = imageSetFileName + "_metadata.json";
                    string azureRelatedFilename = imageSetFileName + "_related.json";

                    //string azureImageSetMetadata = $"{_azureOptions.Value.AssetRootPrefix}{imageSetPathOnly}\\{folderHash}_{imageSetFileName}_metadata.json";
                    //string azureImageSetRelated = $"{_azureOptions.Value.AssetRootPrefix}{imageSetPathOnly}\\{folderHash}_{imageSetFileName}_related.json";

                    //var imageSet = JsonConvert.DeserializeObject<AprimoImageSet>(json);

                    try
                    {
                        bool fileExists = await _assetsWrapper.BlobExistsAsync($"{azureRelatedFilename}", assetFolder);
                        bool fileMetadataExists = await _assetsWrapper.BlobExistsAsync($"{azureMetadataFilename}", assetFolder);

                        if (fileExists && fileMetadataExists)
                        {
                            string imageSetRelatedJson = await ReadJsonFile(azureRelatedFilename, assetFolder);
                            var relatedData = JsonConvert.DeserializeObject<AprimoImageSetAssets>(imageSetRelatedJson);

                            List<string> azureRelatedAssets = new List<string>();
                            foreach (var relatedAsset in relatedData.Resources)
                            {
                                try
                                {
                                    var fileName = Path.GetFileName(relatedAsset);
                                    string cleanedFilename = Regex.Replace(fileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

                                    var azureFile = allAssetFiles.Where(f => ContainsPathIgnoringGuid(f, relatedAsset.Replace(fileName, cleanedFilename))).FirstOrDefault();
                                    azureRelatedAssets.Add(azureFile);

                                    string azureFileName = Path.GetFileName(azureFile);
                                    string[] azureFileNameParts = azureFileName.Split("_");
                                    string relatedUUID = azureFileNameParts[0];

                                    if (allAssetImageSets.ContainsKey(relatedUUID))
                                    {
                                        var imageSets = allAssetImageSets[relatedUUID];
                                        if (!imageSets.Contains(uniqueId))
                                        {
                                            imageSets.Add(uniqueId);
                                            allAssetImageSets[relatedUUID] = imageSets;
                                        }
                                    }
                                    else
                                    {
                                        allAssetImageSets.Add(relatedUUID, new List<string>() { uniqueId });
                                    }

                                }
                                catch (Exception ex)
                                {
                                    logOutput.Add($"Warning! Could not find azure file for related asset {relatedAsset} in image set {uniqueId}. {ex.Message}");
                                    _logger.LogInformation($"Warning! Could not find azure file for related asset {relatedAsset} in image set {uniqueId}. {ex.Message}");
                                }
                            }

                            ;

                        }

                    }
                    catch (Exception ex)
                    {
                        logOutput.Add($"ex: {ex.Message}");
                    }


                }
                catch (Exception ex)
                {
                    logOutput.Add($"ex2: {ex.Message}");
                }

                //    _logger.LogInformation($"blobCounter:  {blobCounter}.");
                //}

                _logger.LogInformation($"processed {blobCounter}");



            }
            // Serialize to a JSON string 
            string jsonString = JsonConvert.SerializeObject(allAssetImageSets, Formatting.None);
            File.WriteAllText($"{Dump}allAssetsToImageSetsProd.json", jsonString);

            await LogToAzure("allAssetsToImageSetsJSONoutput.xlsx", logOutput);
        }

        public async Task CreateAllImageSetData(CancellationToken cancellationToken)
        {

            var logOutput = new List<string>();

            // find all imagesets

            // get everything so we can do lookups
            //var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            //var allImagesetFiles = allFiles.Where(x => x.EndsWith("_related.json")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allImagesetFiles);
            //SaveStreamToFile(stream, SourceDirectory, "allImagesetBlobs.csv");
            //stream.Dispose();

            var allImagesetFiles = File.ReadAllLines($"{SourceDirectory}\\allImagesetBlobs.csv");
            _logger.LogInformation($"Found {allImagesetFiles.Count()} image sets in Azure");
            logOutput.Add($"Found {allImagesetFiles.Count()} image sets in Azure");

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");

            int blobCounter = 0;
            int batchCounter = 0;

            Dictionary<string, AprimoImageSet> allAssetImageSets = new Dictionary<string, AprimoImageSet>();
            Dictionary<string, AprimoImageSetAssets> allAssetImageSetAssets = new Dictionary<string, AprimoImageSetAssets>();

            foreach (var blob in allImagesetFiles)
            {
                //batchCounter++;
                //Dictionary<string, AprimoImageSet> allImageSets = new Dictionary<string, AprimoImageSet>();
                //foreach (var blob in batch)
                //{
                blobCounter++;
                string imageSetPath = blob.Replace("\\", "/").Replace("_related.json", "").Replace($"{_azureOptions.Value.AssetRootPrefix}/", "/");
                try
                {
                    // find the image set
                    var imageSetFileName = Path.GetFileName(imageSetPath);
                    var imageSetPathOnly = Path.GetDirectoryName(imageSetPath);
                    string folderHash = GetFolderHash(imageSetPathOnly);

                    var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{imageSetPathOnly}";
                    string uniqueId = $"{imageSetFileName}";


                    string azureMetadataFilename = imageSetFileName + "_metadata.json";
                    string azureRelatedFilename = imageSetFileName + "_related.json";

                    //string azureImageSetMetadata = $"{_azureOptions.Value.AssetRootPrefix}{imageSetPathOnly}\\{folderHash}_{imageSetFileName}_metadata.json";
                    //string azureImageSetRelated = $"{_azureOptions.Value.AssetRootPrefix}{imageSetPathOnly}\\{folderHash}_{imageSetFileName}_related.json";

                    //var imageSet = JsonConvert.DeserializeObject<AprimoImageSet>(json);

                    try
                    {
                        bool fileExists = await _assetsWrapper.BlobExistsAsync($"{azureRelatedFilename}", assetFolder);
                        bool fileMetadataExists = await _assetsWrapper.BlobExistsAsync($"{azureMetadataFilename}", assetFolder);

                        if (fileExists && fileMetadataExists)
                        {
                            string imageSetRelatedJson = await ReadJsonFile(azureRelatedFilename, assetFolder);
                            var relatedData = JsonConvert.DeserializeObject<AprimoImageSetAssets>(imageSetRelatedJson);

                            if (!allAssetImageSetAssets.ContainsKey(uniqueId))
                            {
                                allAssetImageSetAssets.Add(uniqueId, relatedData);
                            }

                            string imageSetJson = await ReadJsonFile(azureMetadataFilename, assetFolder);
                            var imageSetData = JsonConvert.DeserializeObject<AprimoImageSet>(imageSetJson);

                            if (!allAssetImageSets.ContainsKey(uniqueId))
                            {
                                allAssetImageSets.Add(uniqueId, imageSetData);
                            }
                            ;

                        }

                    }
                    catch (Exception ex)
                    {
                        logOutput.Add($"ex: {ex.Message}");
                    }


                }
                catch (Exception ex)
                {
                    logOutput.Add($"ex2: {ex.Message}");
                }

                //    _logger.LogInformation($"blobCounter:  {blobCounter}.");
                //}

                _logger.LogInformation($"processed {blobCounter}");



            }
            // Serialize to a JSON string 
            string jsonString = JsonConvert.SerializeObject(allAssetImageSets, Formatting.None);
            File.WriteAllText($"{Dump}allImageSets.json", jsonString);

            jsonString = JsonConvert.SerializeObject(allAssetImageSetAssets, Formatting.None);
            File.WriteAllText($"{Dump}allImageSetAssets.json", jsonString);

            await LogToAzure("createAllImageSetDataJSONoutput.xlsx", logOutput);
        }
        public async Task CreateAllImageSetsJSONFromAzure(CancellationToken cancellationToken)
        {


            ResetState();

            var logOutput = new List<string>();

            // find all imagesets

            // get everything so we can do lookups
            //var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            //var allImagesetFiles = allFiles.Where(x => x.EndsWith("_related.json")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allImagesetFiles);
            //SaveStreamToFile(stream, SourceDirectory, "allImagesetBlobs.csv");
            //stream.Dispose();

            var allImagesetFiles = File.ReadAllLines($"{SourceDirectory}\\allImagesetBlobs.csv");
            _logger.LogInformation($"Found {allImagesetFiles.Count()} image sets in Azure");
            logOutput.Add($"Found {allImagesetFiles.Count()} image sets in Azure");

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");

            int blobCounter = 0;
            int batchCounter = 0;
            foreach (var batch in allImagesetFiles.Batch(10000))
            {
                batchCounter++;
                Dictionary<string, AprimoImageSet> allImageSets = new Dictionary<string, AprimoImageSet>();
                foreach (var blob in batch)
                {
                    blobCounter++;
                    string imageSetPath = blob.Replace("\\", "/").Replace("_related.json", "").Replace($"{_azureOptions.Value.AssetRootPrefix}/", "/");
                    try
                    {
                        // find the image set
                        var imageSetFileName = Path.GetFileName(imageSetPath);
                        var imageSetPathOnly = Path.GetDirectoryName(imageSetPath);
                        string folderHash = GetFolderHash(imageSetPathOnly);

                        var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{imageSetPathOnly}";
                        string uniqueId = $"{imageSetFileName}";

                        if (!allImageSets.ContainsKey(uniqueId))
                        {
                            string azureMetadataFilename = imageSetFileName + "_metadata.json";
                            string azureRelatedFilename = imageSetFileName + "_related.json";

                            //string azureImageSetMetadata = $"{_azureOptions.Value.AssetRootPrefix}{imageSetPathOnly}\\{folderHash}_{imageSetFileName}_metadata.json";
                            //string azureImageSetRelated = $"{_azureOptions.Value.AssetRootPrefix}{imageSetPathOnly}\\{folderHash}_{imageSetFileName}_related.json";

                            //var imageSet = JsonConvert.DeserializeObject<AprimoImageSet>(json);

                            try
                            {
                                bool fileExists = await _assetsWrapper.BlobExistsAsync($"{azureRelatedFilename}", assetFolder);
                                bool fileMetadataExists = await _assetsWrapper.BlobExistsAsync($"{azureMetadataFilename}", assetFolder);

                                if (fileExists && fileMetadataExists)
                                {
                                    string imageSetRelatedJson = await ReadJsonFile(azureRelatedFilename, assetFolder);
                                    var relatedData = JsonConvert.DeserializeObject<AprimoImageSetAssets>(imageSetRelatedJson);

                                    string imageSetJson = await ReadJsonFile(azureMetadataFilename, assetFolder);
                                    var imageSetData = JsonConvert.DeserializeObject<AprimoImageSet>(imageSetJson);
                                    imageSetData.PathToImageSet = imageSetPath;

                                    List<string> azureRelatedAssets = new List<string>();
                                    List<string> aprimoRelatedAssets = new List<string>();
                                    foreach (var relatedAsset in relatedData.Resources)
                                    {
                                        try
                                        {
                                            var azureFile = allAssetFiles.Where(f => ContainsPathIgnoringGuid(f, relatedAsset)).FirstOrDefault();
                                            azureRelatedAssets.Add(azureFile);

                                            string azureFileName = Path.GetFileName(azureFile);
                                            string[] azureFileNameParts = azureFileName.Split("_");
                                            string relatedUUID = azureFileNameParts[0];
                                            var assetRelatedRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(relatedUUID, cancellationToken);

                                            if (assetRelatedRecord != null)
                                            {
                                                aprimoRelatedAssets.Add(assetRelatedRecord.Id);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            logOutput.Add($"Warning! Could not find azure file for related asset {relatedAsset} in image set {imageSetData.PathToImageSet}. {ex.Message}");
                                            _logger.LogInformation($"Warning! Could not find azure file for related asset {relatedAsset} in image set {imageSetData.PathToImageSet}. {ex.Message}");
                                        }

                                    }
                                    relatedData.AprimoRecords = aprimoRelatedAssets;
                                    relatedData.AzureResources = azureRelatedAssets;
                                    imageSetData.AprimoImageSetAssets = relatedData;

                                    if (aprimoRelatedAssets.Count() != azureRelatedAssets.Count())
                                    {
                                        logOutput.Add($"Warning! Could not find all related assets in Aprimo for image set");
                                        _logger.LogInformation($"Warning! Could not find all related assets in Aprimo for image set");
                                    }

                                    logOutput.Add($"image set {uniqueId} has {aprimoRelatedAssets}");
                                    _logger.LogInformation($"image set {uniqueId} has {aprimoRelatedAssets.Count()} aprimo assets");

                                    allImageSets.Add($"{uniqueId}", imageSetData);
                                    ;

                                }

                            }
                            catch (Exception ex)
                            {
                                logOutput.Add($"Record {uniqueId} failed: {ex.Message}");
                            }
                        }
                        else
                        {
                            // already processed
                            logOutput.Add($"already processed image set {imageSetPath}. skip");
                            _logger.LogInformation($"already processed image set {imageSetPath}. skip");
                        }

                    }
                    catch (Exception ex)
                    {
                        logOutput.Add($"error processing image set path {imageSetPath}. skip");
                        _logger.LogInformation($"error processing image set path {imageSetPath}. skip");
                    }

                    _logger.LogInformation($"blobCounter:  {blobCounter}.");
                }

                logOutput.Add($"Found {allImageSets.Count} image sets in Batch {batchCounter}");
                _logger.LogInformation($"Found {allImageSets.Count} image sets in Batch {batchCounter}");

                // Serialize to a JSON string 
                string jsonString = JsonConvert.SerializeObject(allImageSets, Formatting.None);
                File.WriteAllText($"{Dump}allImageSetsProd_batch{batchCounter}.json", jsonString);

            }


            await LogToAzure("allImageSetsJSONoutput.xlsx", logOutput);
        }

        public async Task CreateAllFullImageSetsJSONFromAzure(CancellationToken cancellationToken)
        {

            var allImagesetFiles = File.ReadAllLines(Path.Combine(SourceDirectory, "allImagesetBlobs.csv"));
            _logger.LogInformation("Found {Count} image sets in Azure", allImagesetFiles.Length);

            var allDeltaImagesetFiles = File.ReadAllLines(Path.Combine(SourceDirectory, "allDeltaImagesetBlobs.csv"));
            _logger.LogInformation("Found {Count} delta image sets in Azure", allDeltaImagesetFiles.Length);

            var allCombined = allImagesetFiles.Concat(allDeltaImagesetFiles).ToArray();
            _logger.LogInformation("Found {Count} combined image sets in Azure", allCombined.Length);

            // key = file name after last '/'
            static string Key(string path) => path[(path.LastIndexOf('/') + 1)..];

            // Make lookups by key (delta wins)
            var origByKey = allImagesetFiles.ToDictionary(Key, x => x);
            var deltaByKey = allDeltaImagesetFiles.ToDictionary(Key, x => x);

            // distinct keys from both sets
            var keys = origByKey.Keys.Concat(deltaByKey.Keys).Distinct();

            // final list: pick delta if present, else original
            List<string> imageSetsToProcess = keys
                .Select(k => deltaByKey.TryGetValue(k, out var d) ? d : origByKey[k])
                .ToList();

            _logger.LogInformation("Found {Count} imageSetsToProcess", imageSetsToProcess.Count());
            ;
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository imagesetRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSets");

            int counter = 0;
            foreach(var imageSet in imageSetsToProcess)
            {
                counter++;
                if (counter > 146403) // pick up after last error
                {
                    string fullFileName = Path.GetFileName(imageSet);
                    string fileName = fullFileName.Replace("_related.json", "");
                    string azureMetadataFilename = fullFileName.Replace("_related.json", "_metadata.json");
                    string azurePath = Path.GetDirectoryName(imageSet);

                    string imageSetPath = imageSet.Replace("\\", "/").Replace("_related.json", "").Replace($"{_azureOptions.Value.AssetRootPrefix}/", "/").Replace($"{_azureOptions.Value.DeltasRootPrefix}/", "/");

                    string imageSetJson = await ReadJsonFile(azureMetadataFilename, azurePath);
                    var imageSetData = JsonConvert.DeserializeObject<AprimoImageSet>(imageSetJson);

                    string existingJson = imagesetRepo.GetJsonBodyByDictKey(fileName);
                    var existingimageSetData = JsonConvert.DeserializeObject<AprimoImageSet>(existingJson);

                    imageSetData.PathToImageSet = imageSetPath;
                    imageSetData.AprimoImageSetAssets = existingimageSetData.AprimoImageSetAssets;
                    imageSetData.ImageSetId = fileName;

                    string newJson = JsonConvert.SerializeObject(imageSetData);
                    imagesetRepo.UpdateJsonBody(fileName, newJson);

                    _logger.LogInformation($"{counter}: Processing {fileName} ");
                }

            }



        }

        public async Task CreateAllFullMetadataJSONFromAzure(CancellationToken cancellationToken)
        {

            var allAssetFiles = File.ReadAllLines(Path.Combine(SourceDirectory, "allAssetBlobs.csv"));
            _logger.LogInformation("Found {Count} assets in Azure", allAssetFiles.Length);

            var allDeltaAssetFiles = File.ReadAllLines(Path.Combine(SourceDirectory, "allDeltaAssetBlobs.csv"));
            _logger.LogInformation("Found {Count} delta assets in Azure", allDeltaAssetFiles.Length);

            var allCombined = allAssetFiles.Concat(allDeltaAssetFiles).ToArray();
            _logger.LogInformation("Found {Count} combined assets in Azure", allCombined.Length);

            static string Key(string path) => path[(path.LastIndexOf('/') + 1)..];

            var origByKey = allAssetFiles
                .GroupBy(Key)
                .ToDictionary(g => g.Key, g => g.First());

            var deltaByKey = allDeltaAssetFiles
                .GroupBy(Key)
                .ToDictionary(g => g.Key, g => g.First());

            //// distinct keys from both sets
            var keys = origByKey.Keys.Concat(deltaByKey.Keys).Distinct();

            // final list: pick delta if present, else original
            List<string> assetsToProcess = keys
                .Select(k => deltaByKey.TryGetValue(k, out var d) ? d : origByKey[k])
                .ToList();

            _logger.LogInformation("Found {Count} AssetsToProcess", assetsToProcess.Count());
            ;
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository assetRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.AssetMetadata");

            //string testUUID = "5171d86d-fcbd-4dc4-83c0-cdf5fa507002";
            //string testJson = assetRepo.GetJsonBodyByDictKey(testUUID);
            //AssetMetadata aTest = JsonConvert.DeserializeObject<AssetMetadata>(testJson);
            ;

            int counter = 0;
            foreach (var asset in assetsToProcess)
            {
                counter++;
                string fullFileName = Path.GetFileName(asset);
                string[] fileNameParts = fullFileName.Split("_");
                string uuid = fileNameParts[0];
                string azureMetadataFilename = uuid + "_metadata.json";
                string azurePath = Path.GetDirectoryName(asset);

                string assetJson = await ReadJsonFile(azureMetadataFilename, azurePath);
                assetRepo.UpdateJsonBody(asset, assetJson);

                _logger.LogInformation($"{counter}: Processing {asset} ");
            }




        }

        public async Task CreateAllImageSetsInAprimo(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();

            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");
            logOutput.Add($"found {classifications.Count()} classifications");

            var definitions = await _aprimoClient.GetAllDefinitionsAsync(cancellationToken);
            _logger.LogInformation($"found {definitions.Count()} definitions");
            logOutput.Add($"found {definitions.Count()} definitions");

            var imageSetClassification = classifications.Values
                .FirstOrDefault(c =>
                    c.Labels.Any(l =>
                        string.Equals(l.Value, "Image Set",
                                      StringComparison.OrdinalIgnoreCase)));

            string classificationId = imageSetClassification.Id;

            string importFileName = "allImageSetsInAprimo_delta3.xlsx";

            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            RestampPipelineRepository repo = new RestampPipelineRepository(connectionString);
            //MappingHelperObjectsRepository mhoFlatRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjectsFlat");

            // reset to specific imagesets
            //var allUpdatedImagesets = File.ReadAllLines($"{SourceDirectory}\\updatedImageSets.csv");
            //_logger.LogInformation($"Found {allUpdatedImagesets.Count()} updated image sets");

            //await mhoFlatRepo.ResetImageSetQueueAsync(allUpdatedImagesets.ToList(), cancellationToken);
            // end reset

            ;

            //testing
            //string uniqueId = "007DF54A079DDD04_M60000269-3PIS";
            //MappingHelperObjectsRepository imageSetsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSets");
            //string json = imageSetsRepo.GetJsonBodyByDictKey(uniqueId);
            //AprimoImageSet ais = JsonConvert.DeserializeObject<AprimoImageSet>(json);

            await Task.Delay(10000);

            var options = new AprimoAssetStamper.StamperOptions
            {
                BatchSize = 2000,
                MaxDegreeOfParallelism = 3,     // tune based on Aprimo limits
                MaxRetries = 5,
                InitialBackoff = TimeSpan.FromSeconds(1),
                MaxBackoff = TimeSpan.FromSeconds(30),
                ProgressInterval = TimeSpan.FromSeconds(10),
                Progress = p =>
                {
                    _logger.LogInformation(
                        $"{p.Phase} progress: Batches={p.BatchesCompleted:n0}, Claimed={p.TotalClaimed:n0}, Done={p.TotalDone:n0}, Failed={p.TotalFailed:n0}, Rate={p.ItemsPerSecond:n2}/s, Elapsed={p.Elapsed}");
                },
                OnRetry = r =>
                {
                    _logger.LogWarning($"Retry {r.Attempt} for {r.DictKey} after {r.Delay}: {r.Exception.Message}");
                }
            };

            // test run only
            //options.MaxBatches = 1;

            var summary = await AprimoAssetStamper.StampAllImageSetsPerItemAsync(
                repo,
                async (ImageSetStampRow row, CancellationToken ct) =>
                {
                    try
                    {
                        var uuid = row.DictKey;

                        //if (allUpdatedImagesets.Contains(uuid))
                        //{
                            AprimoImageSet ais = JsonConvert.DeserializeObject<AprimoImageSet>(row.ImageSetJson);
                            AprimoImageSetAssets aisa = JsonConvert.DeserializeObject<AprimoImageSetAssets>(row.ImageSetRelationsJson);

                            var errors = new List<string>();

                            logOutput.Add($"processing {uuid}");
                            _logger.LogInformation($"processing {uuid}");

                            if (aisa.AprimoRecords.Count() > 0)
                            {
                                ais.AprimoImageSetAssets = aisa;
                                logOutput.Add($"Image Set {uuid} has {ais.AprimoImageSetAssets.AprimoRecords.Count()} Aprimo Records ");
                                _logger.LogInformation($"Image Set {uuid} has {ais.AprimoImageSetAssets.AprimoRecords.Count()} Aprimo Records ");
                                var aprimoId = await CreateOrUpdateImageSet(uuid, ais, classificationId, definitions, classifications, ct, logOutput, errors);
                                // dry run
                                //await Task.Yield();

                                if (errors.Count() > 0)
                                {
                                    return AprimoAssetStamper.ItemStampResult.Fail(string.Join("~", errors), retryable: false);
                                }
                                else
                                {
                                    await repo.UpdateImageSetQueueDetailsAsync(
                                        row.DictKey,
                                        aprimoId,
                                        aisa.AprimoRecords.Count(),
                                        ct);

                                return AprimoAssetStamper.ItemStampResult.Ok();
                                }

                            }
                            else
                            {
                                logOutput.Add($"{uuid} does not have any resources");
                                _logger.LogInformation($"{uuid} does not have any resources");

                                return AprimoAssetStamper.ItemStampResult.Ok();
                            }
                        //} else
                        //{
                        //    return AprimoAssetStamper.ItemStampResult.Ok();
                        //}


                    }
                    catch (Exception ex)
                    {
                        // decide retryable or not based on the exception
                        return AprimoAssetStamper.ItemStampResult.Fail(ex.Message, retryable: true);
                    }


                },
                options,
                cancellationToken);

            _logger.LogInformation($"DONE. Claimed={summary.TotalClaimed:n0} Done={summary.TotalDone:n0} Failed={summary.TotalFailed:n0} Elapsed={summary.Elapsed}");

            await LogToAzure(importFileName, logOutput);

            ;
        }

        public async Task CreateImageSetsInAprimoFromFile(CancellationToken cancellationToken)
        {

            var logOutput = new List<string>();

            // find all imagesets
            Dictionary<string, AprimoImageSet> allImageSets = new Dictionary<string, AprimoImageSet>();

            // get everything so we can do lookups
            //var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            //var allImagesetFiles = allFiles.Where(x => x.EndsWith("_related.json")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allImagesetFiles);
            //SaveStreamToFile(stream, SourceDirectory, "allImagesetBlobs.csv");
            //stream.Dispose();

            var allImagesetFiles = File.ReadAllLines($"{SourceDirectory}\\allImagesetBlobs.csv");
            _logger.LogInformation($"Found {allImagesetFiles.Count()} image sets in Azure");
            logOutput.Add($"Found {allImagesetFiles.Count()} image sets in Azure");

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");

            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");
            logOutput.Add($"found {classifications.Count()} classifications");

            var definitions = await _aprimoClient.GetAllDefinitionsAsync(cancellationToken);
            _logger.LogInformation($"found {definitions.Count()} definitions");
            logOutput.Add($"found {definitions.Count()} definitions");

            var imageSetClassification = classifications.Values
                .FirstOrDefault(c =>
                    c.Labels.Any(l =>
                        string.Equals(l.Value, "Image Set",
                                      StringComparison.OrdinalIgnoreCase)));

            string classificationId = imageSetClassification.Id;


            string importFileName = "aprimorestamp_sampleAssetsWithMetadata_1.xlsx"; //"aprimorestamp_allAssetsWithMetadata1_1.xlsx";// 

            // don't add aprimoId column to spreadsheets that already have it.
            //var fileData = await ReadImportSpreadsheet(importFileName, _azureOptions.Value.ImportsRootPrefix, skipAprimoIdColumns: true);
            var fileData = await ReadImportSpreadsheet(importFileName, _azureOptions.Value.ImportsRootPrefix);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {importFileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {importFileName}");

            foreach (var rowData in fileData)
            {
                var aprimoId = "";
                //var aprimoId = rowData["AprimoId"];
                //_logger.LogInformation($"Processing Record {aprimoId}.");

                var uuid = rowData["Id"];
                var path = rowData["Path"];
                var imageSets = rowData["ImageSets"];

                if (!string.IsNullOrEmpty(imageSets))
                {
                    string[] imageSetPaths = imageSets.Split(',');
                    foreach (string imageSetPath in imageSetPaths)
                    {
                        try
                        {
                            await CreateImageSetFromPath(imageSetPath, classificationId, allImagesetFiles, allAssetFiles, definitions, classifications, cancellationToken, logOutput);
                            ;
                        }
                        catch (Exception ex)
                        {
                            logOutput.Add($"error processing image set path {imageSetPath}. skip");
                            _logger.LogInformation($"error processing image set path {imageSetPath}. skip");
                        }

                    }

                }
                else
                {
                    logOutput.Add($"{uuid}:{path} does not belong to an image set");
                    _logger.LogInformation($"{uuid}:{path} does not belong to an image set");
                }


            }


            await LogToAzure(importFileName, logOutput);

            if (!importFileName.Contains(".processed"))
            {
                string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{importFileName}";
                await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
            }
            ;
        }

        public async Task CreateImageSetsPreviewsInAprimoFromFile(CancellationToken cancellationToken)
        {

            var logOutput = new List<string>();

            // find all imagesets
            Dictionary<string, AprimoImageSet> allImageSets = new Dictionary<string, AprimoImageSet>();

            // get everything so we can do lookups
            //var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            //var allImagesetFiles = allFiles.Where(x => x.EndsWith("_related.json")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allImagesetFiles);
            //SaveStreamToFile(stream, SourceDirectory, "allImagesetBlobs.csv");
            //stream.Dispose();

            var allImagesetFiles = File.ReadAllLines($"{SourceDirectory}\\allImagesetBlobs.csv");
            _logger.LogInformation($"Found {allImagesetFiles.Count()} image sets in Azure");
            logOutput.Add($"Found {allImagesetFiles.Count()} image sets in Azure");

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");


            string importFileName = "aprimorestamp_allAssetsWithMetadata1_1.xlsx";// "aprimorestamp_sampleAssetsWithMetadata_1.xlsx"; //

            // don't add aprimoId column to spreadsheets that already have it.
            //var fileData = await ReadImportSpreadsheet(importFileName, _azureOptions.Value.ImportsRootPrefix, skipAprimoIdColumns: true);
            var fileData = await ReadImportSpreadsheet(importFileName, _azureOptions.Value.ImportsRootPrefix);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {importFileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {importFileName}");

            foreach (var rowData in fileData)
            {
                var aprimoId = "";
                //var aprimoId = rowData["AprimoId"];
                //_logger.LogInformation($"Processing Record {aprimoId}.");

                var uuid = rowData["Id"];
                var path = rowData["Path"];
                var imageSets = rowData["ImageSets"];

                if (!string.IsNullOrEmpty(imageSets))
                {
                    string[] imageSetPaths = imageSets.Split(',');
                    foreach (string imageSetPath in imageSetPaths)
                    {
                        try
                        {
                            //await CreateImageSetFromPath(imageSetPath, classificationId, allImagesetFiles, allAssetFiles, definitions, classifications, cancellationToken, logOutput);
                            // find the image set
                            var imageSetFileName = Path.GetFileName(imageSetPath);
                            var imageSetPathOnly = Path.GetDirectoryName(imageSetPath);
                            string folderHash = GetFolderHash(imageSetPathOnly);

                            var assetFolder = $"imagesetpreviews";
                            string cleanedFilename = Regex.Replace(imageSetFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

                            string uniqueId = $"{folderHash}_{cleanedFilename}";


                            string azureFileName = $"{uniqueId}_preview.png";


                            var aprimoRecord = await _aprimoClient.GetImageSetByAemImageSetIdAsync(uniqueId, cancellationToken);

                            if (aprimoRecord != null)
                            {
                                var previewStream = await _assetsWrapper.DownloadBlobAsync(azureFileName, assetFolder);
                                await _aprimoClient.UploadPreviewFileToRecordAsync(
                                    recordId: aprimoRecord.Id,
                                    fileStream: previewStream,
                                    fileName: $"{azureFileName}",
                                    contentType: "image/png",
                                    cancellationToken);

                                logOutput.Add($"updated preview image on {uniqueId} in Aprimo. skip");
                                _logger.LogInformation($"updated preview image on  {uniqueId} in Aprimo. skip");
                                ;
                            } else
                            {
                                logOutput.Add($"can't find {uniqueId} in Aprimo. skip");
                                _logger.LogInformation($"can't find {uniqueId} in Aprimo. skip");
                            }



                        }
                        catch (Exception ex)
                        {
                            logOutput.Add($"error processing image set path {imageSetPath}. skip");
                            _logger.LogInformation($"error processing image set path {imageSetPath}. skip");
                        }

                    }

                }
                else
                {
                    logOutput.Add($"{uuid}:{path} does not belong to an image set");
                    _logger.LogInformation($"{uuid}:{path} does not belong to an image set");
                }


            }

            ;
        }

        public async Task CreateImageSetsPreviewsInAprimoFromBatch(CancellationToken cancellationToken)
        {

            List<string> pathsToMissingPreviews = new List<string>();
            List<string> missingImagesets = new List<string>();

            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository imagesetQueueRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.StampImageSetsQueue", 17);
            MappingHelperObjectsRepository imagesetRelationsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSetsRelations");
            MappingHelperObjectsRepository imagesetRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSets");

            var missingPreviews = await _aprimoClient.GetImageSetsMissingPreviewAsync(cancellationToken);

            _logger.LogInformation($"Found {missingPreviews.Count()} imagesets missing a preview");

            foreach (var aprimoRecord in missingPreviews)
            {

                var item = await imagesetQueueRepo.GetImageSetQueueItemByAprimoIdAsync(aprimoRecord.Id, cancellationToken);

                if (item != null)
                {
                    string imageSetID = item.DictKey; 

                    try
                    {
                        var assetFolder = $"imagesetpreviews";
                        string azureFileName = $"{imageSetID}_preview.png";
                        bool previewExists = await _assetsWrapper.BlobExistsAsync(azureFileName, assetFolder);
                        if (!previewExists)
                        {
                            string imageSetJson = imagesetRepo.GetJsonBodyByDictKey(imageSetID);
                            AprimoImageSet imageSet = JsonConvert.DeserializeObject<AprimoImageSet>(imageSetJson);
                            var pathToAsset = imageSet.PathToImageSet;
                            pathsToMissingPreviews.Add(pathToAsset);

                            // already tried to get the above.  just use first image as preview
                            string imageSetAssetsJson = imagesetRelationsRepo.GetJsonBodyByDictKey(imageSetID);
                            AprimoImageSetAssets imageSetAssets = JsonConvert.DeserializeObject<AprimoImageSetAssets>(imageSetAssetsJson);

                            if (imageSetAssets.AzureResources.Count() > 0)
                            {
                                string firstAsset = imageSetAssets.AzureResources.First();
                                string firstAzureFilename = Path.GetFileName(firstAsset);
                                string firstAssetFolder = Path.GetDirectoryName(firstAsset);

                                var previewStream = await _assetsWrapper.DownloadBlobAsync(firstAzureFilename, firstAssetFolder);

                                await _aprimoClient.UploadPreviewFileToRecordAsync(
                                    recordId: aprimoRecord.Id,
                                    fileStream: previewStream,
                                    fileName: $"{azureFileName}",
                                    contentType: "image/png",
                                    cancellationToken);

                                _logger.LogInformation($"updated first image preview image on  {imageSetID} in Aprimo.");
                            } else
                            {
                                var deletedRecord = await _aprimoClient.DeleteAssetByAprimoIdAsync(aprimoRecord.Id, cancellationToken);
                                _logger.LogInformation($"No Resources in imageset.  deleted record {aprimoRecord.Id} : {deletedRecord}");
                                //_logger.LogInformation($"No Resources in imageset.  deleted record {aprimoRecord.Id} : ");
                            }

                        } else
                        {

                            var previewStream = await _assetsWrapper.DownloadBlobAsync(azureFileName, assetFolder);

                            await _aprimoClient.UploadPreviewFileToRecordAsync(
                                recordId: aprimoRecord.Id,
                                fileStream: previewStream,
                                fileName: $"{azureFileName}",
                                contentType: "image/png",
                                cancellationToken);

                            _logger.LogInformation($"updated preview image on  {imageSetID} in Aprimo.");
                        }


                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"error processing image set path {imageSetID}. skip");
                    }

                }
                else
                {
                    _logger.LogInformation($"{aprimoRecord.Id} : Can't Find!. delete");
                    missingImagesets.Add(aprimoRecord.Id);
                    var deletedRecord = await _aprimoClient.DeleteAssetByAprimoIdAsync(aprimoRecord.Id, cancellationToken);
                    _logger.LogInformation($"No Imageset for record.  deleted record {aprimoRecord.Id} : {deletedRecord}");
                    //_logger.LogInformation($"No Imageset for record.  deleted record {aprimoRecord.Id} ");
                    await Task.Delay(250);
                }

                
            }
            //MemoryStream stream = ConvertListToMemoryStream(pathsToMissingPreviews);
            //SaveStreamToFile(stream, SourceDirectory, "pathtomissingpreviews.csv");
            //stream.Dispose();

            //MemoryStream stream2 = ConvertListToMemoryStream(missingImagesets);
            //SaveStreamToFile(stream2, SourceDirectory, "missingimagesets.csv");
            //stream2.Dispose();
            ;
        }

        public async Task CreateImageSetFromPath(string imageSetPath, string classificationId, string[] allImagesetFiles, string[] allAssetFiles, IReadOnlyList<AprimoFieldDefinition> definitions, IReadOnlyDictionary<string, AprimoClassification> classifications, CancellationToken cancellationToken, List<string> logOutput)
        {
            try
            {
                // find the image set
                var imageSetFileName = Path.GetFileName(imageSetPath);
                var imageSetPathOnly = Path.GetDirectoryName(imageSetPath);
                string folderHash = GetFolderHash(imageSetPathOnly);

                var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{imageSetPathOnly}";
                string cleanedFilename = Regex.Replace(imageSetFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

                string uniqueId = $"{folderHash}_{cleanedFilename}";


                string azureMetadataFilename = $"{uniqueId}_metadata.json";
                string azureRelatedFilename = $"{uniqueId}_related.json";

                try
                {
                    bool fileExists = await _assetsWrapper.BlobExistsAsync($"{azureRelatedFilename}", assetFolder);
                    bool fileMetadataExists = await _assetsWrapper.BlobExistsAsync($"{azureMetadataFilename}", assetFolder);

                    if (fileExists && fileMetadataExists)
                    {
                        List<string> errors = new List<string>();
                        string imageSetRelatedJson = await ReadJsonFile(azureRelatedFilename, assetFolder);
                        var relatedData = JsonConvert.DeserializeObject<AprimoImageSetAssets>(imageSetRelatedJson);

                        string imageSetJson = await ReadJsonFile(azureMetadataFilename, assetFolder);
                        var imageSetData = JsonConvert.DeserializeObject<AprimoImageSet>(imageSetJson);
                        imageSetData.PathToImageSet = imageSetPath;

                        List<string> azureRelatedAssets = new List<string>();
                        List<string> aprimoRelatedAssets = new List<string>();
                        _logger.LogInformation($"Found {relatedData.Resources.Count()} assets in Imageset");
                        foreach (var relatedAsset in relatedData.Resources)
                        {
                            try
                            {
                                var fileName = Path.GetFileName(relatedAsset);
                                string cleanedRelatedFilename = Regex.Replace(fileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

                                var fullAzurePath = allAssetFiles.Where(f => ContainsPathIgnoringGuid(f, relatedAsset.Replace(fileName, cleanedRelatedFilename))).FirstOrDefault();
                                azureRelatedAssets.Add(fullAzurePath);

                                string azureFileName = Path.GetFileName(fullAzurePath);
                                string[] azureFileNameParts = azureFileName.Split("_");
                                string relatedUUID = azureFileNameParts[0];
                                var assetRelatedRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(relatedUUID, cancellationToken);

                                string relatedpath = Path.GetDirectoryName(fullAzurePath).Replace("\\", "/");
                                string[] parts = azureFileName.Split('_');

                                string relateduuid = relatedUUID;

                                if (assetRelatedRecord != null)
                                {
                                    aprimoRelatedAssets.Add(assetRelatedRecord.Id);

                                }
                                else
                                {
                                    // CREATE THE ASSET
                                    _logger.LogInformation($"{relatedAsset} not found in Azure");
                                    logOutput.Add($"{relatedAsset} not found in Azure");

                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"Warning! Could not find azure file for related asset {relatedAsset} in image set {imageSetData.PathToImageSet}. {ex.Message}");
                                logOutput.Add($"Warning! Could not find azure file for related asset {relatedAsset} in image set {imageSetData.PathToImageSet}. {ex.Message}");
                            }

                        }
                        relatedData.AprimoRecords = aprimoRelatedAssets;
                        relatedData.AzureResources = azureRelatedAssets;
                        imageSetData.AprimoImageSetAssets = relatedData;

                        if (aprimoRelatedAssets.Count() != azureRelatedAssets.Count())
                        {
                            _logger.LogInformation($"Warning! Could not find all related assets in Aprimo for image set {uniqueId}");
                            logOutput.Add($"Warning! Could not find all related assets in Aprimo for image set {uniqueId}");
                        }

                        _logger.LogInformation($"image set {uniqueId} has {aprimoRelatedAssets.Count()} aprimo assets");
                        logOutput.Add($"image set {uniqueId} has {aprimoRelatedAssets.Count()} aprimo assets");

                        if (relatedData.AprimoRecords.Count() > 0) {
                            await CreateOrUpdateImageSet(uniqueId, imageSetData, classificationId, definitions, classifications, cancellationToken, logOutput, errors);
                        } else
                        {
                            _logger.LogInformation($"image set {uniqueId} has 0 aprimo assets. did not create");
                            logOutput.Add($"image set {uniqueId} has 0 aprimo assets.  did not create");

                        }
                        
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Record {uniqueId} failed: {ex.Message}");
                    logOutput.Add($"Record {uniqueId} failed: {ex.Message}");
                }


            }
            catch (Exception ex)
            {
                _logger.LogInformation($"error processing image set path {imageSetPath}. skip");
                logOutput.Add($"error processing image set path {imageSetPath}. skip");
            }
            ;
        }

        public async Task<AprimoImageSetAssets> CreateImageSetAssetsFromPath(string imageSetPath, string assetsRootPrefix, string[] allAssetFiles, CancellationToken cancellationToken, List<string> logOutput)
        {
            AprimoImageSetAssets aprimoImageSetAssets = new AprimoImageSetAssets();
            try
            {
                // find the image set
                var imageSetFileName = Path.GetFileName(imageSetPath);
                var imageSetPathOnly = Path.GetDirectoryName(imageSetPath);
                string folderHash = GetFolderHash(imageSetPathOnly);

                var assetFolder = $"{assetsRootPrefix}{imageSetPathOnly}";
                string cleanedFilename = Regex.Replace(imageSetFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

                string uniqueId = $"{folderHash}_{cleanedFilename}";
                string azureRelatedFilename = $"{uniqueId}_related.json";

                try
                {
                    bool fileExists = await _assetsWrapper.BlobExistsAsync($"{azureRelatedFilename}", assetFolder);

                    if (fileExists)
                    {
                        string imageSetRelatedJson = await ReadJsonFile(azureRelatedFilename, assetFolder);
                        var relatedData = JsonConvert.DeserializeObject<AprimoImageSetAssets>(imageSetRelatedJson);


                        List<string> azureRelatedAssets = new List<string>();
                        List<string> aprimoRelatedAssets = new List<string>();
                        _logger.LogInformation($"Found {relatedData.Resources.Count()} assets in Imageset");
                        foreach (var relatedAsset in relatedData.Resources)
                        {
                            try
                            {
                                var fileName = Path.GetFileName(relatedAsset);
                                string cleanedRelatedFilename = Regex.Replace(fileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

                                var fullAzurePath = allAssetFiles.Where(f => ContainsPathIgnoringGuid(f, relatedAsset.Replace(fileName, cleanedRelatedFilename))).FirstOrDefault();
                                azureRelatedAssets.Add(fullAzurePath);

                                string azureFileName = Path.GetFileName(fullAzurePath);
                                string[] azureFileNameParts = azureFileName.Split("_");
                                string relatedUUID = azureFileNameParts[0];
                                var assetRelatedRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(relatedUUID, cancellationToken);

                                string relatedpath = Path.GetDirectoryName(fullAzurePath).Replace("\\", "/");
                                string[] parts = azureFileName.Split('_');

                                string relateduuid = relatedUUID;

                                if (assetRelatedRecord != null)
                                {
                                    aprimoRelatedAssets.Add(assetRelatedRecord.Id);

                                }
                                else
                                {
                                    // CREATE THE ASSET
                                    _logger.LogInformation($"{relatedAsset} not found in Azure");
                                    logOutput.Add($"{relatedAsset} not found in Azure");

                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"Warning! Could not find azure file for related asset {relatedAsset} in image set {imageSetPath}. {ex.Message}");
                                logOutput.Add($"Warning! Could not find azure file for related asset {relatedAsset} in image set {imageSetPath}. {ex.Message}");
                            }

                        }
                        relatedData.AprimoRecords = aprimoRelatedAssets;
                        relatedData.AzureResources = azureRelatedAssets;


                        if (aprimoRelatedAssets.Count() != azureRelatedAssets.Count())
                        {
                            _logger.LogInformation($"Warning! Could not find all related assets in Aprimo for image set {uniqueId}");
                            logOutput.Add($"Warning! Could not find all related assets in Aprimo for image set {uniqueId}");
                        }

                        _logger.LogInformation($"image set {uniqueId} has {aprimoRelatedAssets.Count()} aprimo assets");
                        logOutput.Add($"image set {uniqueId} has {aprimoRelatedAssets.Count()} aprimo assets");

                        aprimoImageSetAssets = relatedData;
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Record {uniqueId} failed: {ex.Message}");
                    logOutput.Add($"Record {uniqueId} failed: {ex.Message}");
                }


            }
            catch (Exception ex)
            {
                _logger.LogInformation($"error processing image set path {imageSetPath}. skip");
                logOutput.Add($"error processing image set path {imageSetPath}. skip");
            }

            return aprimoImageSetAssets;
            ;
        }
        public async Task<string> CreateOrUpdateImageSet(string uniqueId, AprimoImageSet imageSet, string classificationId, IReadOnlyList<AprimoFieldDefinition> definitions, IReadOnlyDictionary<string, AprimoClassification> classifications, CancellationToken cancellationToken, List<string> logOutput, List<string> errors)
        {

            // Asset_Type Subtype Rule - folderMapping.json
            var folderRules = FolderClassificationRuleLoader.LoadFolderMapping();

            // Vendor Name and possible Subtype Rule - 3PVendorAndSubtypeMappping.json
            var vendorSubtypeRuleSets = FolderClassificationRuleLoader.Load3PVendorAndSubtypeMapping();

            string title = imageSet.DcTitle;
            if (string.IsNullOrWhiteSpace(title))
            {
                string[] parts = uniqueId.Split('_');
                title = parts[1];
            }
            imageSet.ImageSetId = title;
            string utcString = DateTime.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT+0000";
            imageSet.LastTouchedUTC = utcString; //DateTime.UtcNow.ToString("o");
            logOutput.Add($"ImageSetId = {title}");
            AprimoRecord assetRecord = null;

            var assetRecords = await _aprimoClient.GetImageSetsByAemImageSetIdAsync(uniqueId, cancellationToken);
            if (assetRecords.Count > 0)
            {
                if (assetRecords.Count > 1)
                {
                    _logger.LogInformation($"Uh Oh! Found {assetRecords.Count} records matching uniqueId {uniqueId}");
                    var ordered = assetRecords.OrderByDescending(r => r.ModifiedOn).ToList();
                    assetRecord = ordered.First();
                    var recordsToDelete = ordered.Skip(1).ToList();

                    foreach (var record in recordsToDelete)
                    {
                        var deletedRecord = await _aprimoClient.DeleteAssetByAprimoIdAsync(record.Id, cancellationToken);
                        _logger.LogInformation($"deleted record {record.Id} : {deletedRecord}");
                        logOutput.Add($"deleted record {record.Id} : {deletedRecord}");
                    }
                }
                else
                {
                    assetRecord = assetRecords[0];
                }

            }


            if (assetRecord != null)
            {

                try
                {
                    //fix existing rows that did not get stamped the first time
                    ///  ImageSetId will be null :  9599f21596724fe6abb4b3ef0104281b
                    //string imageSetIdFieldValue = string.Empty;
                    //var imageSetIdField = assetRecord.Embedded.Fields.Items.Where(x => x.Id == "9599f21596724fe6abb4b3ef0104281b").FirstOrDefault();
                    //if (imageSetIdField != null)
                    //{
                    //    imageSetIdFieldValue = imageSetIdField.LocalizedValues[0].Value;
                    //}

                    ////fix upstamped existing assets - remove this for restamping everything, but in this case we are restamping only those that did not stamp properly the first time.
                    //if (string.IsNullOrEmpty(imageSetIdFieldValue))
                    //{
                    if (!hasPrimedRecord)
                    {
                        await _aprimoClient.EnsureLanguagesLoadedAsync(cancellationToken);
                        languageId = _aprimoClient.ResolveLanguageId(locale);
                        await _aprimoClient.PrimeFieldIdCacheFromRecordAsync(assetRecord.Id!, cancellationToken);
                        hasPrimedRecord = true;
                        _logger.LogInformation($"Loaded languages and primed cache");
                        logOutput.Add($"Loaded languages and primed cache");
                    }

                    var upserts = AprimoUpsertBuilder.BuildUpserts(imageSet, _aprimoClient, definitions, classifications, logOutput, languageId);
                    _logger.LogInformation($"Found {upserts.Count()} metadata upserts.");

                    List<AprimoFieldUpsert> recordLinks = upserts.Where(x => x.IsRecordLink).ToList();
                    await _aprimoClient.ClearRecordLinkMetadataAsync(assetRecord.Id!, recordLinks, new List<AprimoFieldUpsert>(), new List<AprimoFieldUpsert>(), cancellationToken);
                    _logger.LogInformation($"Cleared Record Links.");


                    Dictionary<string, string> classificationData = new Dictionary<string, string>();
                    // add custom classification fields here

                    // build classifications for vendor name
                    // 3p image set may not live in the folder where the resources are.  use first resource
                    string pathOfImageInImageSet = imageSet.AprimoImageSetAssets.AzureResources[0];
                    var rule = FolderClassificationRuleLoader.GetRuleForPath(folderRules, pathOfImageInImageSet);

                    string vendor = string.Empty;

                    if (rule == null)
                    {
                        // no match found
                        _logger.LogWarning($"No folder rule found for path {pathOfImageInImageSet}");
                        logOutput.Add($"No folder rule found for path {pathOfImageInImageSet}");
                    }
                    else if (rule.RequiresAdditionalLogic)
                    {

                        if (rule.AemFolder.Equals("/content/dam/ashley-furniture/3rdparty"))
                        {


                            var match = FolderClassificationRuleLoader.Resolve(vendorSubtypeRuleSets, pathOfImageInImageSet);

                            vendor = match?.VendorName;   // may be null for pure subtype rules like Swatch Bank

                        }
                    }

                    string assetSubtype = "3P Image Set";
                    if (imageSet.PathToImageSet.Contains("ahs", StringComparison.OrdinalIgnoreCase))
                    {
                        assetSubtype = "AHS Image Set";
                    }
                    else if (imageSet.PathToImageSet.Contains("afi", StringComparison.OrdinalIgnoreCase))
                    {
                        assetSubtype = "AFI Image Set";
                    }

                    if (imageSet.ImageSetId.StartsWith("PKG", StringComparison.OrdinalIgnoreCase) || imageSet.ImageSetId.StartsWith("APG", StringComparison.OrdinalIgnoreCase))
                    {
                        assetSubtype = "Package Image Set";
                    }

                    classificationData.Add("Asset Subtype", assetSubtype);
                    classificationData.Add("Vendor Name", vendor);

                    var upsertsThatAreClassifications = upserts.Where(u => u.IsClassification).ToList();

                    var classificationUpserts = AprimoUpsertBuilder.BuildClassificationUpserts(classificationData, _aprimoClient, definitions, classifications, logOutput, languageId).ToList();

                    var upsertsReadyToApply = upserts.Where(u => !u.IsClassification).ToList();

                    foreach (var upsert in upsertsThatAreClassifications)
                    {
                        classificationData.Add(upsert.FieldLabel, upsert.RawValue);
                    }
                    classificationUpserts.AddRange(upsertsThatAreClassifications);

                    // handle clean up of previously stamped data
                    List<AprimoFieldUpsert> classificationsToRemove = new List<AprimoFieldUpsert>();
                    foreach (var key in classificationData.Keys)
                    {
                        var currentValues = GetLocalizedValuesForField(assetRecord, key);
                        if (currentValues.Count > 0)  // we only need to worry about cleaning up data if it exists
                        {
                            if (string.IsNullOrWhiteSpace(classificationData[key]))
                            {
                                // no value exists in this current stamping for this key. we need to remove all currently stamped values.
                                foreach (var value in currentValues)
                                {
                                    AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                    afu.Value = value;
                                    classificationsToRemove.Add(afu);
                                }
                            }
                            else
                            {
                                // a value exists in this current stamping.  
                                // we need to remove any current values that are not already the one we are trying to stamp
                                var classUpsert = classificationUpserts.Where(c => c.FieldLabel.Equals(key)).FirstOrDefault();
                                if (classUpsert != null)
                                {
                                    foreach (var value in currentValues)
                                    {
                                        if (!value.Equals(classUpsert.Value))
                                        {
                                            AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                            afu.Value = value;
                                            classificationsToRemove.Add(afu);
                                        }
                                    }
                                }
                            }
                        }

                    }

                    var excludedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        {
                            "Classification List",
                            "Date Time",
                            "Date",
                            "RecordLink"
                        };

                    var fields = typeof(AprimoImageSet)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .SelectMany(p => p.GetCustomAttributes<AprimoFieldAttribute>(true)
                            .Select(attr => new { Prop = p, Attr = attr }))
                        .Where(x => !excludedTypes.Contains((x.Attr.DataType ?? "").Trim()))
                        .Select(x => new
                        {
                            PropertyName = x.Prop.Name,
                            AprimoName = x.Attr.FieldName,
                            AprimoType = x.Attr.DataType,
                            RawValue = x.Prop.GetValue(imageSet),
                            Value = x.Prop.GetValue(imageSet) switch
                            {
                                null => null,
                                string s => s,
                                _ => x.Prop.GetValue(imageSet)!.ToString()
                            }
                        })
                        .ToList();


                    List<AprimoFieldUpsert> fieldsToRemove = new List<AprimoFieldUpsert>();
                //    foreach(var field in fields)
                //    {
                //        var currentValues = GetLocalizedValuesForFieldName(assetRecord, field.AprimoName);
                //        if (currentValues.Count > 0)  // we only need to worry about cleaning up data if it exists
                //        {
                //            var aprimoField = assetRecord?.Embedded?.Fields?.Items?
                //                .FirstOrDefault(f => string.Equals(f.FieldName, field.AprimoName, StringComparison.OrdinalIgnoreCase) && !f.DataType.Equals("ClassificationList"));

                //            bool isMulti = aprimoField.LocalizedValues[0].Values != null;

                //            if (field.Value is not string s || string.IsNullOrWhiteSpace(s))
                //            {
                //                // no value exists in this current stamping for this key. we need to remove all currently stamped values.
                //                foreach (var value in currentValues)
                //                {
                //                    AprimoFieldUpsert afu = new AprimoFieldUpsert();
                //                    if (isMulti)
                //                    {
                //                        afu.Values.Add(value);

                //                    } else
                //                    {
                //                        afu.Value = value;
                //                    }
                                        
                //                    afu.FieldId = aprimoField.Id;
                //                    afu.LanguageId = languageId;
                //                    fieldsToRemove.Add(afu);
                //                }
                //            }
                //            //else
                //            //{
                //            //    // a value exists in this current stamping.  
                //            //    // we need to remove any current values that are not already the one we are trying to stamp
                //            //    var fieldUpsert = upsertsReadyToApply.Where(c => c.FieldName.Equals(field.AprimoName)).FirstOrDefault();
                //            //    if (fieldUpsert != null)
                //            //    {
                //            //        if(fieldUpsert.Values != null)
                //            //        {
                //            //            //bool areEqual = fieldUpsert.Values.Count == currentValues.Count && new HashSet<string>(fieldUpsert.Values).SetEquals(currentValues);
                //            //            var difference = currentValues.Except(fieldUpsert.Values).ToList();
                //            //            if (difference.Count > 0)
                //            //            {
                //            //                AprimoFieldUpsert afu = new AprimoFieldUpsert();
                //            //                afu.Values = difference;
                //            //                afu.FieldId = aprimoField.Id;
                //            //                afu.LanguageId = languageId;
                //            //                fieldsToRemove.Add(afu);
                //            //            }

                //            //        }
                //            //        else
                //            //        {
                //            //            foreach (var value in currentValues)
                //            //            {

                //            //                if (!value.Equals(fieldUpsert.Value))
                //            //                {
                //            //                    AprimoFieldUpsert afu = new AprimoFieldUpsert();
                //            //                    afu.Value = value;
                //            //                    afu.FieldId = aprimoField.Id;
                //            //                    afu.LanguageId = languageId;
                //            //                    fieldsToRemove.Add(afu);
                //            //                }
                //            //            }
                //            //        }
                //            //    }
                //            //}
                //        }
                //}

                ;

                try
                {
                    _logger.LogInformation($"Attempting to Restamp {assetRecord.Id}");
                    logOutput.Add($"Attempting to Restamp {assetRecord.Id}");
                    await _aprimoClient.StampMetadataAsync(assetRecord.Id!, upsertsReadyToApply, fieldsToRemove, classificationUpserts, classificationsToRemove, cancellationToken);
                    return assetRecord.Id;
                } catch (Exception ex)
                {
                    // I specifically had code so that i would not have to do this...
                    if (ex.Message.Contains("500 Internal"))
                    {
                        bool tryStampAgain = false;
                        foreach (var upsert in upsertsReadyToApply)
                        {
                            if (upsert.IsRecordLink)
                            {
                                List<string> values = upsert.Values;
                                List<string> newValues = new List<string>();
                                foreach (string val in values)
                                {
                                    if (val != null)
                                    {
                                        var testRecord = await _aprimoClient.GetAssetByAprimoIdAsync(val, cancellationToken);
                                        if (testRecord != null)
                                        {
                                            newValues.Add(val);
                                        }
                                        else
                                        {
                                            logOutput.Add($"{val} in ImageSet {uniqueId} does not exist! removing from Imageset Assets");
                                        }
                                    } else
                                    {
                                        logOutput.Add($"Value in ImageSet {uniqueId} was Null! removing from Imageset Assets");
                                    }

                                }

                                if (newValues.Count > 0)
                                {
                                    upsert.Values = newValues;
                                    tryStampAgain = true;
                                } else
                                {
                                    logOutput.Add($"No values left in Imageset Assets!");
                                }
                            }
                        }
                        if (tryStampAgain)
                        {
                            _logger.LogInformation($"Attempting to Retry Restamp {assetRecord.Id}");
                            logOutput.Add($"Attempting to Retry Restamp {assetRecord.Id}");
                            await _aprimoClient.StampMetadataAsync(assetRecord.Id!, upsertsReadyToApply, fieldsToRemove, classificationUpserts, classificationsToRemove, cancellationToken);
                            return assetRecord.Id;
                        }
                    }
                    else
                    {
                        throw new Exception(ex.Message);
                    }
                }
                        

                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Could not stamp asset with uniqueId {uniqueId}: {ex.Message}");
                    logOutput.Add($"Could not stamp asset with uniqueId {uniqueId}: {ex.Message}");
                    errors.Add($"Could not stamp asset with uniqueId {uniqueId}: {ex.Message}");
                }

            }
            else
            {
                // create new record

                try
                {
                    logOutput.Add($"Creating new ImageSet Record for {title}");
                    var created = await _aprimoClient.UploadImageSetToAprimoAsync(title, classificationId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(created.Id))
                    {
                        if (!hasPrimedRecord)
                        {
                            await _aprimoClient.EnsureLanguagesLoadedAsync(cancellationToken);
                            languageId = _aprimoClient.ResolveLanguageId(locale);
                            await _aprimoClient.PrimeFieldIdCacheFromRecordAsync(created.Id!, cancellationToken);
                            hasPrimedRecord = true;
                            _logger.LogInformation($"Loaded languages and primed cache");
                            logOutput.Add($"Loaded languages and primed cache");
                        }


                        try
                        {
                            // then stamp AEM Image Set ID
                            logOutput.Add($"Stamping ImageSet Record {created.Id} with {uniqueId}");
                            await _aprimoClient.StampAemImageSetIdAsync(created.Id!, uniqueId, ct: cancellationToken);
                            logOutput.Add($"Record {created.Id} Inital Stamping with {uniqueId} Complete");

                            var upserts = AprimoUpsertBuilder.BuildUpserts(imageSet, _aprimoClient, definitions, classifications, logOutput, languageId);

                            _logger.LogInformation($"Found {upserts.Count()} metadata upserts.");

                            // build classifications for vendor name
                            var rule = FolderClassificationRuleLoader.GetRuleForPath(folderRules, imageSet.PathToImageSet);

                            string vendor = string.Empty;

                            if (rule == null)
                            {
                                // no match found
                                _logger.LogWarning($"No folder rule found for path {imageSet.PathToImageSet}");
                                logOutput.Add($"No folder rule found for path {imageSet.PathToImageSet}");
                            }
                            else if (rule.RequiresAdditionalLogic)
                            {

                                if (rule.AemFolder.Equals("/content/dam/ashley-furniture/3rdparty"))
                                {
                                    // 3p image set may not live in the folder where the resources are.  use first resource
                                    string pathOfImageInImageSet = imageSet.AprimoImageSetAssets.AzureResources[0];

                                    var match = FolderClassificationRuleLoader.Resolve(vendorSubtypeRuleSets, pathOfImageInImageSet);

                                    vendor = match?.VendorName;   // may be null for pure subtype rules like Swatch Bank

                                }
                            }


                            Dictionary<string, string> classificationData = new Dictionary<string, string>();
                            // add custom classification fields here
                            string assetSubtype = "3P Image Set";
                            if (imageSet.PathToImageSet.Contains("ahs", StringComparison.OrdinalIgnoreCase))
                            {
                                assetSubtype = "AHS Image Set";
                            }
                            else if (imageSet.PathToImageSet.Contains("afi", StringComparison.OrdinalIgnoreCase))
                            {
                                assetSubtype = "AFI Image Set";
                            }

                            if (imageSet.ImageSetId.StartsWith("PKG", StringComparison.OrdinalIgnoreCase) || imageSet.ImageSetId.StartsWith("APG", StringComparison.OrdinalIgnoreCase))
                            {
                                assetSubtype = "Package Image Set";
                            }

                            classificationData.Add("Asset Subtype", assetSubtype);
                            classificationData.Add("Vendor Name", vendor);

                            var upsertsThatAreClassifications = upserts.Where(u => u.IsClassification).ToList();

                            var classificationUpserts = AprimoUpsertBuilder.BuildClassificationUpserts(classificationData, _aprimoClient, definitions, classifications, logOutput, languageId).ToList();

                            var upsertsReadyToApply = upserts.Where(u => !u.IsClassification).ToList();

                            foreach (var upsert in upsertsThatAreClassifications)
                            {
                                classificationData.Add(upsert.FieldLabel, upsert.RawValue);
                            }
                            classificationUpserts.AddRange(upsertsThatAreClassifications);

                            // there is no stamped data yet
                            List<AprimoFieldUpsert> classificationsToRemove = new List<AprimoFieldUpsert>();
                            _logger.LogInformation($"Attempting to Initial Stamp {created.Id}");
                            logOutput.Add($"Attempting to Intial Stamp {created.Id}");
                            await _aprimoClient.StampMetadataAsync(created.Id!, upsertsReadyToApply, new List<AprimoFieldUpsert>(), classificationUpserts, classificationsToRemove, cancellationToken);
                            return created.Id;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"Could not stamp asset with uniqueId {uniqueId}: {ex.Message}");
                            logOutput.Add($"Could not stamp asset with uniqueId {uniqueId}: {ex.Message}");
                            errors.Add($"Could not stamp asset with uniqueId {uniqueId}: : {ex.Message}");
                        }

                    }
                    else
                    {
                        _logger.LogInformation($"Null Id returned for created asset");
                        logOutput.Add($"Null Id returned for created asset.");
                        errors.Add($"Null Id returned for created asset. {uniqueId}");
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Whoooa. {ex.Message}");
                    logOutput.Add($"Whoooa. {ex.Message}");
                    errors.Add($"Whoooa. {ex.Message} | asset: {uniqueId}");
                }


            }
            return null;
        }


        public async Task ImportAemAssetsFromUploadSpreadsheet(CancellationToken cancellationToken)
        {
            ResetState();

            var logOutput = new List<string>();

            /****** CHECK THE SECRETS TO MAKE SURE YOU'RE PUSHING TO CORRECT ENV *******/


            ///// SETUP THE CORRECT PATHS FIRST /////
            //var allDelta3Files = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.Deltas3RootPrefix);
            //var allDelta3AssetFiles = allDelta3Files.Where(x => !x.EndsWith(".json") && !x.Contains("_renditions/")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allDelta3AssetFiles);
            //SaveStreamToFile(stream, SourceDirectory, "allDelta3AssetBlobs.csv");
            //stream.Dispose();
            //_logger.LogInformation($"Found {allDelta3AssetFiles.Count()} Delta3 Assets in Azure now");

            var assetBlobFolder = $"allDelta3AssetBlobs.csv";
            string importFromAzureFolder = $"{_azureOptions.Value.Deltas3RootPrefix}";
            string importFileName = "allDeltas3_Assets.xlsx";
            ///// SETUP THE CORRECT PATHS FIRST /////

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\{assetBlobFolder}");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");


            var fileData = await ReadImportSpreadsheet(importFileName, _azureOptions.Value.ImportsRootPrefix);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {importFileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {importFileName}");

            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");
            logOutput.Add($"found {classifications.Count()} classifications");

            var inlineProductImagery = classifications.Values
                .FirstOrDefault(c =>
                    c.Labels.Any(l =>
                        string.Equals(l.Value, "Inline Product Imagery",
                                      StringComparison.OrdinalIgnoreCase)));

            string classificationId = inlineProductImagery.Id;

            //var available = classifications.Values
            //    .FirstOrDefault(c =>
            //        c.Labels.Any(l =>
            //            string.Equals(l.Value, "Available",
            //                          StringComparison.OrdinalIgnoreCase)));

            //string availableId = available.Id;
            //"Available"


            foreach (var rowData in fileData)
            {
                var uuid = rowData["Id"];
                var path = rowData["Path"];

                var realFileName = Path.GetFileName(path);
                var assetFolder = $"{importFromAzureFolder}{Path.GetDirectoryName(path)}";
                string cleanedFilename = Regex.Replace(realFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();
                string azureFilename = $"{uuid}_{cleanedFilename}";

                if (TextQuality.ContainsBadCharacters(path))
                {
                    // since i didn't normalize the path, i won't be able to use this to get the blob
                    //  instead, ill just use my lookup to get the actual path
                    var blobFound = allAssetFiles.Where(x => x.Contains(uuid)).FirstOrDefault();
                    if (blobFound != null)
                    {
                        assetFolder = Path.GetDirectoryName(blobFound);
                        azureFilename = Path.GetFileName(blobFound);
                    } else
                    {
                        logOutput.Add($"Cannot find Blob! {path}");
                        _logger.LogInformation($"Cannot find Blob! {path}");
                    }
                }

                try
                {
                    var blobClient = await _assetsWrapper.GetBlobClientAsync(azureFilename, assetFolder);
                    bool hasPrimed = false;

                    bool recordExists = false;

                    //var assetRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(uuid, cancellationToken);
                    AprimoRecord assetRecord = null;
                    var assetRecords = await _aprimoClient.GetAssetsByAemAssetIdAsync(uuid, cancellationToken);
                    if (assetRecords.Count > 0)
                    {
                        if (assetRecords.Count > 1)
                        {
                            logOutput.Add($"Uh Oh! Found {assetRecords.Count} records matching uuid {uuid}");
                            _logger.LogInformation($"Uh Oh! Found {assetRecords.Count} records matching uuid {uuid}");
                            var ordered = assetRecords.OrderByDescending(r => r.ModifiedOn).ToList();
                            assetRecord = ordered.First();
                            var recordsToDelete = ordered.Skip(1).ToList();

                            foreach (var record in recordsToDelete)
                            {
                                var deletedRecord = await _aprimoClient.DeleteAssetByAprimoIdAsync(record.Id, cancellationToken);
                                logOutput.Add($"deleted record {record.Id} : {deletedRecord}");
                                _logger.LogInformation($"deleted record {record.Id} : {deletedRecord}");
                            }
                        }
                        else
                        {
                            assetRecord = assetRecords[0];
                        }
                    }

                    if (assetRecord != null)
                    {
                        recordExists = true;
                        rowData["AprimoId"] = assetRecord.Id;
                        LogRowData(true, rowData, "Success");
                        _logger.LogInformation($"Record already created for AEM Asset Id {uuid}.  Skip processing.");
                    }
                    else
                    {
                        _logger.LogInformation($"Record could not be found for AEM Asset Id {uuid}.  Process Record.");
                    }

                    if (!recordExists)
                    {
                        try
                        {
                            var created = await _aprimoClient.UploadAzureBlobToAprimoAsync(blobClient, cleanedFilename, classificationId, cancellationToken);

                            if (!string.IsNullOrWhiteSpace(created.Id))
                            {
                                rowData["AprimoId"] = created.Id;

                                // prime cache once 
                                if (!hasPrimed)
                                {
                                    await _aprimoClient.PrimeFieldIdCacheFromRecordAsync(created.Id!, cancellationToken);
                                    hasPrimed = true;
                                }


                                try
                                {
                                    // then stamp AEM Asset ID
                                    await _aprimoClient.StampAemAssetIdAsync(created.Id!, uuid, ct: cancellationToken);

                                    LogRowData(true, rowData, "Success");
                                }
                                catch (Exception ex)
                                {
                                    LogRowData(false, rowData, $"Could not stamp asset.");
                                }

                            }
                            else
                            {
                                LogRowData(false, rowData, $"Null Id returned for created asset.");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogRowData(false, rowData, $"{ex.Message}");
                        }
                    }


                }
                catch (Exception ex)
                {
                    LogRowData(false, rowData, $"{ex.Message}");
                }



            }

            SaveRowData();

            await LogToAzure(importFileName, logOutput);


            if (!importFileName.Contains(".processed"))
            {
                string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{importFileName}";
                await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
            }
        }

        public async Task ImportAemAssetsDirectlyFromAzure(CancellationToken cancellationToken)
        {
            ResetState();

            var logOutput = new List<string>();


            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");


            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");
            logOutput.Add($"found {classifications.Count()} classifications");

            var inlineProductImagery = classifications.Values
                .FirstOrDefault(c =>
                    c.Labels.Any(l =>
                        string.Equals(l.Value, "Inline Product Imagery",
                                      StringComparison.OrdinalIgnoreCase)));

            string classificationId = inlineProductImagery.Id;


            foreach (var file in allAssetFiles.Skip(995000))
            {
                string fileName = Path.GetFileName(file);
                string path = Path.GetDirectoryName(file);
                string[] parts = fileName.Split('_');

                string uuid = parts[0];
                string cleanedFilename = parts[1];

                logOutput.Add($"processing {uuid}");

                try
                {
                    var blobClient = await _assetsWrapper.GetBlobClientAsync(fileName, path);
                    bool hasPrimed = false;

                    bool recordExists = false;

                    //var assetRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(uuid, cancellationToken);
                    AprimoRecord assetRecord = null;
                    var assetRecords = await _aprimoClient.GetAssetsByAemAssetIdAsync(uuid, cancellationToken);
                    if (assetRecords.Count > 0)
                    {
                        if (assetRecords.Count > 1)
                        {
                            logOutput.Add($"Uh Oh! Found {assetRecords.Count} records matching uuid {uuid}");
                            _logger.LogInformation($"Uh Oh! Found {assetRecords.Count} records matching uuid {uuid}");
                            var ordered = assetRecords.OrderByDescending(r => r.ModifiedOn).ToList();
                            assetRecord = ordered.First();
                            var recordsToDelete = ordered.Skip(1).ToList();

                            foreach (var record in recordsToDelete)
                            {
                                var deletedRecord = await _aprimoClient.DeleteAssetByAprimoIdAsync(record.Id, cancellationToken);
                                logOutput.Add($"deleted record {record.Id} : {deletedRecord}");
                                _logger.LogInformation($"deleted record {record.Id} : {deletedRecord}");
                            }
                        }
                        else
                        {
                            assetRecord = assetRecords[0];
                        }
                    }

                    if (assetRecord != null)
                    {
                        recordExists = true;
                        _logger.LogInformation($"Record already created for AEM Asset Id {uuid}.  Skip processing.");
                    }
                    else
                    {
                        _logger.LogInformation($"Record could not be found for AEM Asset Id {uuid}.  Process Record.");
                    }

                    if (!recordExists)
                    {
                        try
                        {
                            var created = await _aprimoClient.UploadAzureBlobToAprimoAsync(blobClient, cleanedFilename, classificationId, cancellationToken);

                            if (!string.IsNullOrWhiteSpace(created.Id))
                            {
                                logOutput.Add($"created record {created.Id}");
                                // prime cache once 
                                if (!hasPrimed)
                                {
                                    await _aprimoClient.PrimeFieldIdCacheFromRecordAsync(created.Id!, cancellationToken);
                                    hasPrimed = true;
                                }


                                try
                                {
                                    // then stamp AEM Asset ID
                                    await _aprimoClient.StampAemAssetIdAsync(created.Id!, uuid, ct: cancellationToken);

                                    logOutput.Add($"stamped record {created.Id} for {uuid}");
                                }
                                catch (Exception ex)
                                {
                                    //LogRowData(false, rowData, $"Could not stamp asset.");
                                    logOutput.Add($"Could not stamp asset error {ex.Message} for {uuid}");
                                }

                            }
                            else
                            {
                                logOutput.Add($"Null Id returned for created asset for {uuid}");
                                //LogRowData(false, rowData, $"Null Id returned for created asset.");
                            }
                        }
                        catch (Exception ex)
                        {
                            logOutput.Add($"Unknown error {ex.Message} for {uuid}");
                            //LogRowData(false, rowData, $"{ex.Message}");
                        }
                    }


                }
                catch (Exception ex)
                {
                    logOutput.Add($"Unknown outer error {ex.Message} for {uuid}");
                    //LogRowData(false, rowData, $"{ex.Message}");
                }



            }

            //SaveRowData();

            await LogToAzure("azureRun.xlsx", logOutput);


        }

        public async Task ImportAemAssetDirectlyFromAzure(CancellationToken cancellationToken)
        {
            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");

            var inlineProductImagery = classifications.Values
                .FirstOrDefault(c =>
                    c.Labels.Any(l =>
                        string.Equals(l.Value, "Inline Product Imagery",
                                      StringComparison.OrdinalIgnoreCase)));

            string classificationId = inlineProductImagery.Id;

            string file = "assets/content/dam/cgi/digital-color-master-standard/laminates/781c13b6-8b50-44c7-9148-a0736a9e7576_10525861_PURE_DMS-A.tif";

                string fileName = Path.GetFileName(file);
                string path = Path.GetDirectoryName(file);
                string[] parts = fileName.Split('_');

                string uuid = parts[0];
                string cleanedFilename = "10525861_PURE_DMS-A.tif";

                _logger.LogInformation($"processing {uuid}");

                try
                {
                    var blobClient = await _assetsWrapper.GetBlobClientAsync(fileName, path);
                    bool hasPrimed = false;

                    bool recordExists = false;

                    //var assetRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(uuid, cancellationToken);
                    AprimoRecord assetRecord = null;
                    var assetRecords = await _aprimoClient.GetAssetsByAemAssetIdAsync(uuid, cancellationToken);
                    if (assetRecords.Count > 0)
                    {
                        if (assetRecords.Count > 1)
                        {
                            _logger.LogInformation($"Uh Oh! Found {assetRecords.Count} records matching uuid {uuid}");
                            _logger.LogInformation($"Uh Oh! Found {assetRecords.Count} records matching uuid {uuid}");
                            var ordered = assetRecords.OrderByDescending(r => r.ModifiedOn).ToList();
                            assetRecord = ordered.First();
                            var recordsToDelete = ordered.Skip(1).ToList();

                            foreach (var record in recordsToDelete)
                            {
                                var deletedRecord = await _aprimoClient.DeleteAssetByAprimoIdAsync(record.Id, cancellationToken);
                                _logger.LogInformation($"deleted record {record.Id} : {deletedRecord}");
                                _logger.LogInformation($"deleted record {record.Id} : {deletedRecord}");
                            }
                        }
                        else
                        {
                            assetRecord = assetRecords[0];
                        }
                    }

                    if (assetRecord != null)
                    {
                        recordExists = true;
                        _logger.LogInformation($"Record already created for AEM Asset Id {uuid}.  Skip processing.");
                    }
                    else
                    {
                        _logger.LogInformation($"Record could not be found for AEM Asset Id {uuid}.  Process Record.");
                    }

                    if (!recordExists)
                    {
                        try
                        {
                            var created = await _aprimoClient.UploadAzureBlobToAprimoAsync(blobClient, cleanedFilename, classificationId, cancellationToken);

                            if (!string.IsNullOrWhiteSpace(created.Id))
                            {
                                _logger.LogInformation($"created record {created.Id}");
                                // prime cache once 
                                if (!hasPrimed)
                                {
                                    await _aprimoClient.PrimeFieldIdCacheFromRecordAsync(created.Id!, cancellationToken);
                                    hasPrimed = true;
                                }


                                try
                                {
                                    // then stamp AEM Asset ID
                                    await _aprimoClient.StampAemAssetIdAsync(created.Id!, uuid, ct: cancellationToken);

                                    _logger.LogInformation($"stamped record {created.Id} for {uuid}");
                                }
                                catch (Exception ex)
                                {
                                    //LogRowData(false, rowData, $"Could not stamp asset.");
                                    _logger.LogInformation($"Could not stamp asset error {ex.Message} for {uuid}");
                                }

                            }
                            else
                            {
                                _logger.LogInformation($"Null Id returned for created asset for {uuid}");
                                //LogRowData(false, rowData, $"Null Id returned for created asset.");
                            }
                        }
                        catch (Exception ex)
                        {
                        _logger.LogInformation($"Unknown error {ex.Message} for {uuid}");
                            //LogRowData(false, rowData, $"{ex.Message}");
                        }
                    }


                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Unknown outer error {ex.Message} for {uuid}");
                    //LogRowData(false, rowData, $"{ex.Message}");
                }


        }

        public async Task ProcessAemAssetsFromStream(string fileName, Stream blobStream, CancellationToken cancellationToken)
        {
            ResetState();
            _logger.LogInformation($"Aprimo Options BaseUrl: {_aprimoOptions.Value.BaseUrl}");

            var logOutput = new List<string>();

            var fileData = await ReadImportSpreadsheet(blobStream);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {fileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {fileName}");

            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");
            logOutput.Add($"found {classifications.Count()} classifications");

            var inlineProductImagery = classifications.Values
                .FirstOrDefault(c =>
                    c.Labels.Any(l =>
                        string.Equals(l.Value, "Inline Product Imagery",
                                      StringComparison.OrdinalIgnoreCase)));

            string classificationId = inlineProductImagery.Id;


            foreach (var rowData in fileData)
            {
                var uuid = rowData["Id"];
                var path = rowData["Path"];
                var realFileName = Path.GetFileName(path);
                var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{Path.GetDirectoryName(path)}";
                //var assetMetadataFolder = $"{_azureOptions.Value.MetadataRootPrefix}/{Path.GetDirectoryName(path)}";
                string cleanedFilename = Regex.Replace(realFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();
                string azureFilename = $"{uuid}_{cleanedFilename}";

                try
                {
                    var blobClient = await _assetsWrapper.GetBlobClientAsync(azureFilename, assetFolder);
                    bool hasPrimed = false;

                    bool recordExists = false;

                    var assetRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(uuid, cancellationToken);
                    if (assetRecord != null)
                    {
                        recordExists = true;
                        rowData["AprimoId"] = assetRecord.Id;
                        LogRowData(true, rowData, "Success");
                        _logger.LogInformation($"Record already created for AEM Asset Id {uuid}.  Skip processing.");
                        await Task.Delay(500).ConfigureAwait(false); // prevent rate limit
                    }
                    else
                    {
                        _logger.LogInformation($"Record could not be found for AEM Asset Id {uuid}.  Process Record.");
                    }

                    if (!recordExists)
                    {
                        try
                        {
                            var created = await _aprimoClient.UploadAzureBlobToAprimoAsync(blobClient, realFileName, classificationId, cancellationToken);

                            if (!string.IsNullOrWhiteSpace(created.Id))
                            {
                                rowData["AprimoId"] = created.Id;

                                // prime cache once 
                                if (!hasPrimed)
                                {
                                    await _aprimoClient.PrimeFieldIdCacheFromRecordAsync(created.Id!, cancellationToken);
                                    hasPrimed = true;
                                }


                                try
                                {
                                    // then stamp AEM Asset ID
                                    await _aprimoClient.StampAemAssetIdAsync(created.Id!, uuid, ct: cancellationToken);

                                    LogRowData(true, rowData, "Success");
                                }
                                catch (Exception ex)
                                {
                                    LogRowData(false, rowData, $"Could not stamp asset.");
                                }

                            }
                            else
                            {
                                LogRowData(false, rowData, $"Null Id returned for created asset.");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogRowData(false, rowData, $"{ex.Message}");
                        }
                    }


                }
                catch (Exception ex)
                {
                    LogRowData(false, rowData, $"{ex.Message}");
                }



            }

            SaveRowData();

            await LogToAzure(fileName, logOutput);

        }


        public async Task StampAssetsInAprimo(CancellationToken cancellationToken)
        {
            ResetState();

            var logOutput = new List<string>();

            // get everything so we can do lookups
            //var allFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.AssetRootPrefix);
            //var allAssetFiles = allFiles.Where(x => !x.EndsWith(".json")).ToList();
            //MemoryStream stream = ConvertListToMemoryStream(allAssetFiles);
            //SaveStreamToFile(stream, SourceDirectory, "allAssetBlobs.csv");
            //stream.Dispose();

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");

            string importFileName = "aprimorestamp_allAssetsWithMetadata1_1_Test.xlsx";//"aprimorestamp_sampleAssetsWithMetadata_1.xlsx"; //

            // don't add aprimoId column to spreadsheets that already have it.
            //var fileData = await ReadImportSpreadsheet(importFileName, _azureOptions.Value.ImportsRootPrefix, skipAprimoIdColumns: true);
            var fileData = await ReadImportSpreadsheet(importFileName, _azureOptions.Value.ImportsRootPrefix);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {importFileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {importFileName}");

            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");
            logOutput.Add($"found {classifications.Count()} classifications");

            var definitions = await _aprimoClient.GetAllDefinitionsAsync(cancellationToken);
            _logger.LogInformation($"found {definitions.Count()} definitions");
            logOutput.Add($"found {definitions.Count()} definitions");

            //testing

            //var def = definitions.FirstOrDefault(d => string.Equals(d.Name, "isItemThirdPartyItem", StringComparison.OrdinalIgnoreCase));
            //var classification = classifications.Values
            //                        .FirstOrDefault(c =>
            //                            c.Id.Equals(def.RootId));

            //var labelClassification = classification.Embedded.Children.Items
            //    .FirstOrDefault(c =>
            //        c.Labels.Any(l =>
            //            string.Equals(l.Value, "True",
            //                          StringComparison.OrdinalIgnoreCase)));

            //if (labelClassification != null)
            //{
            //    _logger.LogInformation("all good");
            //}
            //else
            //{
            //    var nameClassification = classification.Embedded.Children.Items
            //        .FirstOrDefault(c =>
            //            c.Name.Contains("True",StringComparison.OrdinalIgnoreCase));


            //    ;
            //}

            // end testing

            // item status rules
            var itemStatusRules = FolderClassificationRuleLoader.LoadItemStatusRules();

            // Asset_Type Subtype Rule - folderMapping.json
            var folderRules = FolderClassificationRuleLoader.LoadFolderMapping();
            // Vendor Name and possible Subtype Rule - 3PVendorAndSubtypeMappping.json
            // NOTE: 
            var vendorSubtypeRuleSets = FolderClassificationRuleLoader.Load3PVendorAndSubtypeMapping();
            // 1P Subtype from FileName Rule - 1PSubtypeFromFileName.json
            var subfolderP1Rules = FolderClassificationRuleLoader.LoadP1Subtype();

            // spreadsheet indicates no need to do Product View rules per client

            var glbRules = FolderClassificationRuleLoader.LoadGLBProductCatRules();
            var studioPhotographyRules = FolderClassificationRuleLoader.LoadStudioPhotographyProductCatRules();
            var imagePlaceholderRules = FolderClassificationRuleLoader.LoadImagePlaceholderProductCatRules();
            var seriesFPORules = FolderClassificationRuleLoader.LoadSeriesFPOProductCatRules();
            var afiVideoRules = FolderClassificationRuleLoader.LoadAFIVideoProductCatRules();
            var ahsVideoRules = FolderClassificationRuleLoader.LoadAHSVideoProductCatRules();
            var cgiInlineFinishRules = FolderClassificationRuleLoader.LoadInlineFinishCGIRules();

            logOutput.Add($"loaded all classification rules");
            _logger.LogInformation($"loaded all classification rules");

            bool hasPrimed = false;
            foreach (var rowData in fileData)
            {
                var aprimoId = "";
                //var aprimoId = rowData["AprimoId"];
                //_logger.LogInformation($"Processing Record {aprimoId}.");

                var uuid = rowData["Id"];
                var path = rowData["Path"];
                var createdAEMDate = rowData["Created"];

                var realFileName = Path.GetFileName(path);
                var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{Path.GetDirectoryName(path)}";
                //var assetMetadataFolder = $"{_azureOptions.Value.MetadataRootPrefix}/{Path.GetDirectoryName(path)}";
                string cleanedFilename = Regex.Replace(realFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();
                string azureFilename = $"{uuid}_{cleanedFilename}";
                string azureMetadataFilename = $"{uuid}_metadata.json";
                string fullAzurePath = $"{assetFolder}/{cleanedFilename}";
                fullAzurePath = Normalize(fullAzurePath);
                logOutput.Add($"Processing {uuid} : {path}");
                logOutput.Add($"  matched with azure path {fullAzurePath}");
                _logger.LogInformation($"Processing {uuid} : {path}");
                try
                {
                    var blobClient = await _assetsWrapper.GetBlobClientAsync(azureFilename, assetFolder);

                    //var assetRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(uuid, cancellationToken);
                    AprimoRecord assetRecord = null;
                    var assetRecords = await _aprimoClient.GetAssetsByAemAssetIdAsync(uuid, cancellationToken);
                    if (assetRecords.Count > 0)
                    {
                        if (assetRecords.Count > 1)
                        {
                            logOutput.Add($"Uh Oh! Found {assetRecords.Count} records matching uuid {uuid}");
                            _logger.LogInformation($"Uh Oh! Found {assetRecords.Count} records matching uuid {uuid}");
                            var ordered = assetRecords.OrderByDescending(r => r.ModifiedOn).ToList();
                            assetRecord = ordered.First();
                            var recordsToDelete = ordered.Skip(1).ToList();

                            foreach (var record in recordsToDelete)
                            {
                                var deletedRecord = await _aprimoClient.DeleteAssetByAprimoIdAsync(record.Id, cancellationToken);
                                logOutput.Add($"deleted record {record.Id} : {deletedRecord}");
                                _logger.LogInformation($"deleted record {record.Id} : {deletedRecord}");
                            }
                        }
                        else
                        {
                            assetRecord = assetRecords[0];
                        }
                    }


                    //var assetRecord = await _aprimoClient.GetAssetByAprimoIdAsync(aprimoId, cancellationToken);
                    if (assetRecord != null)
                    {
                        logOutput.Add($"Found matching asset in Aprimo : {assetRecord.Id}");
                        _logger.LogInformation($"Found matching asset in Aprimo : {assetRecord.Id}");
                        rowData["AprimoId"] = assetRecord.Id;
                        aprimoId = rowData["AprimoId"];
                        // prime cache once 
                        if (!hasPrimed)
                        {
                            await _aprimoClient.EnsureLanguagesLoadedAsync(cancellationToken);
                            languageId = _aprimoClient.ResolveLanguageId(locale);
                            await _aprimoClient.PrimeFieldIdCacheFromRecordAsync(assetRecord.Id!, cancellationToken);

                            logOutput.Add($"Loaded languages and primed cache");
                            _logger.LogInformation($"Loaded languages and primed cache");
                            // configure output tables adding columns we missed before
                            var metadataFromExcel = AssetMetadataFactory.FromExcelRow(rowData);
                            var plan = AprimoMetadataMapper.GetStampPlan(metadataFromExcel);
                            var fromJsonSidecar = plan.FromJsonSidecar;

                            foreach (var (_, _, prop) in fromJsonSidecar)
                            {
                                var key = prop.Name;
                                _state.SuccessTable.Columns.Add(key);
                                _state.RetryTable.Columns.Add(key);
                            }

                            hasPrimed = true;
                        }

                        // just read the metadata file to get all the data.  there were added fields since i pulled the data into excel.
                        string json = await ReadJsonFile(azureMetadataFilename, assetFolder);
                        var metadataFromJson = JsonConvert.DeserializeObject<AssetMetadata>(json);

                        // handle ItemStatus -> Asset Status mapping
                        string assetStatus = string.Empty;
                        var itemStatusRule = FolderClassificationRuleLoader.GetItemStatusRule(itemStatusRules, metadataFromJson.ProductsItemStatus);
                        if (itemStatusRule != null)
                        {
                            assetStatus = itemStatusRule.AprimoValue;
                        }

                        // pop the created date time in there
                        metadataFromJson.ProductsAEMCreationDate = createdAEMDate;
                        string utcString = DateTime.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT+0000";
                        metadataFromJson.LastTouchedUTC = utcString; //DateTime.UtcNow.ToString("o");

                        var upserts = AprimoUpsertBuilder.BuildUpserts(metadataFromJson, _aprimoClient, definitions, classifications, logOutput, languageId);
                        logOutput.Add($"Found {upserts.Count()} metadata upserts.");
                        _logger.LogInformation($"Found {upserts.Count()} metadata upserts.");

                        // build classifications for type,subtype, vendor name, product category, and inline finish 
                        var rule = FolderClassificationRuleLoader.GetRuleForPath(folderRules, fullAzurePath);

                        string assetType = string.Empty;
                        string assetSubtype = string.Empty;
                        string category = string.Empty;
                        string inlineFinish = string.Empty;
                        string vendor = string.Empty;

                        if (rule == null)
                        {
                            // no match found
                            _logger.LogWarning($"No folder rule found for path {fullAzurePath}");
                            logOutput.Add($"No folder rule found for path {fullAzurePath}");
                        }
                        else if (rule.RequiresAdditionalLogic)
                        {
                            assetType = rule.AssetType; // Got this, but check for additional sub type logic.

                            logOutput.Add($"Found {assetType} from folder rule, but require additional logic for subType");
                            _logger.LogInformation($"Found {assetType} from folder rule, but require additional logic for subType.");

                            // branch to filename logic
                            if (rule.AemFolder.Equals("/content/dam/ashley-furniture/studiophotography"))
                            {
                                var subfolderRule = FolderClassificationRuleLoader.ResolveFromFileName(subfolderP1Rules, cleanedFilename);
                                assetSubtype = subfolderRule?.Subtype;
                            }

                            if (rule.AemFolder.Equals("/content/dam/ashley-furniture/3rdparty"))
                            {
                                var match = FolderClassificationRuleLoader.Resolve(vendorSubtypeRuleSets, fullAzurePath);

                                vendor = match?.VendorName;   // may be null for pure subtype rules like Swatch Bank

                                // NOTE: client has opted to not do 3P subtype rules.

                                //var subtype = match?.Subtype;     // may be null for vendor-only rows

                                //if (subtype != null)
                                //{
                                //    // only handle subtype
                                //    assetSubtype = subtype;
                                //}
                                //else if (vendor != null)
                                //{
                                //    string fileNameMinusExtension = Path.GetFileNameWithoutExtension(cleanedFilename);
                                //    // handle vendor as part of upsert metadata
                                //    if (fileNameMinusExtension.EndsWith("_1"))
                                //    {
                                //        subtype = "Lifestyle";
                                //    }
                                //    else if (fileNameMinusExtension.EndsWith("_2"))
                                //    {
                                //        subtype = "Sweep";
                                //    }
                                //    else if (fileNameMinusExtension.EndsWith("_")) // this should work even if images come in out of order.
                                //    {
                                //        // if file minus extension ends in _{somenumber}, look to see if _{somenumber+1} exists.  if not, then subtype = "swatch"

                                //        var nextFileInSeries = GetIncrementAfterLastUnderscore(fileNameMinusExtension);
                                //        if (nextFileInSeries != null)
                                //        {
                                //            // this was to search the azure blobs by tag, but probably more efficient to pull all blobs ahead of time (which i did)
                                //            //    var folderHash = FolderHash(assetFolder);
                                //            //    var query = $@"""folderHash"" = '{EscapeTagValue(folderHash)}' AND ""originalFilename"" = '{EscapeTagValue(actualFilename)}'";
                                //            //    var blobsFound = await _assetsWrapper.SearchBlobListingByTagsAsync(query, cancellationToken);

                                //            var actualFilename = ReplaceSuffixAfterLastUnderscore(fileNameMinusExtension, (int)nextFileInSeries);
                                //            var fileToSearchFor = $"{Path.GetDirectoryName(fullAzurePath)}/{actualFilename}";
                                //            var blobFound = allAssetFiles.Where(x => x.Contains(fileToSearchFor)).FirstOrDefault();

                                //            if (!string.IsNullOrEmpty(blobFound))
                                //            {
                                //                // don't stamp this as swatch as there are more in this set.
                                //            }
                                //            else
                                //            {
                                //                subtype = "Swatch";
                                //            }

                                //        }

                                //    }

                                //    assetSubtype = subtype;

                                //}
                            }
                        }
                        else
                        {
                            // stamp rule.AssetType + rule.AssetSubtype
                            assetType = rule.AssetType;
                            assetSubtype = rule.AssetSubtype;

                        }

                        // handle classification for product category
                        string ext = Path.GetExtension(cleanedFilename);

                        if (ext.ToLower().Equals(".glb"))
                        {
                            category = FolderClassificationRuleLoader.GetCategoryFromFilename(glbRules, cleanedFilename);
                        }
                        else if (path.Contains("/content/dam/ashley-furniture/studiophotography"))
                        {
                            var studioPhotographyrule = FolderClassificationRuleLoader.GetStudioPhotographyProductCategoryRule(studioPhotographyRules, fullAzurePath);

                            if (studioPhotographyrule?.AprimoProductCategory != null)
                            {
                                // build AprimoFieldUpsert for Product Category
                                category = studioPhotographyrule.AprimoProductCategory;
                            }
                            else if (studioPhotographyrule?.Flags?.NoMappingProvided == true)
                            {
                                // intentionally unmapped → log / skip / manual handling
                            }
                        }
                        else if (path.Contains("/content/dam/ashley-furniture/image_placeholders"))
                        {
                            var imagePlaceholderRule = FolderClassificationRuleLoader.GetImagePlaceholderRule(imagePlaceholderRules, fullAzurePath);

                            if (!string.IsNullOrWhiteSpace(imagePlaceholderRule?.AprimoProductCategory))
                            {
                                // create AprimoFieldUpsert
                                category = imagePlaceholderRule.AprimoProductCategory;
                            }
                        }
                        else if (path.Contains("/content/dam/ashley-furniture/series-fpo"))
                        {
                            var seriesFPORule = FolderClassificationRuleLoader.GetSeriesFPORule(seriesFPORules, fullAzurePath);

                            if (!string.IsNullOrWhiteSpace(seriesFPORule?.AprimoProductCategory))
                            {
                                // create AprimoFieldUpsert
                                category = seriesFPORule.AprimoProductCategory;
                            }
                        }
                        else if (path.Contains("/content/dam/ashley-furniture/video/afi_product_videos"))
                        {
                            var afiVideorule = FolderClassificationRuleLoader.GetAFIVideoProductCategoryRule(afiVideoRules, fullAzurePath);

                            if (afiVideorule?.AprimoProductCategory != null)
                            {
                                // build AprimoFieldUpsert for Product Category
                                category = afiVideorule.AprimoProductCategory;
                            }
                            else if (afiVideorule?.Flags?.NoMappingProvided == true)
                            {
                                // intentionally unmapped → log / skip / manual handling
                            }
                        }
                        else if (path.Contains("/content/dam/ashley-furniture/video/ahs_product_videos"))
                        {
                            var ahsVideorule = FolderClassificationRuleLoader.GetAHSVideoProductCategoryRule(ahsVideoRules, fullAzurePath);

                            if (ahsVideorule?.AprimoProductCategory != null)
                            {
                                // build AprimoFieldUpsert for Product Category
                                category = ahsVideorule.AprimoProductCategory;
                            }
                            else if (ahsVideorule?.Flags?.NoMappingProvided == true)
                            {
                                // intentionally unmapped → log / skip / manual handling
                            }
                        }

                        // inline finish

                        if (path.Contains("/content/dam/cgi/digital-color-master-standard"))
                        {
                            var cgiInlineFinishrule = FolderClassificationRuleLoader.GetInlineFinishCGIRule(cgiInlineFinishRules, fullAzurePath);

                            if (cgiInlineFinishrule?.AprimoInlineFinish != null)
                            {
                                // build AprimoFieldUpsert for Inline Finish
                                inlineFinish = cgiInlineFinishrule.AprimoInlineFinish;
                            }
                            else if (cgiInlineFinishrule?.Flags?.NoMappingProvided == true)
                            {
                                // intentionally unmapped → log / skip / manual handling
                            }
                        }

                        _logger.LogInformation($"after rules, found AssetType: {assetType}, Asset Subtype: {assetSubtype}, Product Category: {category}, Inline Finish: {inlineFinish}, Vendor: {vendor}, Asset Status: {assetStatus}");
                        logOutput.Add($"after rules, found AssetType: {assetType}, Asset Subtype: {assetSubtype}, Product Category: {category}, Inline Finish: {inlineFinish}, Vendor: {vendor}, Asset Status: {assetStatus}");

                        Dictionary<string, string> classificationData = new Dictionary<string, string>();
                        classificationData.Add("Asset Type", assetType);
                        classificationData.Add("Asset Subtype", assetSubtype);
                        classificationData.Add("Product Category", category);
                        classificationData.Add("Inline Finishes", inlineFinish);
                        classificationData.Add("Vendor Name", vendor);
                        classificationData.Add("Asset Status", assetStatus);

                        var upsertsThatAreClassifications = upserts.Where(u => u.IsClassification).ToList();

                        var classificationUpserts = AprimoUpsertBuilder.BuildClassificationUpserts(classificationData, _aprimoClient, definitions, classifications, logOutput, languageId).ToList();

                        var upsertsReadyToApply = upserts.Where(u => !u.IsClassification).ToList();

                        foreach (var upsert in upsertsThatAreClassifications)
                        {
                            classificationData.Add(upsert.FieldLabel, upsert.RawValue);
                        }
                        classificationUpserts.AddRange(upsertsThatAreClassifications);

                        // handle clean up of previously stamped data
                        List<AprimoFieldUpsert> classificationsToRemove = new List<AprimoFieldUpsert>();
                        foreach (var key in classificationData.Keys)
                        {
                            var currentValues = GetLocalizedValuesForField(assetRecord, key);
                            if (currentValues.Count > 0)  // we only need to worry about cleaning up data if it exists
                            {
                                if (string.IsNullOrWhiteSpace(classificationData[key]))
                                {
                                    // no value exists in this current stamping for this key. we need to remove all currently stamped values.
                                    foreach (var value in currentValues)
                                    {
                                        AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                        afu.Value = value;
                                        classificationsToRemove.Add(afu);
                                    }
                                }
                                else
                                {
                                    // a value exists in this current stamping.  
                                    // we need to remove any current values that are not already the one we are trying to stamp
                                    var classUpsert = classificationUpserts.Where(c => c.FieldLabel.Equals(key)).FirstOrDefault();
                                    if (classUpsert != null)
                                    {
                                        foreach (var value in currentValues)
                                        {
                                            if (!value.Equals(classUpsert.Value))
                                            {
                                                AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                                afu.Value = value;
                                                classificationsToRemove.Add(afu);
                                            }
                                        }
                                    }
                                }
                            }

                        }

                        var excludedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "Classification List",
                                "Date Time",
                                "Date",
                                "RecordLink"
                            };

                        var excludedFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "DisplayTitle",
                                "Description"
                            };

                        var fields = typeof(AssetMetadata)
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .SelectMany(p => p.GetCustomAttributes<AprimoFieldAttribute>(true)
                                .Select(attr => new { Prop = p, Attr = attr }))
                            .Where(x => !excludedTypes.Contains((x.Attr.DataType ?? "").Trim()) &&
                                !excludedFieldNames.Contains(x.Attr.FieldName))
                            .Select(x => new
                            {
                                PropertyName = x.Prop.Name,
                                AprimoName = x.Attr.FieldName,
                                AprimoType = x.Attr.DataType,
                                RawValue = x.Prop.GetValue(metadataFromJson),
                                Value = x.Prop.GetValue(metadataFromJson) switch
                                {
                                    null => null,
                                    string s => s,
                                    _ => x.Prop.GetValue(metadataFromJson)!.ToString()
                                }
                            })
                            .ToList();


                        List<AprimoFieldUpsert> fieldsToRemove = new List<AprimoFieldUpsert>();
                        foreach (var field in fields)
                        {
                            var currentValues = GetLocalizedValuesForFieldName(assetRecord, field.AprimoName);
                            if (currentValues.Count > 0)  // we only need to worry about cleaning up data if it exists
                            {
                                var aprimoField = assetRecord?.Embedded?.Fields?.Items?
                                    .FirstOrDefault(f => string.Equals(f.FieldName, field.AprimoName, StringComparison.OrdinalIgnoreCase) && !f.DataType.Equals("ClassificationList"));

                                bool isMulti = aprimoField.LocalizedValues[0].Values != null;

                                if (field.Value is not string s || string.IsNullOrWhiteSpace(s))
                                {
                                    // no value exists in this current stamping for this key. we need to remove all currently stamped values.
                                    foreach (var value in currentValues)
                                    {
                                        AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                        if (isMulti)
                                        {
                                            afu.Values.Add(value);

                                        }
                                        else
                                        {
                                            afu.Value = value;
                                        }

                                        afu.FieldId = aprimoField.Id;
                                        afu.LanguageId = languageId;
                                        fieldsToRemove.Add(afu);
                                    }
                                }
                                //else
                                //{
                                //    // a value exists in this current stamping.  
                                //    // we need to remove any current values that are not already the one we are trying to stamp
                                //    var fieldUpsert = upsertsReadyToApply.Where(c => c.FieldName.Equals(field.AprimoName)).FirstOrDefault();
                                //    if (fieldUpsert != null)
                                //    {
                                //        if(fieldUpsert.Values != null)
                                //        {
                                //            //bool areEqual = fieldUpsert.Values.Count == currentValues.Count && new HashSet<string>(fieldUpsert.Values).SetEquals(currentValues);
                                //            var difference = currentValues.Except(fieldUpsert.Values).ToList();
                                //            if (difference.Count > 0)
                                //            {
                                //                AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                //                afu.Values = difference;
                                //                afu.FieldId = aprimoField.Id;
                                //                afu.LanguageId = languageId;
                                //                fieldsToRemove.Add(afu);
                                //            }

                                //        }
                                //        else
                                //        {
                                //            foreach (var value in currentValues)
                                //            {

                                //                if (!value.Equals(fieldUpsert.Value))
                                //                {
                                //                    AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                //                    afu.Value = value;
                                //                    afu.FieldId = aprimoField.Id;
                                //                    afu.LanguageId = languageId;
                                //                    fieldsToRemove.Add(afu);
                                //                }
                                //            }
                                //        }
                                //    }
                                //}
                            }
                        }



                        await _aprimoClient.StampMetadataAsync(aprimoId, upsertsReadyToApply, fieldsToRemove,classificationUpserts, classificationsToRemove, cancellationToken);


                        // update the rowData for output
                        RowDataMetadataPopulator.PopulateExistingColumnsFromMetadata(rowData, metadataFromJson);

                        ;
                        LogRowData(true, rowData, "Success");
                        _logger.LogInformation($"Record {aprimoId} for AEM Asset Id {uuid} Has been stamped.");
                        logOutput.Add($"Record {aprimoId} for AEM Asset Id {uuid} Has been stamped.");

                    }
                    else
                    {
                        LogRowData(false, rowData, $"Record could not be found for Aprimo Record {aprimoId} / AEM Asset Id {uuid}. Skipping Record.");
                        _logger.LogInformation($"Record could not be found for Aprimo Record {aprimoId} / AEM Asset Id {uuid}. Skipping Record.");
                    }

                }
                catch (Exception ex)
                {
                    logOutput.Add($"Record {aprimoId} restamp failed: {ex.Message}");
                    LogRowData(false, rowData, $"{ex.Message}");
                }



            }

            SaveRowData();

            await LogToAzure(importFileName, logOutput);


            if (!importFileName.Contains(".processed"))
            {
                string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{importFileName}";
                await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
            }
        }

        public async Task StampAssetsInAprimoEnv(CancellationToken cancellationToken)
        {
            ResetState();

            var logOutput = new List<string>();

            string importFileName = "aprimorestamp_allAssetsInSB2.xlsx";//"aprimorestamp_sampleAssetsWithMetadata_1.xlsx"; //

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");

            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");
            logOutput.Add($"found {classifications.Count()} classifications");

            var definitions = await _aprimoClient.GetAllDefinitionsAsync(cancellationToken);
            _logger.LogInformation($"found {definitions.Count()} definitions");
            logOutput.Add($"found {definitions.Count()} definitions");

            // item status rules
            var itemStatusRules = FolderClassificationRuleLoader.LoadItemStatusRules();

            // Asset_Type Subtype Rule - folderMapping.json
            var folderRules = FolderClassificationRuleLoader.LoadFolderMapping();
            // Vendor Name and possible Subtype Rule - 3PVendorAndSubtypeMappping.json
            // NOTE: 
            var vendorSubtypeRuleSets = FolderClassificationRuleLoader.Load3PVendorAndSubtypeMapping();
            // 1P Subtype from FileName Rule - 1PSubtypeFromFileName.json
            var subfolderP1Rules = FolderClassificationRuleLoader.LoadP1Subtype();

            // spreadsheet indicates no need to do Product View rules per client

            var glbRules = FolderClassificationRuleLoader.LoadGLBProductCatRules();
            var studioPhotographyRules = FolderClassificationRuleLoader.LoadStudioPhotographyProductCatRules();
            var imagePlaceholderRules = FolderClassificationRuleLoader.LoadImagePlaceholderProductCatRules();
            var seriesFPORules = FolderClassificationRuleLoader.LoadSeriesFPOProductCatRules();
            var afiVideoRules = FolderClassificationRuleLoader.LoadAFIVideoProductCatRules();
            var ahsVideoRules = FolderClassificationRuleLoader.LoadAHSVideoProductCatRules();
            var cgiInlineFinishRules = FolderClassificationRuleLoader.LoadInlineFinishCGIRules();

            logOutput.Add($"loaded all classification rules");
            _logger.LogInformation($"loaded all classification rules");

            var allRecords = await _aprimoClient.GetAllAssetsAsync(cancellationToken);

            _logger.LogInformation($"Found {allRecords.Count()} Aprimo Records");
            logOutput.Add($"Found {allRecords.Count()} Aprimo Records");

            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjectsFlat");

            bool hasPrimed = false;
            bool hasStarted = true;
            int recordCounter = 0;
            foreach (var assetRecord in allRecords)
            {
                var aprimoId = assetRecord.Id;



                var vals = GetLocalizedValuesForFieldName(assetRecord, "productsAEMAssetID");
                string uuid = string.Empty;
                if (vals.Count() > 0)
                {
                    uuid = vals[0];
                }

                if (!string.IsNullOrEmpty(uuid))
                {
                    var dateStamped = GetLocalizedValuesForFieldName(assetRecord, "lastTouchedUTC");
                    bool hasStamped = false;
                    if (dateStamped.Count() > 0)
                    {
                        if (DateTime.TryParse(dateStamped[0], out var parsedDate))
                        {
                            DateTime todayAt9 = DateTime.Today.AddHours(9);

                            if (parsedDate < todayAt9)
                            {
                                // before 9am
                            }
                            else
                            {
                                // 9am or later
                                hasStamped = true;
                            }
                        }
                    }

                    if (!hasStamped) 
                    {
                        try
                        {
                            var mhoRecord = await mhoRepo.GetByAemAssetIdAsync(uuid);

                            var path = mhoRecord.AemAssetPath;
                            var createdAEMDate = mhoRecord.AemCreatedDate;

                            var realFileName = Path.GetFileName(path);
                            var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{Path.GetDirectoryName(path)}";
                            //var assetMetadataFolder = $"{_azureOptions.Value.MetadataRootPrefix}/{Path.GetDirectoryName(path)}";
                            string cleanedFilename = Regex.Replace(realFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();
                            string azureFilename = $"{uuid}_{cleanedFilename}";
                            string azureMetadataFilename = $"{uuid}_metadata.json";
                            string fullAzurePath = $"{assetFolder}/{cleanedFilename}";
                            fullAzurePath = Normalize(fullAzurePath);
                            logOutput.Add($"Processing {uuid} : {path}");
                            logOutput.Add($"  matched with azure path {fullAzurePath}");
                            _logger.LogInformation($"Processing {uuid} : {path}");
                            try
                            {
                                //var assetRecord = await _aprimoClient.GetAssetByAprimoIdAsync(aprimoId, cancellationToken);
                                if (assetRecord != null)
                                {
                                    logOutput.Add($"Found matching asset in Aprimo : {assetRecord.Id}");
                                    _logger.LogInformation($"Found matching asset in Aprimo : {assetRecord.Id}");

                                    // prime cache once 
                                    if (!hasPrimed)
                                    {
                                        await _aprimoClient.EnsureLanguagesLoadedAsync(cancellationToken);
                                        languageId = _aprimoClient.ResolveLanguageId(locale);
                                        await _aprimoClient.PrimeFieldIdCacheFromRecordAsync(assetRecord.Id!, cancellationToken);

                                        logOutput.Add($"Loaded languages and primed cache");
                                        _logger.LogInformation($"Loaded languages and primed cache");


                                        hasPrimed = true;
                                    }

                                    // just read the metadata file to get all the data.  there were added fields since i pulled the data into excel.
                                    string json = await ReadJsonFile(azureMetadataFilename, assetFolder);
                                    var metadataFromJson = JsonConvert.DeserializeObject<AssetMetadata>(json);

                                    // handle ItemStatus -> Asset Status mapping
                                    string assetStatus = string.Empty;
                                    var itemStatusRule = FolderClassificationRuleLoader.GetItemStatusRule(itemStatusRules, metadataFromJson.ProductsItemStatus);
                                    if (itemStatusRule != null)
                                    {
                                        assetStatus = itemStatusRule.AprimoValue;
                                    }

                                    // pop the created date time in there
                                    metadataFromJson.ProductsAEMCreationDate = createdAEMDate;
                                    string utcString = DateTime.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT+0000";
                                    metadataFromJson.LastTouchedUTC = utcString; //DateTime.UtcNow.ToString("o");

                                    var upserts = AprimoUpsertBuilder.BuildUpserts(metadataFromJson, _aprimoClient, definitions, classifications, logOutput, languageId);
                                    logOutput.Add($"Found {upserts.Count()} metadata upserts.");
                                    _logger.LogInformation($"Found {upserts.Count()} metadata upserts.");

                                    // build classifications for type,subtype, vendor name, product category, and inline finish 
                                    var rule = FolderClassificationRuleLoader.GetRuleForPath(folderRules, fullAzurePath);

                                    string assetType = string.Empty;
                                    string assetSubtype = string.Empty;
                                    string category = string.Empty;
                                    string inlineFinish = string.Empty;
                                    string vendor = string.Empty;

                                    if (rule == null)
                                    {
                                        // no match found
                                        _logger.LogWarning($"No folder rule found for path {fullAzurePath}");
                                        logOutput.Add($"No folder rule found for path {fullAzurePath}");
                                    }
                                    else if (rule.RequiresAdditionalLogic)
                                    {
                                        assetType = rule.AssetType; // Got this, but check for additional sub type logic.

                                        logOutput.Add($"Found {assetType} from folder rule, but require additional logic for subType");
                                        _logger.LogInformation($"Found {assetType} from folder rule, but require additional logic for subType.");

                                        // branch to filename logic
                                        if (rule.AemFolder.Equals("/content/dam/ashley-furniture/studiophotography"))
                                        {
                                            var subfolderRule = FolderClassificationRuleLoader.ResolveFromFileName(subfolderP1Rules, cleanedFilename);
                                            assetSubtype = subfolderRule?.Subtype;
                                        }

                                        if (rule.AemFolder.Equals("/content/dam/ashley-furniture/3rdparty"))
                                        {
                                            var match = FolderClassificationRuleLoader.Resolve(vendorSubtypeRuleSets, fullAzurePath);

                                            vendor = match?.VendorName;   // may be null for pure subtype rules like Swatch Bank

                                            // NOTE: client has opted to not do 3P subtype rules.

                                        }
                                    }
                                    else
                                    {
                                        // stamp rule.AssetType + rule.AssetSubtype
                                        assetType = rule.AssetType;
                                        assetSubtype = rule.AssetSubtype;

                                    }

                                    // handle classification for product category
                                    string ext = Path.GetExtension(cleanedFilename);

                                    if (ext.ToLower().Equals(".glb"))
                                    {
                                        category = FolderClassificationRuleLoader.GetCategoryFromFilename(glbRules, cleanedFilename);
                                    }
                                    else if (path.Contains("/content/dam/ashley-furniture/studiophotography"))
                                    {
                                        var studioPhotographyrule = FolderClassificationRuleLoader.GetStudioPhotographyProductCategoryRule(studioPhotographyRules, fullAzurePath);

                                        if (studioPhotographyrule?.AprimoProductCategory != null)
                                        {
                                            // build AprimoFieldUpsert for Product Category
                                            category = studioPhotographyrule.AprimoProductCategory;
                                        }
                                        else if (studioPhotographyrule?.Flags?.NoMappingProvided == true)
                                        {
                                            // intentionally unmapped → log / skip / manual handling
                                        }
                                    }
                                    else if (path.Contains("/content/dam/ashley-furniture/image_placeholders"))
                                    {
                                        var imagePlaceholderRule = FolderClassificationRuleLoader.GetImagePlaceholderRule(imagePlaceholderRules, fullAzurePath);

                                        if (!string.IsNullOrWhiteSpace(imagePlaceholderRule?.AprimoProductCategory))
                                        {
                                            // create AprimoFieldUpsert
                                            category = imagePlaceholderRule.AprimoProductCategory;
                                        }
                                    }
                                    else if (path.Contains("/content/dam/ashley-furniture/series-fpo"))
                                    {
                                        var seriesFPORule = FolderClassificationRuleLoader.GetSeriesFPORule(seriesFPORules, fullAzurePath);

                                        if (!string.IsNullOrWhiteSpace(seriesFPORule?.AprimoProductCategory))
                                        {
                                            // create AprimoFieldUpsert
                                            category = seriesFPORule.AprimoProductCategory;
                                        }
                                    }
                                    else if (path.Contains("/content/dam/ashley-furniture/video/afi_product_videos"))
                                    {
                                        var afiVideorule = FolderClassificationRuleLoader.GetAFIVideoProductCategoryRule(afiVideoRules, fullAzurePath);

                                        if (afiVideorule?.AprimoProductCategory != null)
                                        {
                                            // build AprimoFieldUpsert for Product Category
                                            category = afiVideorule.AprimoProductCategory;
                                        }
                                        else if (afiVideorule?.Flags?.NoMappingProvided == true)
                                        {
                                            // intentionally unmapped → log / skip / manual handling
                                        }
                                    }
                                    else if (path.Contains("/content/dam/ashley-furniture/video/ahs_product_videos"))
                                    {
                                        var ahsVideorule = FolderClassificationRuleLoader.GetAHSVideoProductCategoryRule(ahsVideoRules, fullAzurePath);

                                        if (ahsVideorule?.AprimoProductCategory != null)
                                        {
                                            // build AprimoFieldUpsert for Product Category
                                            category = ahsVideorule.AprimoProductCategory;
                                        }
                                        else if (ahsVideorule?.Flags?.NoMappingProvided == true)
                                        {
                                            // intentionally unmapped → log / skip / manual handling
                                        }
                                    }

                                    // inline finish

                                    if (path.Contains("/content/dam/cgi/digital-color-master-standard"))
                                    {
                                        var cgiInlineFinishrule = FolderClassificationRuleLoader.GetInlineFinishCGIRule(cgiInlineFinishRules, fullAzurePath);

                                        if (cgiInlineFinishrule?.AprimoInlineFinish != null)
                                        {
                                            // build AprimoFieldUpsert for Inline Finish
                                            inlineFinish = cgiInlineFinishrule.AprimoInlineFinish;
                                        }
                                        else if (cgiInlineFinishrule?.Flags?.NoMappingProvided == true)
                                        {
                                            // intentionally unmapped → log / skip / manual handling
                                        }
                                    }

                                    _logger.LogInformation($"after rules, found AssetType: {assetType}, Asset Subtype: {assetSubtype}, Product Category: {category}, Inline Finish: {inlineFinish}, Vendor: {vendor}, Asset Status: {assetStatus}");
                                    logOutput.Add($"after rules, found AssetType: {assetType}, Asset Subtype: {assetSubtype}, Product Category: {category}, Inline Finish: {inlineFinish}, Vendor: {vendor}, Asset Status: {assetStatus}");

                                    Dictionary<string, string> classificationData = new Dictionary<string, string>();
                                    classificationData.Add("Asset Type", assetType);
                                    classificationData.Add("Asset Subtype", assetSubtype);
                                    classificationData.Add("Product Category", category);
                                    classificationData.Add("Inline Finishes", inlineFinish);
                                    classificationData.Add("Vendor Name", vendor);
                                    classificationData.Add("Asset Status", assetStatus);

                                    var upsertsThatAreClassifications = upserts.Where(u => u.IsClassification).ToList();

                                    var classificationUpserts = AprimoUpsertBuilder.BuildClassificationUpserts(classificationData, _aprimoClient, definitions, classifications, logOutput, languageId).ToList();

                                    var upsertsReadyToApply = upserts.Where(u => !u.IsClassification).ToList();

                                    foreach (var upsert in upsertsThatAreClassifications)
                                    {
                                        classificationData.Add(upsert.FieldLabel, upsert.RawValue);
                                    }
                                    classificationUpserts.AddRange(upsertsThatAreClassifications);

                                    // handle clean up of previously stamped data
                                    List<AprimoFieldUpsert> classificationsToRemove = new List<AprimoFieldUpsert>();
                                    foreach (var key in classificationData.Keys)
                                    {
                                        var currentValues = GetLocalizedValuesForField(assetRecord, key);
                                        if (currentValues.Count > 0)  // we only need to worry about cleaning up data if it exists
                                        {
                                            if (string.IsNullOrWhiteSpace(classificationData[key]))
                                            {
                                                // no value exists in this current stamping for this key. we need to remove all currently stamped values.
                                                foreach (var value in currentValues)
                                                {
                                                    AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                                    afu.Value = value;
                                                    classificationsToRemove.Add(afu);
                                                }
                                            }
                                            else
                                            {
                                                // a value exists in this current stamping.  
                                                // we need to remove any current values that are not already the one we are trying to stamp
                                                var classUpsert = classificationUpserts.Where(c => c.FieldLabel.Equals(key)).FirstOrDefault();
                                                if (classUpsert != null)
                                                {
                                                    foreach (var value in currentValues)
                                                    {
                                                        if (!value.Equals(classUpsert.Value))
                                                        {
                                                            AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                                            afu.Value = value;
                                                            classificationsToRemove.Add(afu);
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                    }

                            //        var excludedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            //{
                            //    "Classification List",
                            //    "Date Time",
                            //    "Date",
                            //    "RecordLink"
                            //};

                            //        var excludedFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            //{
                            //    "DisplayTitle",
                            //    "Description"
                            //};

                            //        var fields = typeof(AssetMetadata)
                            //            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            //            .SelectMany(p => p.GetCustomAttributes<AprimoFieldAttribute>(true)
                            //                .Select(attr => new { Prop = p, Attr = attr }))
                            //            .Where(x => !excludedTypes.Contains((x.Attr.DataType ?? "").Trim()) &&
                            //                !excludedFieldNames.Contains(x.Attr.FieldName))
                            //            .Select(x => new
                            //            {
                            //                PropertyName = x.Prop.Name,
                            //                AprimoName = x.Attr.FieldName,
                            //                AprimoType = x.Attr.DataType,
                            //                RawValue = x.Prop.GetValue(metadataFromJson),
                            //                Value = x.Prop.GetValue(metadataFromJson) switch
                            //                {
                            //                    null => null,
                            //                    string s => s,
                            //                    _ => x.Prop.GetValue(metadataFromJson)!.ToString()
                            //                }
                            //            })
                            //            .ToList();


                            //        List<AprimoFieldUpsert> fieldsToRemove = new List<AprimoFieldUpsert>();
                            //        foreach (var field in fields)
                            //        {
                            //            var currentValues = GetLocalizedValuesForFieldName(assetRecord, field.AprimoName);
                            //            if (currentValues.Count > 0)  // we only need to worry about cleaning up data if it exists
                            //            {
                            //                var aprimoField = assetRecord?.Embedded?.Fields?.Items?
                            //                    .FirstOrDefault(f => string.Equals(f.FieldName, field.AprimoName, StringComparison.OrdinalIgnoreCase) && !f.DataType.Equals("ClassificationList"));

                            //                bool isMulti = aprimoField.LocalizedValues[0].Values != null;

                            //                if (field.Value is not string s || string.IsNullOrWhiteSpace(s))
                            //                {
                            //                    // no value exists in this current stamping for this key. we need to remove all currently stamped values.
                            //                    foreach (var value in currentValues)
                            //                    {
                            //                        AprimoFieldUpsert afu = new AprimoFieldUpsert();
                            //                        if (isMulti)
                            //                        {
                            //                            afu.Values.Add(value);

                            //                        }
                            //                        else
                            //                        {
                            //                            afu.Value = value;
                            //                        }

                            //                        var hasIdAlready = upsertsReadyToApply.Where(x => x.FieldId == aprimoField.Id).FirstOrDefault();
                            //                        if (hasIdAlready == null)
                            //                        {
                            //                            afu.FieldId = aprimoField.Id;
                            //                            afu.LanguageId = languageId;
                            //                            fieldsToRemove.Add(afu);
                            //                        }

                            //                    }
                            //                }
                            //                //else
                            //                //{
                            //                //    // a value exists in this current stamping.  
                            //                //    // we need to remove any current values that are not already the one we are trying to stamp
                            //                //    var fieldUpsert = upsertsReadyToApply.Where(c => c.FieldName.Equals(field.AprimoName)).FirstOrDefault();
                            //                //    if (fieldUpsert != null)
                            //                //    {
                            //                //        if(fieldUpsert.Values != null)
                            //                //        {
                            //                //            //bool areEqual = fieldUpsert.Values.Count == currentValues.Count && new HashSet<string>(fieldUpsert.Values).SetEquals(currentValues);
                            //                //            var difference = currentValues.Except(fieldUpsert.Values).ToList();
                            //                //            if (difference.Count > 0)
                            //                //            {
                            //                //                AprimoFieldUpsert afu = new AprimoFieldUpsert();
                            //                //                afu.Values = difference;
                            //                //                afu.FieldId = aprimoField.Id;
                            //                //                afu.LanguageId = languageId;
                            //                //                fieldsToRemove.Add(afu);
                            //                //            }

                            //                //        }
                            //                //        else
                            //                //        {
                            //                //            foreach (var value in currentValues)
                            //                //            {

                            //                //                if (!value.Equals(fieldUpsert.Value))
                            //                //                {
                            //                //                    AprimoFieldUpsert afu = new AprimoFieldUpsert();
                            //                //                    afu.Value = value;
                            //                //                    afu.FieldId = aprimoField.Id;
                            //                //                    afu.LanguageId = languageId;
                            //                //                    fieldsToRemove.Add(afu);
                            //                //                }
                            //                //            }
                            //                //        }
                            //                //    }
                            //                //}
                            //            }
                            //        }

                                    // TODO: deal with fieldsToRemove?

                                    await _aprimoClient.StampMetadataAsync(aprimoId, upsertsReadyToApply, new List<AprimoFieldUpsert>(), classificationUpserts, classificationsToRemove, cancellationToken);


                                    _logger.LogInformation($"Record {aprimoId} for AEM Asset Id {uuid} Has been stamped.");
                                    logOutput.Add($"Record {aprimoId} for AEM Asset Id {uuid} Has been stamped.");

                                }
                                else
                                {
                                    _logger.LogInformation($"Record could not be found for Aprimo Record {aprimoId} / AEM Asset Id {uuid}. Skipping Record.");
                                    logOutput.Add($"Record could not be found for Aprimo Record {aprimoId} / AEM Asset Id {uuid}. Skipping Record.");
                                }

                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"Record {aprimoId} restamp failed: {ex.Message}");
                                logOutput.Add($"Record {aprimoId} restamp failed: {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"Could not get MHO Record for {uuid} : {aprimoId} restamp failed: {ex.Message}");
                            logOutput.Add($"Could not get MHO Record for {uuid} : {aprimoId} restamp failed: {ex.Message}");
                        }
                    }


                } else
                {
                    _logger.LogInformation($"Record {aprimoId} does not have AEM Asset Id {uuid}. Skipping Record.");
                    logOutput.Add($"Record {aprimoId} does not have AEM Asset Id {uuid}. Skipping Record.");
                }
            }


            await LogToAzure(importFileName, logOutput);

        }

        public async Task RetryStampAssetsInAprimoEnv(CancellationToken cancellationToken)
        {
            ResetState();

            var logOutput = new List<string>();

            string importFileName = "aprimorestamp_allAssetsInSB2.xlsx";//"aprimorestamp_sampleAssetsWithMetadata_1.xlsx"; //

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");

            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");
            logOutput.Add($"found {classifications.Count()} classifications");

            var definitions = await _aprimoClient.GetAllDefinitionsAsync(cancellationToken);
            _logger.LogInformation($"found {definitions.Count()} definitions");
            logOutput.Add($"found {definitions.Count()} definitions");

            // item status rules
            var itemStatusRules = FolderClassificationRuleLoader.LoadItemStatusRules();

            // Asset_Type Subtype Rule - folderMapping.json
            var folderRules = FolderClassificationRuleLoader.LoadFolderMapping();
            // Vendor Name and possible Subtype Rule - 3PVendorAndSubtypeMappping.json
            // NOTE: 
            var vendorSubtypeRuleSets = FolderClassificationRuleLoader.Load3PVendorAndSubtypeMapping();
            // 1P Subtype from FileName Rule - 1PSubtypeFromFileName.json
            var subfolderP1Rules = FolderClassificationRuleLoader.LoadP1Subtype();

            // spreadsheet indicates no need to do Product View rules per client

            var glbRules = FolderClassificationRuleLoader.LoadGLBProductCatRules();
            var studioPhotographyRules = FolderClassificationRuleLoader.LoadStudioPhotographyProductCatRules();
            var imagePlaceholderRules = FolderClassificationRuleLoader.LoadImagePlaceholderProductCatRules();
            var seriesFPORules = FolderClassificationRuleLoader.LoadSeriesFPOProductCatRules();
            var afiVideoRules = FolderClassificationRuleLoader.LoadAFIVideoProductCatRules();
            var ahsVideoRules = FolderClassificationRuleLoader.LoadAHSVideoProductCatRules();
            var cgiInlineFinishRules = FolderClassificationRuleLoader.LoadInlineFinishCGIRules();

            logOutput.Add($"loaded all classification rules");
            _logger.LogInformation($"loaded all classification rules");

            //var allRecords = await _aprimoClient.GetAllAssetsAsync(cancellationToken);
            var allFailedRecords = File.ReadAllLines($"{SourceDirectory}\\retrySB2.txt");

            _logger.LogInformation($"Found {allFailedRecords.Count()} Failed Aprimo Records");

            List<AprimoRecord> allRecords = new List<AprimoRecord>();
            foreach (var recId in allFailedRecords)
            {
                if (!string.IsNullOrEmpty(recId))
                {
                    var record = await _aprimoClient.GetAssetByAprimoIdAsync(recId.Trim());
                    allRecords.Add(record);
                }
            }



            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjectsFlat");

            bool hasPrimed = false;
            bool hasStarted = true;
            foreach (var assetRecord in allRecords)
            {
                var aprimoId = assetRecord.Id;



                var vals = GetLocalizedValuesForFieldName(assetRecord, "productsAEMAssetID");
                string uuid = string.Empty;
                if (vals.Count() > 0)
                {
                    uuid = vals[0];
                }

                if (!string.IsNullOrEmpty(uuid))
                {
                    var dateStamped = GetLocalizedValuesForFieldName(assetRecord, "lastTouchedUTC");
                    bool hasStamped = false;
                    if (dateStamped.Count() > 0)
                    {
                        if (DateTime.TryParse(dateStamped[0], out var parsedDate))
                        {
                            DateTime todayAt9 = DateTime.Today.AddHours(9);

                            if (parsedDate < todayAt9)
                            {
                                // before 9am
                            }
                            else
                            {
                                // 9am or later
                                hasStamped = true;
                            }
                        }
                    }

                    if (!hasStamped)
                    {
                        try
                        {
                            var mhoRecord = await mhoRepo.GetByAemAssetIdAsync(uuid);

                            var path = mhoRecord.AemAssetPath;
                            var createdAEMDate = mhoRecord.AemCreatedDate;

                            var realFileName = Path.GetFileName(path);
                            var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{Path.GetDirectoryName(path)}";
                            //var assetMetadataFolder = $"{_azureOptions.Value.MetadataRootPrefix}/{Path.GetDirectoryName(path)}";
                            string cleanedFilename = Regex.Replace(realFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();
                            string azureFilename = $"{uuid}_{cleanedFilename}";
                            string azureMetadataFilename = $"{uuid}_metadata.json";
                            string fullAzurePath = $"{assetFolder}/{cleanedFilename}";
                            fullAzurePath = Normalize(fullAzurePath);
                            logOutput.Add($"Processing {uuid} : {path}");
                            logOutput.Add($"  matched with azure path {fullAzurePath}");
                            _logger.LogInformation($"Processing {uuid} : {path}");
                            try
                            {
                                //var assetRecord = await _aprimoClient.GetAssetByAprimoIdAsync(aprimoId, cancellationToken);
                                if (assetRecord != null)
                                {
                                    logOutput.Add($"Found matching asset in Aprimo : {assetRecord.Id}");
                                    _logger.LogInformation($"Found matching asset in Aprimo : {assetRecord.Id}");

                                    // prime cache once 
                                    if (!hasPrimed)
                                    {
                                        await _aprimoClient.EnsureLanguagesLoadedAsync(cancellationToken);
                                        languageId = _aprimoClient.ResolveLanguageId(locale);
                                        await _aprimoClient.PrimeFieldIdCacheFromRecordAsync(assetRecord.Id!, cancellationToken);

                                        logOutput.Add($"Loaded languages and primed cache");
                                        _logger.LogInformation($"Loaded languages and primed cache");


                                        hasPrimed = true;
                                    }

                                    // just read the metadata file to get all the data.  there were added fields since i pulled the data into excel.
                                    string json = await ReadJsonFile(azureMetadataFilename, assetFolder);
                                    var metadataFromJson = JsonConvert.DeserializeObject<AssetMetadata>(json);

                                    // handle ItemStatus -> Asset Status mapping
                                    string assetStatus = string.Empty;
                                    var itemStatusRule = FolderClassificationRuleLoader.GetItemStatusRule(itemStatusRules, metadataFromJson.ProductsItemStatus);
                                    if (itemStatusRule != null)
                                    {
                                        assetStatus = itemStatusRule.AprimoValue;
                                    }

                                    // pop the created date time in there
                                    metadataFromJson.ProductsAEMCreationDate = createdAEMDate;
                                    string utcString = DateTime.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT+0000";
                                    metadataFromJson.LastTouchedUTC = utcString; //DateTime.UtcNow.ToString("o");

                                    var upserts = AprimoUpsertBuilder.BuildUpserts(metadataFromJson, _aprimoClient, definitions, classifications, logOutput, languageId);
                                    logOutput.Add($"Found {upserts.Count()} metadata upserts.");
                                    _logger.LogInformation($"Found {upserts.Count()} metadata upserts.");

                                    // build classifications for type,subtype, vendor name, product category, and inline finish 
                                    var rule = FolderClassificationRuleLoader.GetRuleForPath(folderRules, fullAzurePath);

                                    string assetType = string.Empty;
                                    string assetSubtype = string.Empty;
                                    string category = string.Empty;
                                    string inlineFinish = string.Empty;
                                    string vendor = string.Empty;

                                    if (rule == null)
                                    {
                                        // no match found
                                        _logger.LogWarning($"No folder rule found for path {fullAzurePath}");
                                        logOutput.Add($"No folder rule found for path {fullAzurePath}");
                                    }
                                    else if (rule.RequiresAdditionalLogic)
                                    {
                                        assetType = rule.AssetType; // Got this, but check for additional sub type logic.

                                        logOutput.Add($"Found {assetType} from folder rule, but require additional logic for subType");
                                        _logger.LogInformation($"Found {assetType} from folder rule, but require additional logic for subType.");

                                        // branch to filename logic
                                        if (rule.AemFolder.Equals("/content/dam/ashley-furniture/studiophotography"))
                                        {
                                            var subfolderRule = FolderClassificationRuleLoader.ResolveFromFileName(subfolderP1Rules, cleanedFilename);
                                            assetSubtype = subfolderRule?.Subtype;
                                        }

                                        if (rule.AemFolder.Equals("/content/dam/ashley-furniture/3rdparty"))
                                        {
                                            var match = FolderClassificationRuleLoader.Resolve(vendorSubtypeRuleSets, fullAzurePath);

                                            vendor = match?.VendorName;   // may be null for pure subtype rules like Swatch Bank

                                            // NOTE: client has opted to not do 3P subtype rules.

                                        }
                                    }
                                    else
                                    {
                                        // stamp rule.AssetType + rule.AssetSubtype
                                        assetType = rule.AssetType;
                                        assetSubtype = rule.AssetSubtype;

                                    }

                                    // handle classification for product category
                                    string ext = Path.GetExtension(cleanedFilename);

                                    if (ext.ToLower().Equals(".glb"))
                                    {
                                        category = FolderClassificationRuleLoader.GetCategoryFromFilename(glbRules, cleanedFilename);
                                    }
                                    else if (path.Contains("/content/dam/ashley-furniture/studiophotography"))
                                    {
                                        var studioPhotographyrule = FolderClassificationRuleLoader.GetStudioPhotographyProductCategoryRule(studioPhotographyRules, fullAzurePath);

                                        if (studioPhotographyrule?.AprimoProductCategory != null)
                                        {
                                            // build AprimoFieldUpsert for Product Category
                                            category = studioPhotographyrule.AprimoProductCategory;
                                        }
                                        else if (studioPhotographyrule?.Flags?.NoMappingProvided == true)
                                        {
                                            // intentionally unmapped → log / skip / manual handling
                                        }
                                    }
                                    else if (path.Contains("/content/dam/ashley-furniture/image_placeholders"))
                                    {
                                        var imagePlaceholderRule = FolderClassificationRuleLoader.GetImagePlaceholderRule(imagePlaceholderRules, fullAzurePath);

                                        if (!string.IsNullOrWhiteSpace(imagePlaceholderRule?.AprimoProductCategory))
                                        {
                                            // create AprimoFieldUpsert
                                            category = imagePlaceholderRule.AprimoProductCategory;
                                        }
                                    }
                                    else if (path.Contains("/content/dam/ashley-furniture/series-fpo"))
                                    {
                                        var seriesFPORule = FolderClassificationRuleLoader.GetSeriesFPORule(seriesFPORules, fullAzurePath);

                                        if (!string.IsNullOrWhiteSpace(seriesFPORule?.AprimoProductCategory))
                                        {
                                            // create AprimoFieldUpsert
                                            category = seriesFPORule.AprimoProductCategory;
                                        }
                                    }
                                    else if (path.Contains("/content/dam/ashley-furniture/video/afi_product_videos"))
                                    {
                                        var afiVideorule = FolderClassificationRuleLoader.GetAFIVideoProductCategoryRule(afiVideoRules, fullAzurePath);

                                        if (afiVideorule?.AprimoProductCategory != null)
                                        {
                                            // build AprimoFieldUpsert for Product Category
                                            category = afiVideorule.AprimoProductCategory;
                                        }
                                        else if (afiVideorule?.Flags?.NoMappingProvided == true)
                                        {
                                            // intentionally unmapped → log / skip / manual handling
                                        }
                                    }
                                    else if (path.Contains("/content/dam/ashley-furniture/video/ahs_product_videos"))
                                    {
                                        var ahsVideorule = FolderClassificationRuleLoader.GetAHSVideoProductCategoryRule(ahsVideoRules, fullAzurePath);

                                        if (ahsVideorule?.AprimoProductCategory != null)
                                        {
                                            // build AprimoFieldUpsert for Product Category
                                            category = ahsVideorule.AprimoProductCategory;
                                        }
                                        else if (ahsVideorule?.Flags?.NoMappingProvided == true)
                                        {
                                            // intentionally unmapped → log / skip / manual handling
                                        }
                                    }

                                    // inline finish

                                    if (path.Contains("/content/dam/cgi/digital-color-master-standard"))
                                    {
                                        var cgiInlineFinishrule = FolderClassificationRuleLoader.GetInlineFinishCGIRule(cgiInlineFinishRules, fullAzurePath);

                                        if (cgiInlineFinishrule?.AprimoInlineFinish != null)
                                        {
                                            // build AprimoFieldUpsert for Inline Finish
                                            inlineFinish = cgiInlineFinishrule.AprimoInlineFinish;
                                        }
                                        else if (cgiInlineFinishrule?.Flags?.NoMappingProvided == true)
                                        {
                                            // intentionally unmapped → log / skip / manual handling
                                        }
                                    }

                                    _logger.LogInformation($"after rules, found AssetType: {assetType}, Asset Subtype: {assetSubtype}, Product Category: {category}, Inline Finish: {inlineFinish}, Vendor: {vendor}, Asset Status: {assetStatus}");
                                    logOutput.Add($"after rules, found AssetType: {assetType}, Asset Subtype: {assetSubtype}, Product Category: {category}, Inline Finish: {inlineFinish}, Vendor: {vendor}, Asset Status: {assetStatus}");

                                    Dictionary<string, string> classificationData = new Dictionary<string, string>();
                                    classificationData.Add("Asset Type", assetType);
                                    classificationData.Add("Asset Subtype", assetSubtype);
                                    classificationData.Add("Product Category", category);
                                    classificationData.Add("Inline Finishes", inlineFinish);
                                    classificationData.Add("Vendor Name", vendor);
                                    classificationData.Add("Asset Status", assetStatus);

                                    var upsertsThatAreClassifications = upserts.Where(u => u.IsClassification).ToList();

                                    var classificationUpserts = AprimoUpsertBuilder.BuildClassificationUpserts(classificationData, _aprimoClient, definitions, classifications, logOutput, languageId).ToList();

                                    var upsertsReadyToApply = upserts.Where(u => !u.IsClassification).ToList();

                                    foreach (var upsert in upsertsThatAreClassifications)
                                    {
                                        classificationData.Add(upsert.FieldLabel, upsert.RawValue);
                                    }
                                    classificationUpserts.AddRange(upsertsThatAreClassifications);

                                    // handle clean up of previously stamped data
                                    List<AprimoFieldUpsert> classificationsToRemove = new List<AprimoFieldUpsert>();
                                    foreach (var key in classificationData.Keys)
                                    {
                                        var currentValues = GetLocalizedValuesForField(assetRecord, key);
                                        if (currentValues.Count > 0)  // we only need to worry about cleaning up data if it exists
                                        {
                                            if (string.IsNullOrWhiteSpace(classificationData[key]))
                                            {
                                                // no value exists in this current stamping for this key. we need to remove all currently stamped values.
                                                foreach (var value in currentValues)
                                                {
                                                    AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                                    afu.Value = value;
                                                    classificationsToRemove.Add(afu);
                                                }
                                            }
                                            else
                                            {
                                                // a value exists in this current stamping.  
                                                // we need to remove any current values that are not already the one we are trying to stamp
                                                var classUpsert = classificationUpserts.Where(c => c.FieldLabel.Equals(key)).FirstOrDefault();
                                                if (classUpsert != null)
                                                {
                                                    foreach (var value in currentValues)
                                                    {
                                                        if (!value.Equals(classUpsert.Value))
                                                        {
                                                            AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                                            afu.Value = value;
                                                            classificationsToRemove.Add(afu);
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                    }

                                    var excludedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "Classification List",
                                "Date Time",
                                "Date",
                                "RecordLink"
                            };

                                    var excludedFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "DisplayTitle",
                                "Description",
                                "Creator",
                                "itemGeneralColor"
                            };

                                    var fields = typeof(AssetMetadata)
                                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .SelectMany(p => p.GetCustomAttributes<AprimoFieldAttribute>(true)
                                            .Select(attr => new { Prop = p, Attr = attr }))
                                        .Where(x => !excludedTypes.Contains((x.Attr.DataType ?? "").Trim()) &&
                                            !excludedFieldNames.Contains(x.Attr.FieldName))
                                        .Select(x => new
                                        {
                                            PropertyName = x.Prop.Name,
                                            AprimoName = x.Attr.FieldName,
                                            AprimoType = x.Attr.DataType,
                                            RawValue = x.Prop.GetValue(metadataFromJson),
                                            Value = x.Prop.GetValue(metadataFromJson) switch
                                            {
                                                null => null,
                                                string s => s,
                                                _ => x.Prop.GetValue(metadataFromJson)!.ToString()
                                            }
                                        })
                                        .ToList();


                                    List<AprimoFieldUpsert> fieldsToRemove = new List<AprimoFieldUpsert>();
                                    foreach (var field in fields)
                                    {
                                        var currentValues = GetLocalizedValuesForFieldName(assetRecord, field.AprimoName);
                                        if (currentValues.Count > 0)  // we only need to worry about cleaning up data if it exists
                                        {
                                            var aprimoField = assetRecord?.Embedded?.Fields?.Items?
                                                .FirstOrDefault(f => string.Equals(f.FieldName, field.AprimoName, StringComparison.OrdinalIgnoreCase) && !f.DataType.Equals("ClassificationList"));

                                            bool isMulti = aprimoField.LocalizedValues[0].Values != null;

                                            if (field.Value is not string s || string.IsNullOrWhiteSpace(s))
                                            {
                                                // no value exists in this current stamping for this key. we need to remove all currently stamped values.
                                                AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                                foreach (var value in currentValues)
                                                {
                                                    if (isMulti)
                                                    {
                                                        if (afu.Values == null) 
                                                            afu.Values = new List<string>();

                                                        afu.Values.Add(value);

                                                    }
                                                    else
                                                    {
                                                        afu.Value = value;
                                                    }
                                                }
                                                var hasIdAlready = upsertsReadyToApply.Where(x => x.FieldId == aprimoField.Id).FirstOrDefault();
                                                if (hasIdAlready == null)
                                                {
                                                    afu.FieldId = aprimoField.Id;
                                                    afu.LanguageId = languageId;
                                                    fieldsToRemove.Add(afu);
                                                }
                                            }
                                            //else
                                            //{
                                            //    // a value exists in this current stamping.  
                                            //    // we need to remove any current values that are not already the one we are trying to stamp
                                            //    var fieldUpsert = upsertsReadyToApply.Where(c => c.FieldName.Equals(field.AprimoName)).FirstOrDefault();
                                            //    if (fieldUpsert != null)
                                            //    {
                                            //        if(fieldUpsert.Values != null)
                                            //        {
                                            //            //bool areEqual = fieldUpsert.Values.Count == currentValues.Count && new HashSet<string>(fieldUpsert.Values).SetEquals(currentValues);
                                            //            var difference = currentValues.Except(fieldUpsert.Values).ToList();
                                            //            if (difference.Count > 0)
                                            //            {
                                            //                AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                            //                afu.Values = difference;
                                            //                afu.FieldId = aprimoField.Id;
                                            //                afu.LanguageId = languageId;
                                            //                fieldsToRemove.Add(afu);
                                            //            }

                                            //        }
                                            //        else
                                            //        {
                                            //            foreach (var value in currentValues)
                                            //            {

                                            //                if (!value.Equals(fieldUpsert.Value))
                                            //                {
                                            //                    AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                            //                    afu.Value = value;
                                            //                    afu.FieldId = aprimoField.Id;
                                            //                    afu.LanguageId = languageId;
                                            //                    fieldsToRemove.Add(afu);
                                            //                }
                                            //            }
                                            //        }
                                            //    }
                                            //}
                                        }
                                    }

                                    
                                    // TODO: what fields am i allowed to remove?

                                    await _aprimoClient.StampMetadataAsync(aprimoId, upsertsReadyToApply, new List<AprimoFieldUpsert>(), classificationUpserts, classificationsToRemove, cancellationToken);


                                    _logger.LogInformation($"Record {aprimoId} for AEM Asset Id {uuid} Has been stamped.");
                                    logOutput.Add($"Record {aprimoId} for AEM Asset Id {uuid} Has been stamped.");

                                }
                                else
                                {
                                    _logger.LogInformation($"Record could not be found for Aprimo Record {aprimoId} / AEM Asset Id {uuid}. Skipping Record.");
                                    logOutput.Add($"Record could not be found for Aprimo Record {aprimoId} / AEM Asset Id {uuid}. Skipping Record.");
                                }

                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"Record {aprimoId} restamp failed: {ex.Message}");
                                logOutput.Add($"Record {aprimoId} restamp failed: {ex.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"Could not get MHO Record for {uuid} : {aprimoId} restamp failed: {ex.Message}");
                            logOutput.Add($"Could not get MHO Record for {uuid} : {aprimoId} restamp failed: {ex.Message}");
                        }
                    }


                }
                else
                {
                    _logger.LogInformation($"Record {aprimoId} does not have AEM Asset Id {uuid}. Skipping Record.");
                    logOutput.Add($"Record {aprimoId} does not have AEM Asset Id {uuid}. Skipping Record.");
                }
            }


            await LogToAzure(importFileName, logOutput);

        }

        public async Task StampAllAssetsInAprimo(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();
            string importFileName = "allAssetsInAprimoFullRestamp.xlsx";

            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");
            logOutput.Add($"found {classifications.Count()} classifications");

            var definitions = await _aprimoClient.GetAllDefinitionsAsync(cancellationToken);
            _logger.LogInformation($"found {definitions.Count()} definitions");
            logOutput.Add($"found {definitions.Count()} definitions");

            // item status rules
            var itemStatusRules = FolderClassificationRuleLoader.LoadItemStatusRules();

            // Asset_Type Subtype Rule - folderMapping.json
            var folderRules = FolderClassificationRuleLoader.LoadFolderMapping();
            // Vendor Name and possible Subtype Rule - 3PVendorAndSubtypeMappping.json
            // NOTE: 
            var vendorSubtypeRuleSets = FolderClassificationRuleLoader.Load3PVendorAndSubtypeMapping();
            // 1P Subtype from FileName Rule - 1PSubtypeFromFileName.json
            var subfolderP1Rules = FolderClassificationRuleLoader.LoadP1Subtype();

            // spreadsheet indicates no need to do Product View rules per client

            var glbRules = FolderClassificationRuleLoader.LoadGLBProductCatRules();
            var studioPhotographyRules = FolderClassificationRuleLoader.LoadStudioPhotographyProductCatRules();
            var imagePlaceholderRules = FolderClassificationRuleLoader.LoadImagePlaceholderProductCatRules();
            var seriesFPORules = FolderClassificationRuleLoader.LoadSeriesFPOProductCatRules();
            var afiVideoRules = FolderClassificationRuleLoader.LoadAFIVideoProductCatRules();
            var ahsVideoRules = FolderClassificationRuleLoader.LoadAHSVideoProductCatRules();
            var cgiInlineFinishRules = FolderClassificationRuleLoader.LoadInlineFinishCGIRules();

            logOutput.Add($"loaded all classification rules");
            _logger.LogInformation($"loaded all classification rules");

            bool hasPrimed = false;

            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            RestampPipelineRepository repo = new RestampPipelineRepository(connectionString);
            MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjects");

            await Task.Delay(10000);

            var options = new AprimoAssetStamper.StamperOptions
            {
                BatchSize = 2000,
                MaxDegreeOfParallelism = 5,     // tune based on Aprimo limits
                MaxRetries = 5,
                InitialBackoff = TimeSpan.FromSeconds(1),
                MaxBackoff = TimeSpan.FromSeconds(30),
                ProgressInterval = TimeSpan.FromSeconds(10),
                Progress = p =>
                {
                    _logger.LogInformation(
                        $"{p.Phase} progress: Batches={p.BatchesCompleted:n0}, Claimed={p.TotalClaimed:n0}, Done={p.TotalDone:n0}, Failed={p.TotalFailed:n0}, Rate={p.ItemsPerSecond:n2}/s, Elapsed={p.Elapsed}");
                },
                OnRetry = r =>
                {
                    _logger.LogWarning($"Retry {r.Attempt} for {r.DictKey} after {r.Delay}: {r.Exception.Message}");
                }
            };

            // test run only
            //options.MaxBatches = 1;

            var summary = await AprimoAssetStamper.StampAllAprimoAssetsPerItemAsync(
                repo,
                async (AssetStampRow row, CancellationToken ct) =>
                {
                    try
                    {
                        MappingHelperObject mho = JsonConvert.DeserializeObject<MappingHelperObject>(row.MappingHelperJson);
                        AssetMetadata metadataFromJson = JsonConvert.DeserializeObject<AssetMetadata>(row.AssetMetadataJson);

                        var aprimoId = mho.AprimoId;
                        var uuid = row.DictKey;
                        var path = mho.AemAssetPath;
                        var createdAEMDate = mho.AemCreatedDate;
                        _logger.LogInformation($"Processing Record {aprimoId}.");


                        var realFileName = Path.GetFileName(path);
                        var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{Path.GetDirectoryName(path)}";
                        //var assetMetadataFolder = $"{_azureOptions.Value.MetadataRootPrefix}/{Path.GetDirectoryName(path)}";
                        string cleanedFilename = Regex.Replace(realFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();
                        string azureFilename = $"{uuid}_{cleanedFilename}";
                        string azureMetadataFilename = $"{uuid}_metadata.json";
                        string fullAzurePath = $"{assetFolder}/{cleanedFilename}";
                        fullAzurePath = Normalize(fullAzurePath);
                        logOutput.Add($"Processing {uuid} : {path}");
                        logOutput.Add($"  matched with azure path {fullAzurePath}");
                        _logger.LogInformation($"Processing {uuid} : {path}");

                        _logger.LogInformation($"Processing {uuid} : {path}");
                        try
                        {
                            bool hasBadOrMissingAprimoId = false;
                            AprimoRecord assetRecord = null;
                            //this should not be the case, but to be safe:
                            if (!string.IsNullOrEmpty(aprimoId))
                            {
                                assetRecord = await _aprimoClient.GetAssetByAprimoIdAsync(aprimoId, cancellationToken);
                            }
                            else
                            {
                                assetRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(uuid, cancellationToken);
                                hasBadOrMissingAprimoId = true;
                            }

                            // this should not happen, but if the aprimo record was not found somehow...
                            if (assetRecord == null)
                            {
                                assetRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(uuid, cancellationToken);
                                hasBadOrMissingAprimoId = true;
                            }

                            if (assetRecord != null)
                            {
                                aprimoId = assetRecord.Id; // in case it was missing 

                                if(hasBadOrMissingAprimoId)
                                {
                                    // fix it
                                    mho.AprimoId = aprimoId;
                                    string fixedMho = JsonConvert.SerializeObject(mho);
                                    mhoRepo.UpdateJsonBody(uuid, fixedMho);
                                }

                                logOutput.Add($"Found matching asset in Aprimo : {assetRecord.Id}");
                                _logger.LogInformation($"Found matching asset in Aprimo : {assetRecord.Id}");

                                // prime cache once 
                                if (!hasPrimed)
                                {
                                    await _aprimoClient.EnsureLanguagesLoadedAsync(cancellationToken);
                                    languageId = _aprimoClient.ResolveLanguageId(locale);
                                    await _aprimoClient.PrimeFieldIdCacheFromRecordAsync(assetRecord.Id!, cancellationToken);

                                    logOutput.Add($"Loaded languages and primed cache");
                                    _logger.LogInformation($"Loaded languages and primed cache");

                                    hasPrimed = true;
                                }

                                // handle ItemStatus -> Asset Status mapping
                                string assetStatus = string.Empty;
                                var itemStatusRule = FolderClassificationRuleLoader.GetItemStatusRule(itemStatusRules, metadataFromJson.ProductsItemStatus);
                                if (itemStatusRule != null)
                                {
                                    assetStatus = itemStatusRule.AprimoValue;
                                }

                                // pop the created date time in there
                                metadataFromJson.ProductsAEMCreationDate = createdAEMDate;
                                string utcString = DateTime.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT+0000";
                                metadataFromJson.LastTouchedUTC = utcString; //DateTime.UtcNow.ToString("o");

                                var upserts = AprimoUpsertBuilder.BuildUpserts(metadataFromJson, _aprimoClient, definitions, classifications, logOutput, languageId);
                                logOutput.Add($"Found {upserts.Count()} metadata upserts.");
                                _logger.LogInformation($"Found {upserts.Count()} metadata upserts.");

                                // build classifications for type,subtype, vendor name, product category, and inline finish 
                                var rule = FolderClassificationRuleLoader.GetRuleForPath(folderRules, fullAzurePath);

                                string assetType = string.Empty;
                                string assetSubtype = string.Empty;
                                string category = string.Empty;
                                string inlineFinish = string.Empty;
                                string vendor = string.Empty;

                                if (rule == null)
                                {
                                    // no match found
                                    _logger.LogWarning($"No folder rule found for path {fullAzurePath}");
                                    logOutput.Add($"No folder rule found for path {fullAzurePath}");
                                }
                                else if (rule.RequiresAdditionalLogic)
                                {
                                    assetType = rule.AssetType; // Got this, but check for additional sub type logic.

                                    logOutput.Add($"Found {assetType} from folder rule, but require additional logic for subType");
                                    _logger.LogInformation($"Found {assetType} from folder rule, but require additional logic for subType.");

                                    // branch to filename logic
                                    if (rule.AemFolder.Equals("/content/dam/ashley-furniture/studiophotography"))
                                    {
                                        var subfolderRule = FolderClassificationRuleLoader.ResolveFromFileName(subfolderP1Rules, cleanedFilename);
                                        assetSubtype = subfolderRule?.Subtype;
                                    }

                                    if (rule.AemFolder.Equals("/content/dam/ashley-furniture/3rdparty"))
                                    {
                                        var match = FolderClassificationRuleLoader.Resolve(vendorSubtypeRuleSets, fullAzurePath);

                                        vendor = match?.VendorName;   // may be null for pure subtype rules like Swatch Bank

                                    }
                                }
                                else
                                {
                                    // stamp rule.AssetType + rule.AssetSubtype
                                    assetType = rule.AssetType;
                                    assetSubtype = rule.AssetSubtype;

                                }

                                // handle classification for product category
                                string ext = Path.GetExtension(cleanedFilename);

                                if (ext.ToLower().Equals(".glb"))
                                {
                                    category = FolderClassificationRuleLoader.GetCategoryFromFilename(glbRules, cleanedFilename);
                                }
                                else if (path.Contains("/content/dam/ashley-furniture/studiophotography"))
                                {
                                    var studioPhotographyrule = FolderClassificationRuleLoader.GetStudioPhotographyProductCategoryRule(studioPhotographyRules, fullAzurePath);

                                    if (studioPhotographyrule?.AprimoProductCategory != null)
                                    {
                                        // build AprimoFieldUpsert for Product Category
                                        category = studioPhotographyrule.AprimoProductCategory;
                                    }
                                    else if (studioPhotographyrule?.Flags?.NoMappingProvided == true)
                                    {
                                        // intentionally unmapped → log / skip / manual handling
                                    }
                                }
                                else if (path.Contains("/content/dam/ashley-furniture/image_placeholders"))
                                {
                                    var imagePlaceholderRule = FolderClassificationRuleLoader.GetImagePlaceholderRule(imagePlaceholderRules, fullAzurePath);

                                    if (!string.IsNullOrWhiteSpace(imagePlaceholderRule?.AprimoProductCategory))
                                    {
                                        // create AprimoFieldUpsert
                                        category = imagePlaceholderRule.AprimoProductCategory;
                                    }
                                }
                                else if (path.Contains("/content/dam/ashley-furniture/series-fpo"))
                                {
                                    var seriesFPORule = FolderClassificationRuleLoader.GetSeriesFPORule(seriesFPORules, fullAzurePath);

                                    if (!string.IsNullOrWhiteSpace(seriesFPORule?.AprimoProductCategory))
                                    {
                                        // create AprimoFieldUpsert
                                        category = seriesFPORule.AprimoProductCategory;
                                    }
                                }
                                else if (path.Contains("/content/dam/ashley-furniture/video/afi_product_videos"))
                                {
                                    var afiVideorule = FolderClassificationRuleLoader.GetAFIVideoProductCategoryRule(afiVideoRules, fullAzurePath);

                                    if (afiVideorule?.AprimoProductCategory != null)
                                    {
                                        // build AprimoFieldUpsert for Product Category
                                        category = afiVideorule.AprimoProductCategory;
                                    }
                                    else if (afiVideorule?.Flags?.NoMappingProvided == true)
                                    {
                                        // intentionally unmapped → log / skip / manual handling
                                    }
                                }
                                else if (path.Contains("/content/dam/ashley-furniture/video/ahs_product_videos"))
                                {
                                    var ahsVideorule = FolderClassificationRuleLoader.GetAHSVideoProductCategoryRule(ahsVideoRules, fullAzurePath);

                                    if (ahsVideorule?.AprimoProductCategory != null)
                                    {
                                        // build AprimoFieldUpsert for Product Category
                                        category = ahsVideorule.AprimoProductCategory;
                                    }
                                    else if (ahsVideorule?.Flags?.NoMappingProvided == true)
                                    {
                                        // intentionally unmapped → log / skip / manual handling
                                    }
                                }

                                // inline finish

                                if (path.Contains("/content/dam/cgi/digital-color-master-standard"))
                                {
                                    var cgiInlineFinishrule = FolderClassificationRuleLoader.GetInlineFinishCGIRule(cgiInlineFinishRules, fullAzurePath);

                                    if (cgiInlineFinishrule?.AprimoInlineFinish != null)
                                    {
                                        // build AprimoFieldUpsert for Inline Finish
                                        inlineFinish = cgiInlineFinishrule.AprimoInlineFinish;
                                    }
                                    else if (cgiInlineFinishrule?.Flags?.NoMappingProvided == true)
                                    {
                                        // intentionally unmapped → log / skip / manual handling
                                    }
                                }

                                _logger.LogInformation($"after rules, found AssetType: {assetType}, Asset Subtype: {assetSubtype}, Product Category: {category}, Inline Finish: {inlineFinish}, Vendor: {vendor}, Asset Status: {assetStatus}");
                                logOutput.Add($"after rules, found AssetType: {assetType}, Asset Subtype: {assetSubtype}, Product Category: {category}, Inline Finish: {inlineFinish}, Vendor: {vendor}, Asset Status: {assetStatus}");

                                Dictionary<string, string> classificationData = new Dictionary<string, string>();
                                classificationData.Add("Asset Type", assetType);
                                classificationData.Add("Asset Subtype", assetSubtype);
                                classificationData.Add("Product Category", category);
                                classificationData.Add("Inline Finishes", inlineFinish);
                                classificationData.Add("Vendor Name", vendor);
                                classificationData.Add("Asset Status", assetStatus);

                                var upsertsThatAreClassifications = upserts.Where(u => u.IsClassification).ToList();

                                var classificationUpserts = AprimoUpsertBuilder.BuildClassificationUpserts(classificationData, _aprimoClient, definitions, classifications, logOutput, languageId).ToList();

                                var upsertsReadyToApply = upserts.Where(u => !u.IsClassification).ToList();

                                foreach (var upsert in upsertsThatAreClassifications)
                                {
                                    classificationData.Add(upsert.FieldLabel, upsert.RawValue);
                                }
                                classificationUpserts.AddRange(upsertsThatAreClassifications);

                                // handle clean up of previously stamped data
                                List<AprimoFieldUpsert> classificationsToRemove = new List<AprimoFieldUpsert>();
                                foreach (var key in classificationData.Keys)
                                {
                                    var currentValues = GetLocalizedValuesForField(assetRecord, key);
                                    if (currentValues.Count > 0)  // we only need to worry about cleaning up data if it exists
                                    {
                                        if (string.IsNullOrWhiteSpace(classificationData[key]))
                                        {
                                            // no value exists in this current stamping for this key. we need to remove all currently stamped values.
                                            foreach (var value in currentValues)
                                            {
                                                AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                                afu.Value = value;
                                                classificationsToRemove.Add(afu);
                                            }
                                        }
                                        else
                                        {
                                            // a value exists in this current stamping.  
                                            // we need to remove any current values that are not already the one we are trying to stamp
                                            var classUpsert = classificationUpserts.Where(c => c.FieldLabel.Equals(key)).FirstOrDefault();
                                            if (classUpsert != null)
                                            {
                                                foreach (var value in currentValues)
                                                {
                                                    if (!value.Equals(classUpsert.Value))
                                                    {
                                                        AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                                        afu.Value = value;
                                                        classificationsToRemove.Add(afu);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }

                                var excludedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "Classification List",
                                "Date Time",
                                "Date",
                                "RecordLink"
                            };

                                var excludedFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "DisplayTitle",
                                "Description"
                            };

                                var fields = typeof(AssetMetadata)
                                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .SelectMany(p => p.GetCustomAttributes<AprimoFieldAttribute>(true)
                                        .Select(attr => new { Prop = p, Attr = attr }))
                                    .Where(x => !excludedTypes.Contains((x.Attr.DataType ?? "").Trim()) &&
                                        !excludedFieldNames.Contains(x.Attr.FieldName))
                                    .Select(x => new
                                    {
                                        PropertyName = x.Prop.Name,
                                        AprimoName = x.Attr.FieldName,
                                        AprimoType = x.Attr.DataType,
                                        RawValue = x.Prop.GetValue(metadataFromJson),
                                        Value = x.Prop.GetValue(metadataFromJson) switch
                                        {
                                            null => null,
                                            string s => s,
                                            _ => x.Prop.GetValue(metadataFromJson)!.ToString()
                                        }
                                    })
                                    .ToList();


                                List<AprimoFieldUpsert> fieldsToRemove = new List<AprimoFieldUpsert>();
                                //foreach (var field in fields)
                                //{
                                //    var currentValues = GetLocalizedValuesForFieldName(assetRecord, field.AprimoName);
                                //    if (currentValues.Count > 0)  // we only need to worry about cleaning up data if it exists
                                //    {
                                //        var aprimoField = assetRecord?.Embedded?.Fields?.Items?
                                //            .FirstOrDefault(f => string.Equals(f.FieldName, field.AprimoName, StringComparison.OrdinalIgnoreCase) && !f.DataType.Equals("ClassificationList"));

                                //        bool isMulti = aprimoField.LocalizedValues[0].Values != null;

                                //        if (field.Value is not string s || string.IsNullOrWhiteSpace(s))
                                //        {
                                //            // no value exists in this current stamping for this key. we need to remove all currently stamped values.
                                //            foreach (var value in currentValues)
                                //            {
                                //                AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                //                if (isMulti)
                                //                {
                                //                    afu.Values.Add(value);

                                //                }
                                //                else
                                //                {
                                //                    afu.Value = value;
                                //                }

                                //                afu.FieldId = aprimoField.Id;
                                //                afu.LanguageId = languageId;
                                //                fieldsToRemove.Add(afu);
                                //            }
                                //        }
                                //        //else
                                //        //{
                                //        //    // a value exists in this current stamping.  
                                //        //    // we need to remove any current values that are not already the one we are trying to stamp
                                //        //    var fieldUpsert = upsertsReadyToApply.Where(c => c.FieldName.Equals(field.AprimoName)).FirstOrDefault();
                                //        //    if (fieldUpsert != null)
                                //        //    {
                                //        //        if(fieldUpsert.Values != null)
                                //        //        {
                                //        //            //bool areEqual = fieldUpsert.Values.Count == currentValues.Count && new HashSet<string>(fieldUpsert.Values).SetEquals(currentValues);
                                //        //            var difference = currentValues.Except(fieldUpsert.Values).ToList();
                                //        //            if (difference.Count > 0)
                                //        //            {
                                //        //                AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                //        //                afu.Values = difference;
                                //        //                afu.FieldId = aprimoField.Id;
                                //        //                afu.LanguageId = languageId;
                                //        //                fieldsToRemove.Add(afu);
                                //        //            }

                                //        //        }
                                //        //        else
                                //        //        {
                                //        //            foreach (var value in currentValues)
                                //        //            {

                                //        //                if (!value.Equals(fieldUpsert.Value))
                                //        //                {
                                //        //                    AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                //        //                    afu.Value = value;
                                //        //                    afu.FieldId = aprimoField.Id;
                                //        //                    afu.LanguageId = languageId;
                                //        //                    fieldsToRemove.Add(afu);
                                //        //                }
                                //        //            }
                                //        //        }
                                //        //    }
                                //        //}
                                //    }
                                //}


                                await _aprimoClient.StampMetadataAsync(aprimoId, upsertsReadyToApply, fieldsToRemove,classificationUpserts, classificationsToRemove, cancellationToken);

                                // test that the pipeline works before stamping.
                                //await Task.Yield();

                                _logger.LogInformation($"Record {aprimoId} for AEM Asset Id {uuid} Has been stamped.");
                                logOutput.Add($"Record {aprimoId} for AEM Asset Id {uuid} Has been stamped.");
                                return AprimoAssetStamper.ItemStampResult.Ok();

                            }
                            else
                            {
                                _logger.LogInformation($"Record could not be found for Aprimo Record {aprimoId} / AEM Asset Id {uuid}. Skipping Record.");
                                return AprimoAssetStamper.ItemStampResult.Fail($"Record could not be found for Aprimo Record {aprimoId} / AEM Asset Id {uuid}. Skipping Record.", retryable: false);
                            }

                        }
                        catch (Exception ex)
                        {
                            logOutput.Add($"Record {aprimoId} restamp failed: {ex.Message}");
                            return AprimoAssetStamper.ItemStampResult.Fail(ex.Message, retryable: true);
                        }

                    }
                    catch (Exception ex)
                    {
                        // decide retryable or not based on the exception
                        return AprimoAssetStamper.ItemStampResult.Fail(ex.Message, retryable: true);
                    }


                },
                options,
                cancellationToken);

            _logger.LogInformation($"DONE. Claimed={summary.TotalClaimed:n0} Done={summary.TotalDone:n0} Failed={summary.TotalFailed:n0} Elapsed={summary.Elapsed}");



            await LogToAzure(importFileName, logOutput);

        }

        public async Task TestStampAssetsInAprimo(CancellationToken cancellationToken)
        {
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjects");
            MappingHelperObjectsRepository mhoMDRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.AssetMetadata");

            var logOutput = new List<string>();

            var classifications = await _aprimoClient.GetAllClassificationsAsync(cancellationToken);
            _logger.LogInformation($"found {classifications.Count()} classifications");
            logOutput.Add($"found {classifications.Count()} classifications");

            var definitions = await _aprimoClient.GetAllDefinitionsAsync(cancellationToken);
            _logger.LogInformation($"found {definitions.Count()} definitions");
            logOutput.Add($"found {definitions.Count()} definitions");

            // item status rules
            var itemStatusRules = FolderClassificationRuleLoader.LoadItemStatusRules();

            // Asset_Type Subtype Rule - folderMapping.json
            var folderRules = FolderClassificationRuleLoader.LoadFolderMapping();
            // Vendor Name and possible Subtype Rule - 3PVendorAndSubtypeMappping.json
            // NOTE: 
            var vendorSubtypeRuleSets = FolderClassificationRuleLoader.Load3PVendorAndSubtypeMapping();
            // 1P Subtype from FileName Rule - 1PSubtypeFromFileName.json
            var subfolderP1Rules = FolderClassificationRuleLoader.LoadP1Subtype();

            // spreadsheet indicates no need to do Product View rules per client

            var glbRules = FolderClassificationRuleLoader.LoadGLBProductCatRules();
            var studioPhotographyRules = FolderClassificationRuleLoader.LoadStudioPhotographyProductCatRules();
            var imagePlaceholderRules = FolderClassificationRuleLoader.LoadImagePlaceholderProductCatRules();
            var seriesFPORules = FolderClassificationRuleLoader.LoadSeriesFPOProductCatRules();
            var afiVideoRules = FolderClassificationRuleLoader.LoadAFIVideoProductCatRules();
            var ahsVideoRules = FolderClassificationRuleLoader.LoadAHSVideoProductCatRules();
            var cgiInlineFinishRules = FolderClassificationRuleLoader.LoadInlineFinishCGIRules();

            logOutput.Add($"loaded all classification rules");
            _logger.LogInformation($"loaded all classification rules");

            bool hasPrimed = false;
            string dictKey = "ff55d0a9-62ca-43e1-90e0-eb4e38eb27b4";
            string mhoJson = mhoRepo.GetJsonBodyByDictKey(dictKey);
            MappingHelperObject mho = JsonConvert.DeserializeObject<MappingHelperObject>(mhoJson);

            var aprimoId = "";
                //var aprimoId = rowData["AprimoId"];
                //_logger.LogInformation($"Processing Record {aprimoId}.");

                var uuid = dictKey;
                var path = mho.AemAssetPath;
                var createdAEMDate = mho.AemCreatedDate;

                var realFileName = Path.GetFileName(path);
                var assetFolder = $"{_azureOptions.Value.AssetRootPrefix}{Path.GetDirectoryName(path)}";
                //var assetMetadataFolder = $"{_azureOptions.Value.MetadataRootPrefix}/{Path.GetDirectoryName(path)}";
                string cleanedFilename = Regex.Replace(realFileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();
                string azureFilename = $"{uuid}_{cleanedFilename}";
                string azureMetadataFilename = $"{uuid}_metadata.json";
                string fullAzurePath = $"{assetFolder}/{cleanedFilename}";
                fullAzurePath = Normalize(fullAzurePath);
                logOutput.Add($"Processing {uuid} : {path}");
                logOutput.Add($"  matched with azure path {fullAzurePath}");
                _logger.LogInformation($"Processing {uuid} : {path}");
                try
                {
                    var blobClient = await _assetsWrapper.GetBlobClientAsync(azureFilename, assetFolder);

                    //var assetRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(uuid, cancellationToken);
                    AprimoRecord assetRecord = null;
                    var assetRecords = await _aprimoClient.GetAssetsByAemAssetIdAsync(uuid, cancellationToken);
                    if (assetRecords.Count > 0)
                    {
                        if (assetRecords.Count > 1)
                        {
                            logOutput.Add($"Uh Oh! Found {assetRecords.Count} records matching uuid {uuid}");
                            _logger.LogInformation($"Uh Oh! Found {assetRecords.Count} records matching uuid {uuid}");
                            var ordered = assetRecords.OrderByDescending(r => r.ModifiedOn).ToList();
                            assetRecord = ordered.First();
                            var recordsToDelete = ordered.Skip(1).ToList();

                            foreach (var record in recordsToDelete)
                            {
                                var deletedRecord = await _aprimoClient.DeleteAssetByAprimoIdAsync(record.Id, cancellationToken);
                                logOutput.Add($"deleted record {record.Id} : {deletedRecord}");
                                _logger.LogInformation($"deleted record {record.Id} : {deletedRecord}");
                            }
                        }
                        else
                        {
                            assetRecord = assetRecords[0];
                        }
                    }


                    //var assetRecord = await _aprimoClient.GetAssetByAprimoIdAsync(aprimoId, cancellationToken);
                    if (assetRecord != null)
                    {
                        logOutput.Add($"Found matching asset in Aprimo : {assetRecord.Id}");
                        _logger.LogInformation($"Found matching asset in Aprimo : {assetRecord.Id}");
                        aprimoId = assetRecord.Id;

                        // prime cache once 
                        if (!hasPrimed)
                        {
                            await _aprimoClient.EnsureLanguagesLoadedAsync(cancellationToken);
                            languageId = _aprimoClient.ResolveLanguageId(locale);
                            await _aprimoClient.PrimeFieldIdCacheFromRecordAsync(assetRecord.Id!, cancellationToken);

                            logOutput.Add($"Loaded languages and primed cache");
                            _logger.LogInformation($"Loaded languages and primed cache");

                            hasPrimed = true;
                        }

                        // just read the metadata file to get all the data.  there were added fields since i pulled the data into excel.
                        string json = mhoMDRepo.GetJsonBodyByDictKey(dictKey);
                        var metadataFromJson = JsonConvert.DeserializeObject<AssetMetadata>(json);

                        // handle ItemStatus -> Asset Status mapping
                        string assetStatus = string.Empty;
                        var itemStatusRule = FolderClassificationRuleLoader.GetItemStatusRule(itemStatusRules, metadataFromJson.ProductsItemStatus);
                        if (itemStatusRule != null)
                        {
                            assetStatus = itemStatusRule.AprimoValue;
                        }

                        // pop the created date time in there
                        metadataFromJson.ProductsAEMCreationDate = createdAEMDate;

                        var upserts = AprimoUpsertBuilder.BuildUpserts(metadataFromJson, _aprimoClient, definitions, classifications, logOutput, languageId);
                        logOutput.Add($"Found {upserts.Count()} metadata upserts.");
                        _logger.LogInformation($"Found {upserts.Count()} metadata upserts.");

                        // build classifications for type,subtype, vendor name, product category, and inline finish 
                        var rule = FolderClassificationRuleLoader.GetRuleForPath(folderRules, fullAzurePath);

                        string assetType = string.Empty;
                        string assetSubtype = string.Empty;
                        string category = string.Empty;
                        string inlineFinish = string.Empty;
                        string vendor = string.Empty;

                        if (rule == null)
                        {
                            // no match found
                            _logger.LogWarning($"No folder rule found for path {fullAzurePath}");
                            logOutput.Add($"No folder rule found for path {fullAzurePath}");
                        }
                        else if (rule.RequiresAdditionalLogic)
                        {
                            assetType = rule.AssetType; // Got this, but check for additional sub type logic.

                            logOutput.Add($"Found {assetType} from folder rule, but require additional logic for subType");
                            _logger.LogInformation($"Found {assetType} from folder rule, but require additional logic for subType.");

                            // branch to filename logic
                            if (rule.AemFolder.Equals("/content/dam/ashley-furniture/studiophotography"))
                            {
                                var subfolderRule = FolderClassificationRuleLoader.ResolveFromFileName(subfolderP1Rules, cleanedFilename);
                                assetSubtype = subfolderRule?.Subtype;
                            }

                            if (rule.AemFolder.Equals("/content/dam/ashley-furniture/3rdparty"))
                            {
                                var match = FolderClassificationRuleLoader.Resolve(vendorSubtypeRuleSets, fullAzurePath);

                                vendor = match?.VendorName;   // may be null for pure subtype rules like Swatch Bank
                            }
                        }
                        else
                        {
                            // stamp rule.AssetType + rule.AssetSubtype
                            assetType = rule.AssetType;
                            assetSubtype = rule.AssetSubtype;

                        }

                        // handle classification for product category
                        string ext = Path.GetExtension(cleanedFilename);

                        if (ext.ToLower().Equals(".glb"))
                        {
                            category = FolderClassificationRuleLoader.GetCategoryFromFilename(glbRules, cleanedFilename);
                        }
                        else if (path.Contains("/content/dam/ashley-furniture/studiophotography"))
                        {
                            var studioPhotographyrule = FolderClassificationRuleLoader.GetStudioPhotographyProductCategoryRule(studioPhotographyRules, fullAzurePath);

                            if (studioPhotographyrule?.AprimoProductCategory != null)
                            {
                                // build AprimoFieldUpsert for Product Category
                                category = studioPhotographyrule.AprimoProductCategory;
                            }
                            else if (studioPhotographyrule?.Flags?.NoMappingProvided == true)
                            {
                                // intentionally unmapped → log / skip / manual handling
                            }
                        }
                        else if (path.Contains("/content/dam/ashley-furniture/image_placeholders"))
                        {
                            var imagePlaceholderRule = FolderClassificationRuleLoader.GetImagePlaceholderRule(imagePlaceholderRules, fullAzurePath);

                            if (!string.IsNullOrWhiteSpace(imagePlaceholderRule?.AprimoProductCategory))
                            {
                                // create AprimoFieldUpsert
                                category = imagePlaceholderRule.AprimoProductCategory;
                            }
                        }
                        else if (path.Contains("/content/dam/ashley-furniture/series-fpo"))
                        {
                            var seriesFPORule = FolderClassificationRuleLoader.GetSeriesFPORule(seriesFPORules, fullAzurePath);

                            if (!string.IsNullOrWhiteSpace(seriesFPORule?.AprimoProductCategory))
                            {
                                // create AprimoFieldUpsert
                                category = seriesFPORule.AprimoProductCategory;
                            }
                        }
                        else if (path.Contains("/content/dam/ashley-furniture/video/afi_product_videos"))
                        {
                            var afiVideorule = FolderClassificationRuleLoader.GetAFIVideoProductCategoryRule(afiVideoRules, fullAzurePath);

                            if (afiVideorule?.AprimoProductCategory != null)
                            {
                                // build AprimoFieldUpsert for Product Category
                                category = afiVideorule.AprimoProductCategory;
                            }
                            else if (afiVideorule?.Flags?.NoMappingProvided == true)
                            {
                                // intentionally unmapped → log / skip / manual handling
                            }
                        }
                        else if (path.Contains("/content/dam/ashley-furniture/video/ahs_product_videos"))
                        {
                            var ahsVideorule = FolderClassificationRuleLoader.GetAHSVideoProductCategoryRule(ahsVideoRules, fullAzurePath);

                            if (ahsVideorule?.AprimoProductCategory != null)
                            {
                                // build AprimoFieldUpsert for Product Category
                                category = ahsVideorule.AprimoProductCategory;
                            }
                            else if (ahsVideorule?.Flags?.NoMappingProvided == true)
                            {
                                // intentionally unmapped → log / skip / manual handling
                            }
                        }

                        // inline finish

                        if (path.Contains("/content/dam/cgi/digital-color-master-standard"))
                        {
                            var cgiInlineFinishrule = FolderClassificationRuleLoader.GetInlineFinishCGIRule(cgiInlineFinishRules, fullAzurePath);

                            if (cgiInlineFinishrule?.AprimoInlineFinish != null)
                            {
                                // build AprimoFieldUpsert for Inline Finish
                                inlineFinish = cgiInlineFinishrule.AprimoInlineFinish;
                            }
                            else if (cgiInlineFinishrule?.Flags?.NoMappingProvided == true)
                            {
                                // intentionally unmapped → log / skip / manual handling
                            }
                        }

                        _logger.LogInformation($"after rules, found AssetType: {assetType}, Asset Subtype: {assetSubtype}, Product Category: {category}, Inline Finish: {inlineFinish}, Vendor: {vendor}, Asset Status: {assetStatus}");
                        logOutput.Add($"after rules, found AssetType: {assetType}, Asset Subtype: {assetSubtype}, Product Category: {category}, Inline Finish: {inlineFinish}, Vendor: {vendor}, Asset Status: {assetStatus}");

                        Dictionary<string, string> classificationData = new Dictionary<string, string>();
                        classificationData.Add("Asset Type", assetType);
                        classificationData.Add("Asset Subtype", assetSubtype);
                        classificationData.Add("Product Category", category);
                        classificationData.Add("Inline Finishes", inlineFinish);
                        classificationData.Add("Vendor Name", vendor);
                        classificationData.Add("Asset Status", assetStatus);

                        var upsertsThatAreClassifications = upserts.Where(u => u.IsClassification).ToList();

                        var classificationUpserts = AprimoUpsertBuilder.BuildClassificationUpserts(classificationData, _aprimoClient, definitions, classifications, logOutput, languageId).ToList();

                        var upsertsReadyToApply = upserts.Where(u => !u.IsClassification).ToList();

                        foreach (var upsert in upsertsThatAreClassifications)
                        {
                            classificationData.Add(upsert.FieldLabel, upsert.RawValue);
                        }
                        classificationUpserts.AddRange(upsertsThatAreClassifications);

                        // handle clean up of previously stamped data
                        List<AprimoFieldUpsert> classificationsToRemove = new List<AprimoFieldUpsert>();
                        foreach (var key in classificationData.Keys)
                        {
                            var currentValues = GetLocalizedValuesForField(assetRecord, key);
                            if (currentValues.Count > 0)  // we only need to worry about cleaning up data if it exists
                            {
                                if (string.IsNullOrWhiteSpace(classificationData[key]))
                                {
                                    // no value exists in this current stamping for this key. we need to remove all currently stamped values.
                                    foreach (var value in currentValues)
                                    {
                                        AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                        afu.Value = value;
                                        classificationsToRemove.Add(afu);
                                    }
                                }
                                else
                                {
                                    // a value exists in this current stamping.  
                                    // we need to remove any current values that are not already the one we are trying to stamp
                                    var classUpsert = classificationUpserts.Where(c => c.FieldLabel.Equals(key)).FirstOrDefault();
                                    if (classUpsert != null)
                                    {
                                        foreach (var value in currentValues)
                                        {
                                            if (!value.Equals(classUpsert.Value))
                                            {
                                                AprimoFieldUpsert afu = new AprimoFieldUpsert();
                                                afu.Value = value;
                                                classificationsToRemove.Add(afu);
                                            }
                                        }
                                    }
                                }
                            }

                        }
                        // TODO: handle fields to remove
                        await _aprimoClient.StampMetadataAsync(aprimoId, upsertsReadyToApply, new List<AprimoFieldUpsert>(), classificationUpserts, classificationsToRemove, cancellationToken);

                        ;

                        _logger.LogInformation($"Record {aprimoId} for AEM Asset Id {uuid} Has been stamped.");
                        logOutput.Add($"Record {aprimoId} for AEM Asset Id {uuid} Has been stamped.");

                    }
                    else
                    {

                        _logger.LogInformation($"Record could not be found for Aprimo Record {aprimoId} / AEM Asset Id {uuid}. Skipping Record.");
                    }

                }
                catch (Exception ex)
                {
                    logOutput.Add($"Record {aprimoId} restamp failed: {ex.Message}");

                }

                     

            
            
        }
        public async Task UpdateGLBsInAprimoFromFile(CancellationToken cancellationToken)
        {

            var logOutput = new List<string>();


            string blenderExePath = BlenderExecutablePath;
            string blenderScriptPath = BlenderThumbnailScriptPath;

            /****** CHECK THE SECRETS TO MAKE SURE YOU'RE PUSHING TO CORRECT ENV *******/

            ///// SETUP THE CORRECT PATHS FIRST /////
            var assetBlobFolder = $"allAssetBlobs.csv";
            //string importFromAzureFolder = $"{_azureOptions.Value.DeltasRootPrefix}";
            //string importFileName = "allDeltas_Metadata.xlsx";
            string importFileName = "aprimorestamp_sampleAssetsWithMetadata_1.xlsx"; // "aprimorestamp_allAssetsWithMetadata1_1.xlsx";//
            ///// SETUP THE CORRECT PATHS FIRST /////
            ///

            var allAssetFiles = File.ReadAllLines($"{SourceDirectory}\\{assetBlobFolder}");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allAssetFiles.Count()} assets in Azure");


            // don't add aprimoId column to spreadsheets that already have it.
            //var fileData = await ReadImportSpreadsheet(importFileName, _azureOptions.Value.ImportsRootPrefix, skipAprimoIdColumns: true);
            var fileData = await ReadImportSpreadsheet(importFileName, _azureOptions.Value.ImportsRootPrefix);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {importFileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {importFileName}");

            bool hasStarted = true;
            foreach (var rowData in fileData)
            {

                var uuid = rowData["Id"];
                var path = rowData["Path"];
                //var status = rowData["Status"];
                var status = "New";

                //if (uuid == "8f271dc3-ad46-40c5-bc26-e1c46db08d50")
                //{
                //    hasStarted = true;
                //}

                if (path.EndsWith(".glb") && hasStarted && status.Equals("New"))
                {
                    _logger.LogInformation($"Processing GLB Record {uuid}.");

                    var fullAzurePath = allAssetFiles.Where(f => f.Contains(uuid)).FirstOrDefault();

                    string azureFileName = Path.GetFileName(fullAzurePath);
                    string azurePath = Path.GetDirectoryName(fullAzurePath);

                    string previewFileName = Path.GetFileNameWithoutExtension(path);

                    var aprimoRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(uuid, cancellationToken);

                    if (aprimoRecord != null)
                    {
                        try
                        {
                            var glbStream = await _assetsWrapper.DownloadBlobAsync(azureFileName, azurePath);
                            var pngBytes = await GlbThumbnailRenderer.RenderGlbThumbnailAsync(glbStream, blenderExePath, blenderScriptPath, 2000, cancellationToken);
                            using var pngStream = new MemoryStream(pngBytes);

                            string zipFileName = azureFileName.Replace(".glb", "_3dpackage.zip");

                            await using var zipStream = await _aprimoClient.Build3dPackageZipFromExistingRecordAsync(glbStream, azureFileName, pngStream, "preview.png", cancellationToken);

                            await _aprimoClient.UploadNewVersionFileToRecordAsync(
                                aprimoRecord.Id,
                                zipStream,
                                zipFileName,          // must match your ZIP-identification package rule
                                "application/zip",
                                cancellationToken);

                            //await _aprimoClient.UploadFileToRecordAsync(
                            //    recordId: aprimoRecord.Id,
                            //    fileStream: pngStream,
                            //    fileName: $"{previewFileName}_preview.png",
                            //    contentType: "image/png",
                            //    setAsPreview: true,
                            //    cancellationToken);

                            //File.WriteAllBytes($"{SourceDirectory}test_thumb.png", pngBytes);
                        }
                        catch (GlbThumbnailRenderer.BlenderRenderException ex)
                        {
                            Console.WriteLine("TEMP DIR: " + ex.TempDir);
                            Console.WriteLine("CMD: " + ex.CommandLine);
                            Console.WriteLine("EXIT: " + ex.ExitCode);
                            Console.WriteLine("STDOUT:\n" + ex.StdOut);
                            Console.WriteLine("STDERR:\n" + ex.StdErr);

                            // Now you can open ex.TempDir and see if model.glb exists, etc.
                            //throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"Failure! {ex.Message}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Record {uuid} is not in this env");
                    }


                }

            }



            //if (!importFileName.Contains(".processed"))
            //{
            //    string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{importFileName}";
            //    await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
            //}
            ;
        }

        public async Task UpdateGLBsInAprimoFromAzure(CancellationToken cancellationToken)
        {

            var logOutput = new List<string>();


            string blenderExePath = BlenderExecutablePath;
            string blenderScriptPath = BlenderThumbnailScriptPath;


            //var allOrigAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allAssetBlobs.csv");
            //_logger.LogInformation($"Found {allOrigAssetFiles.Count()} assets in Azure");
            //logOutput.Add($"Found {allOrigAssetFiles.Count()} assets in Azure");

            //var allDeltaAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allDeltaAssetBlobs.csv");
            //_logger.LogInformation($"Found {allDeltaAssetFiles.Count()} delta assets in Azure");
            //logOutput.Add($"Found {allDeltaAssetFiles.Count()} delta assets in Azure");


            var allDeltaAssetFiles = File.ReadAllLines($"{SourceDirectory}\\allDelta3AssetBlobs.csv");
            _logger.LogInformation($"Found {allDeltaAssetFiles.Count()} delta3 assets in Azure");
            logOutput.Add($"Found {allDeltaAssetFiles.Count()} delta3 assets in Azure");

            //var allOrigGLBFiles = allOrigAssetFiles.Where(x => x.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)).ToList();
            var allDeltaGLBFiles = allDeltaAssetFiles.Where(x => x.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)).ToList();

            var allGLBFiles = allDeltaGLBFiles; //allOrigGLBFiles.Concat(allDeltaGLBFiles);
            _logger.LogInformation($"Found {allGLBFiles.Count()} total GLB assets in Azure");

            List<string> processedGUIDs = new List<string>();

            bool hasStarted = true;
            foreach (var glbFile in allGLBFiles)
            {
                if (hasStarted)
                {
                    string azureFileName = Path.GetFileName(glbFile);
                    string[] azureFileNameParts = azureFileName.Split("_");
                    string uuid = azureFileNameParts[0];
                    string azureCleanName = azureFileName.Replace(uuid + "_", "");


                    _logger.LogInformation($"Processing GLB Record {uuid}.");
                    string azurePath = Path.GetDirectoryName(glbFile);

                    var aprimoRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(uuid, cancellationToken);

                    if (aprimoRecord != null && !processedGUIDs.Contains(uuid))
                    {
                        try
                        {
                            var glbStream = await _assetsWrapper.DownloadBlobAsync(azureFileName, azurePath);
                            var pngBytes = await GlbThumbnailRenderer.RenderGlbThumbnailAsync(glbStream, blenderExePath, blenderScriptPath, 2000, cancellationToken);
                            using var pngStream = new MemoryStream(pngBytes);

                            string zipFileName = azureCleanName.Replace(".glb", "_3dpackage.zip");

                            await using var zipStream = await _aprimoClient.Build3dPackageZipFromExistingRecordAsync(glbStream, azureCleanName, pngStream, "preview.png", cancellationToken);

                            await _aprimoClient.UploadNewVersionFileToRecordAsync(
                                aprimoRecord.Id,
                                zipStream,
                                zipFileName,          // must match your ZIP-identification package rule
                                "application/zip",
                                cancellationToken);


                            if (!processedGUIDs.Contains(uuid))
                            {
                                processedGUIDs.Add(uuid);
                            }

                            ;

                        }
                        catch (GlbThumbnailRenderer.BlenderRenderException ex)
                        {
                            Console.WriteLine("TEMP DIR: " + ex.TempDir);
                            Console.WriteLine("CMD: " + ex.CommandLine);
                            Console.WriteLine("EXIT: " + ex.ExitCode);
                            Console.WriteLine("STDOUT:\n" + ex.StdOut);
                            Console.WriteLine("STDERR:\n" + ex.StdErr);

                            // Now you can open ex.TempDir and see if model.glb exists, etc.
                            //throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"Failure! {ex.Message}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Record {uuid} is not in this env");
                    }
                }

                //if (glbFile.Contains("f646cc22-4578-4936-915f-81bd0d7ab959"))
                //{
                //    hasStarted = true;
                //}

            }



            //if (!importFileName.Contains(".processed"))
            //{
            //    string importFile = $"{_azureOptions.Value.ImportsRootPrefix}/{importFileName}";
            //    await _assetsWrapper.MoveBlobAsync(importFile, $"{importFile}.processed");
            //}
            ;
        }


        #endregion MAIN_PROCESSES

        #region HELPER_METHODS

        public async Task CombineFiles(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();
            string importFileName = "allProdMappings.xlsx";

            Dictionary<string, MappingHelperObject> AllProdMappings = new Dictionary<string, MappingHelperObject>();

            var allImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allImagesetBlobs.csv");
            _logger.LogInformation($"Found {allImagesetFiles.Count()} image sets in Azure");

            var allAssetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allAssetBlobs.csv");
            _logger.LogInformation($"Found {allAssetFiles.Count()} assets in Azure");

            string jsonString = File.ReadAllText($"{Dump}\\baseFiles\\initialProdMappings.json");
            Dictionary<string, MappingHelperObject> prodMappings = JsonConvert.DeserializeObject<Dictionary<string, MappingHelperObject>>(jsonString);
            _logger.LogInformation($"Found {prodMappings.Count()} initial AEM to Azure Mappings");

            string aemjsonString = File.ReadAllText($"{Dump}\\baseFiles\\allAEMdataByUUID.json");
            Dictionary<string, MappingHelperObject> aemMappings = JsonConvert.DeserializeObject<Dictionary<string, MappingHelperObject>>(aemjsonString);
            _logger.LogInformation($"Found {aemMappings.Count()} initial AEM Data Mappings");

            var stream = File.Open($"{Dump}\\baseFiles\\webimageSuccess.xlsx", FileMode.Open, FileAccess.Read);
            var knockOutsData = await ReadImportSpreadsheet(stream, false);
            _logger.LogInformation($"Found {knockOutsData.Count()} knockouts");


            string imageSetAssetsjsonString = File.ReadAllText($"{Dump}\\baseFiles\\allImageSetAssets.json");
            Dictionary<string, AprimoImageSetAssets> allAssetImageSetAssets = JsonConvert.DeserializeObject<Dictionary<string, AprimoImageSetAssets>>(imageSetAssetsjsonString);
            _logger.LogInformation($"Found {allAssetImageSetAssets.Count()} image set assets");

            int prodMappingCounter = 0;
            foreach (var azureFilePath in allAssetFiles)
            {
                string azureFileName = Path.GetFileName(azureFilePath);
                string[] azureFileNameParts = azureFileName.Split("_");
                string aemUUID = azureFileNameParts[0];
                string azureCleanName = azureFileName.Replace(aemUUID + "_", "");
                MappingHelperObject mho = new MappingHelperObject();

                mho.AemAssetId = aemUUID;

                if (aemMappings.ContainsKey(aemUUID))
                {
                    mho.AemAssetName = aemMappings[aemUUID].AemAssetName;
                    mho.AemAssetPath = aemMappings[aemUUID].AemAssetPath;
                    mho.AemCreatedDate = aemMappings[aemUUID].AemCreatedDate;
                }
                else
                {
                    var aemMappingFromKnockouts = knockOutsData.Where(x => x["Id"].Equals(aemUUID)).FirstOrDefault();
                    if (aemMappingFromKnockouts != null)
                    {
                        mho.AemAssetName = Path.GetFileName(aemMappingFromKnockouts["Path"]);
                        mho.AemAssetPath = aemMappingFromKnockouts["Path"];
                        mho.AemCreatedDate = aemMappingFromKnockouts["Created"];
                        mho.AprimoId = aemMappingFromKnockouts["AprimoId"];
                    }
                    else
                    {
                        _logger.LogInformation($"Could not map aem data for {aemUUID} !!");
                        logOutput.Add($"Could not map aem data for {aemUUID} !!");
                    }
                }


                mho.AzureAssetPath = azureFilePath;
                mho.AzureAssetName = azureCleanName;

                if (string.IsNullOrEmpty(mho.AprimoId))
                {
                    if (prodMappings.ContainsKey(aemUUID))
                    {
                        mho.AprimoId = prodMappings[aemUUID].AprimoId;
                    }
                    else
                    {
                        _logger.LogInformation($"Could not map Aprimo Id for {aemUUID} !!");
                        logOutput.Add($"Could not map Aprimo Id for {aemUUID} !!");
                    }

                }

                // imagesets here isn't really important, but can include for reporting
                var imageSets = allAssetImageSetAssets.Where(x => x.Value.Resources.Contains(mho.AemAssetPath)).Select(y => y.Key).ToList();
                mho.ImageSets.AddRange(imageSets);
                mho.ImageSetCount = imageSets.Count();
                // _logger.LogInformation($"found {imageSets.Count} image sets for : {aemUUID}");

                if (!AllProdMappings.ContainsKey(aemUUID))
                {
                    AllProdMappings.Add(aemUUID, mho);
                    prodMappingCounter++;
                }
                else
                {
                    _logger.LogInformation($"Umm, found duplicate for {aemUUID} !!");
                    logOutput.Add($"Umm, found duplicate for {aemUUID} !!");

                }


                _logger.LogInformation($"Processed {prodMappingCounter} : {aemUUID}");
            }


            // Serialize to a JSON string 
            string newJsonString = JsonConvert.SerializeObject(AllProdMappings, Formatting.None);
            File.WriteAllText($"{Dump}\\baseFiles\\AllProdMappings.json", newJsonString);

            await LogToAzure(importFileName, logOutput);
        }
        public static bool ContainsPathIgnoringGuid(string fullPathWithGuid, string expectedPathWithoutGuid)
    {
        if (string.IsNullOrWhiteSpace(fullPathWithGuid) ||
            string.IsNullOrWhiteSpace(expectedPathWithoutGuid))
            return false;

        // Normalize slashes
        fullPathWithGuid = fullPathWithGuid.Replace('\\', '/');
        expectedPathWithoutGuid = expectedPathWithoutGuid.Replace('\\', '/');

        // Split path
        var directory = Path.GetDirectoryName(fullPathWithGuid)?.Replace('\\', '/');
        var fileName = Path.GetFileName(fullPathWithGuid);

        if (directory == null || fileName == null)
            return false;

        // Remove GUID_ prefix if present
        // Matches: {guid}_
        fileName = Regex.Replace(
            fileName,
            @"^[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}_",
            "",
            RegexOptions.IgnoreCase);

        // Rebuild normalized path
        var normalized = $"{directory}/{fileName}";

        // Case-insensitive contains check
        return normalized
            .IndexOf(expectedPathWithoutGuid, StringComparison.OrdinalIgnoreCase) >= 0;
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

        public async Task DigestDelta(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();

            /****** CHECK THE SECRETS TO MAKE SURE YOU'RE PUSHING TO CORRECT ENV *******/

            ///// SETUP THE CORRECT PATHS FIRST /////
            //string connectionString = @"Server=(LocalDB)\MSSQLLocalDB;Integrated Security=true;Initial Catalog=ashley;";
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository metaDataRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.AssetMetadata");
            MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjects");
            MappingHelperObjectsRepository imageSetsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSets");
            MappingHelperObjectsRepository imageSetsRelationsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSetsRelations");
            MappingHelperObjectsRepository mhoFlatRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjectsFlat");
            var assetBlobFolder = $"allAssetBlobs.csv";
            var assetDeltasBlobFolder = $"allDeltaAssetBlobs.csv";
            var assetDeltas2BlobFolder = $"allDelta2AssetBlobs.csv";
            var assetDeltas3BlobFolder = $"allDelta3AssetBlobs.csv";
            string importFromAzureFolder = $"{_azureOptions.Value.Deltas3RootPrefix}";
            string importFromOrigAzureFolder = $"{_azureOptions.Value.AssetRootPrefix}";
            string importFileName = "allDeltas3_Metadata_updated.xlsx";
            ///// SETUP THE CORRECT PATHS FIRST /////
            ///

            ;
            var allDeltasAssetFiles = File.ReadAllLines($"{SourceDirectory}\\{assetDeltasBlobFolder}");
            _logger.LogInformation($"Found {allDeltasAssetFiles.Count()} delta assets in Azure");
            logOutput.Add($"Found {allDeltasAssetFiles.Count()} delta assets in Azure");

            var allDeltas2AssetFiles = File.ReadAllLines($"{SourceDirectory}\\{assetDeltas2BlobFolder}");
            _logger.LogInformation($"Found {allDeltas2AssetFiles.Count()} delta2 assets in Azure");
            logOutput.Add($"Found {allDeltas2AssetFiles.Count()} delta2 assets in Azure");

            var allDeltas3AssetFiles = File.ReadAllLines($"{SourceDirectory}\\{assetDeltas3BlobFolder}");
            _logger.LogInformation($"Found {allDeltas3AssetFiles.Count()} delta3 assets in Azure");
            logOutput.Add($"Found {allDeltas3AssetFiles.Count()} delta3 assets in Azure");

            var allOrigAssetFiles = File.ReadAllLines($"{SourceDirectory}\\{assetBlobFolder}");
            _logger.LogInformation($"Found {allOrigAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allOrigAssetFiles.Count()} assets in Azure");

            string[] allAssetFiles = allOrigAssetFiles.Concat(allDeltasAssetFiles).Concat(allDeltas2AssetFiles).Concat(allDeltas3AssetFiles).ToArray();


            // don't add aprimoId column to spreadsheets that already have it.
            var fileData = await ReadImportSpreadsheet(importFileName, _azureOptions.Value.ImportsRootPrefix, skipAprimoIdColumns: true);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {importFileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {importFileName}");

            bool hasStarted = true;
            foreach (var rowData in fileData)
            {
                var uuid = rowData["Id"];

                //if (uuid == "3ab4980b-05f4-4b63-ace8-9cd780506b95")
                //{
                //    hasStarted = true;
                //}

                var path = rowData["Path"];
                var status = rowData["Status"];
                var byteSize = rowData["SizeBytes"];
                string aprimoId = string.Empty; // want to run against both kinds of files - those that have processed to aprimo and those not.
                try
                {
                    aprimoId = rowData["AprimoId"];
                } catch (Exception ex) {
                    _logger.LogInformation($"File does not have AprimoId for {uuid}");
                    logOutput.Add($"File does not have AprimoId for {uuid}");
                }
                
                var created = rowData["Created"];
                var name = rowData["Name"];
                var fileName = Path.GetFileName(path);
                string cleanedFilename = Regex.Replace(fileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

                var assetFolder = $"{importFromAzureFolder}{Path.GetDirectoryName(path)}";

                if (hasStarted)
                {
                    if (string.IsNullOrEmpty(byteSize) || byteSize == "0")
                    {
                        // process image set
                        string folderHash = GetFolderHash(Path.GetDirectoryName(path));
                        string uniqueId = $"{folderHash}_{cleanedFilename}";
                        string azureMetadataFilename = $"{folderHash}_{cleanedFilename}_metadata.json";
                        string azureRelatedFilename = $"{folderHash}_{cleanedFilename}_related.json";
                        // get image set metadata
                        string imageSetjson = await ReadJsonFile(azureMetadataFilename, assetFolder);
                        // get image set relations
                        string imageSetRelatedjson = await ReadJsonFile(azureRelatedFilename, assetFolder);
                        // get image set relations mapped

                        // upsert image set metadata
                        imageSetsRepo.UpsertJsonBody(uniqueId, imageSetjson);
                        _logger.LogInformation($"Updated ImageSetsRepo for {uniqueId}");
                        logOutput.Add($"Updated ImageSetsRepo for {uniqueId}");
                        ;


                        // upsert image set mappings
                        if (status.Equals("Needs Update") || status.Equals("New"))
                        {
                            var imageSetAssets = await CreateImageSetAssetsFromPath(path, importFromAzureFolder, allAssetFiles, cancellationToken, logOutput);
                            string jsonImageAssets = JsonConvert.SerializeObject(imageSetAssets);
                            imageSetsRelationsRepo.UpsertJsonBody(uniqueId, jsonImageAssets);
                            _logger.LogInformation($"Updated ImageSetsRelationsRepo for {uniqueId}");
                            logOutput.Add($"Updated ImageSetsRelationsRepo for {uniqueId}");
                            ;
                        }

                    }
                    else
                    {
                        // handle empty uuid binaries
                        bool bHasHashedUUID = false;
                        if (string.IsNullOrEmpty(uuid))
                        {
                            uuid = GetFolderHash(path);
                            bHasHashedUUID = true;
                        }

                        //process asset
                        string azureMetadataFilename = $"{uuid}_metadata.json";
                        string azureFilename = $"{uuid}_{cleanedFilename}";
                        string azureAssetPath = allDeltasAssetFiles.Where(x => x.Contains(uuid)).FirstOrDefault();

                        if (status.Equals("Needs Update")) // gets new metadata from Delta, and updates the value in the DB.
                        {
                            // get asset metadata
                            string json = await ReadJsonFile(azureMetadataFilename, assetFolder);
                            // upsert asset metadata
                            metaDataRepo.UpsertJsonBody(uuid, json);
                            _logger.LogInformation($"Updated MetadataRepo for {uuid}");
                            logOutput.Add($"Updated MetadataRepo for {uuid}");
                            ;

                        }
                        else if (status.Equals("New") && !string.IsNullOrEmpty(aprimoId))
                        {
                            //bool fileMetadataExists = await _assetsWrapper.BlobExistsAsync($"{azureMetadataFilename}", assetFolder);
                            //if (!fileMetadataExists)
                            //{

                            //}


                            // get asset metadata
                            string json = await ReadJsonFile(azureMetadataFilename, assetFolder);
                            // upsert asset metadata
                            metaDataRepo.UpsertJsonBody(uuid, json);

                            // build mapping helper record
                            MappingHelperObject mho = new MappingHelperObject();

                            mho.AemCreatedDate = created;
                            mho.AemAssetId = uuid;
                            mho.AemAssetName = Path.GetFileName(path);
                            mho.AemAssetPath = path;

                            mho.AprimoId = aprimoId;

                            mho.AzureAssetPath = azureAssetPath;
                            mho.AzureAssetName = cleanedFilename;

                            // upsert asset mapping record
                            string jsonMHO = JsonConvert.SerializeObject(mho);
                            mhoRepo.UpsertJsonBody(uuid, jsonMHO);
                            _logger.LogInformation($"Updated MHORepo for {uuid}");
                            logOutput.Add($"Updated MHORepo for {uuid}");

                            await mhoFlatRepo.UpsertMHOFlatAsync(mho);
                            ;
                        }

                    }
                }



            }
        }

        public async Task DigestDeltaUpdateOnly(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();

            /****** CHECK THE SECRETS TO MAKE SURE YOU'RE PUSHING TO CORRECT ENV *******/

            ///// SETUP THE CORRECT PATHS FIRST /////
            //string connectionString = @"Server=(LocalDB)\MSSQLLocalDB;Integrated Security=true;Initial Catalog=ashley;";
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository metaDataRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.AssetMetadata");
            MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjects");
            MappingHelperObjectsRepository imageSetsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSets");
            MappingHelperObjectsRepository imageSetsRelationsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSetsRelations");
            MappingHelperObjectsRepository mhoFlatRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjectsFlat");
            var assetBlobFolder = $"allAssetBlobs.csv";
            var assetDeltasBlobFolder = $"allDeltaAssetBlobs.csv";
            var assetDeltas2BlobFolder = $"allDelta2AssetBlobs.csv";
            var assetDeltas3BlobFolder = $"allDelta3AssetBlobs.csv";
            string importFromAzureFolder = $"{_azureOptions.Value.Deltas3RootPrefix}";
            string importFromOrigAzureFolder = $"{_azureOptions.Value.AssetRootPrefix}";
            string importFileName = "allDeltas3_Metadata_updated.xlsx";
            ///// SETUP THE CORRECT PATHS FIRST /////
            ///

            ;
            var allDeltasAssetFiles = File.ReadAllLines($"{SourceDirectory}\\{assetDeltasBlobFolder}");
            _logger.LogInformation($"Found {allDeltasAssetFiles.Count()} delta assets in Azure");
            logOutput.Add($"Found {allDeltasAssetFiles.Count()} delta assets in Azure");

            var allDeltas2AssetFiles = File.ReadAllLines($"{SourceDirectory}\\{assetDeltas2BlobFolder}");
            _logger.LogInformation($"Found {allDeltas2AssetFiles.Count()} delta2 assets in Azure");
            logOutput.Add($"Found {allDeltas2AssetFiles.Count()} delta2 assets in Azure");

            var allDeltas3AssetFiles = File.ReadAllLines($"{SourceDirectory}\\{assetDeltas3BlobFolder}");
            _logger.LogInformation($"Found {allDeltas3AssetFiles.Count()} delta3 assets in Azure");
            logOutput.Add($"Found {allDeltas3AssetFiles.Count()} delta3 assets in Azure");

            var allOrigAssetFiles = File.ReadAllLines($"{SourceDirectory}\\{assetBlobFolder}");
            _logger.LogInformation($"Found {allOrigAssetFiles.Count()} assets in Azure");
            logOutput.Add($"Found {allOrigAssetFiles.Count()} assets in Azure");

            string[] allAssetFiles = allOrigAssetFiles.Concat(allDeltasAssetFiles).Concat(allDeltas2AssetFiles).Concat(allDeltas3AssetFiles).ToArray();


            // don't add aprimoId column to spreadsheets that already have it.
            var fileData = await ReadImportSpreadsheet(importFileName, _azureOptions.Value.ImportsRootPrefix, skipAprimoIdColumns: true);

            _logger.LogInformation($"Processing {fileData.Count()} rows from {importFileName}");
            logOutput.Add($"Processing {fileData.Count()} rows from {importFileName}");

            bool hasStarted = true;
            foreach (var rowData in fileData)
            {
                var uuid = rowData["Id"];

                //if (uuid == "3ab4980b-05f4-4b63-ace8-9cd780506b95")
                //{
                //    hasStarted = true;
                //}

                var path = rowData["Path"];
                var status = rowData["Status"];
                var byteSize = rowData["SizeBytes"];
                string aprimoId = string.Empty; // want to run against both kinds of files - those that have processed to aprimo and those not.
                try
                {
                    aprimoId = rowData["AprimoId"];
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"File does not have AprimoId for {uuid}");
                    logOutput.Add($"File does not have AprimoId for {uuid}");
                }

                var created = rowData["Created"];
                var name = rowData["Name"];
                var fileName = Path.GetFileName(path);
                string cleanedFilename = Regex.Replace(fileName, @"[^\w\-.\/]", "_").TrimEnd('_').Trim();

                var assetFolder = $"{importFromAzureFolder}{Path.GetDirectoryName(path)}";

                if (hasStarted)
                {
                    if (string.IsNullOrEmpty(byteSize) || byteSize == "0")
                    {
                        // process image set
                        string folderHash = GetFolderHash(Path.GetDirectoryName(path));
                        string uniqueId = $"{folderHash}_{cleanedFilename}";
                        string azureMetadataFilename = $"{folderHash}_{cleanedFilename}_metadata.json";
                        string azureRelatedFilename = $"{folderHash}_{cleanedFilename}_related.json";
                        // get image set metadata
                        string imageSetjson = await ReadJsonFile(azureMetadataFilename, assetFolder);
                        // get image set relations
                        string imageSetRelatedjson = await ReadJsonFile(azureRelatedFilename, assetFolder);
                        // get image set relations mapped

                        // upsert image set metadata
                        imageSetsRepo.UpsertJsonBody(uniqueId, imageSetjson);
                        _logger.LogInformation($"Updated ImageSetsRepo for {uniqueId}");
                        logOutput.Add($"Updated ImageSetsRepo for {uniqueId}");
                        ;


                        // upsert image set mappings
                        if (status.Equals("Update"))
                        {
                            var imageSetAssets = await CreateImageSetAssetsFromPath(path, importFromAzureFolder, allAssetFiles, cancellationToken, logOutput);
                            string jsonImageAssets = JsonConvert.SerializeObject(imageSetAssets);
                            imageSetsRelationsRepo.UpsertJsonBody(uniqueId, jsonImageAssets);
                            _logger.LogInformation($"Updated ImageSetsRelationsRepo for {uniqueId}");
                            logOutput.Add($"Updated ImageSetsRelationsRepo for {uniqueId}");
                            ;
                        }

                    }
                    else
                    {
                        // handle empty uuid binaries
                        bool bHasHashedUUID = false;
                        if (string.IsNullOrEmpty(uuid))
                        {
                            uuid = GetFolderHash(path);
                            bHasHashedUUID = true;
                        }

                        //process asset
                        string azureMetadataFilename = $"{uuid}_metadata.json";
                        string azureFilename = $"{uuid}_{cleanedFilename}";
                        string azureAssetPath = allDeltasAssetFiles.Where(x => x.Contains(uuid)).FirstOrDefault();

                        if (status.Equals("Update")) // gets new metadata from Delta, and updates the value in the DB.
                        {
                            // get asset metadata
                            string json = await ReadJsonFile(azureMetadataFilename, assetFolder);
                            // upsert asset metadata
                            metaDataRepo.UpsertJsonBody(uuid, json);
                            _logger.LogInformation($"Updated MetadataRepo for {uuid}");
                            logOutput.Add($"Updated MetadataRepo for {uuid}");
                            ;

                        }

                    }
                }



            }
        }

        public async Task FixImageSetPaths(CancellationToken cancellationToken)
        {
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository imageSetsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSets");

            ///// fixing Imageset paths..
            var allOrigImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allImagesetBlobs.csv");
            var allDeltaImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allDeltaImagesetBlobs.csv");
            var allDelta2ImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allDelta2ImagesetBlobs.csv");

            var allDelta3Files = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.Deltas3RootPrefix);
            var allDelta3ImagesetFiles = allDelta3Files.Where(x => x.EndsWith("_related.json")).ToList();
            MemoryStream stream = ConvertListToMemoryStream(allDelta3ImagesetFiles);
            SaveStreamToFile(stream, SourceDirectory, "allDelta3ImagesetBlobs.csv");

            //var allDelta3ImagesetFiles = File.ReadAllLines($"{Dump}\\baseFiles\\allDelta3ImagesetBlobs.csv");
            _logger.LogInformation($"Found {allOrigImagesetFiles.Count()} orig image sets in Azure");
            _logger.LogInformation($"Found {allDeltaImagesetFiles.Count()} delta image sets in Azure");
            _logger.LogInformation($"Found {allDelta2ImagesetFiles.Count()} delta2 image sets in Azure");
            _logger.LogInformation($"Found {allDelta3ImagesetFiles.Count()} delta3 image sets in Azure");
            var allImagesetFiles = allOrigImagesetFiles.Concat(allDeltaImagesetFiles).Concat(allDelta2ImagesetFiles).Concat(allDelta3ImagesetFiles);
            _logger.LogInformation($"Found {allImagesetFiles.Count()} ALL image sets in Azure");
            int updatedCount = 0;
            foreach (var blob in allImagesetFiles)
            {
                string imageSetPath = blob.Replace("\\", "/").Replace("_related.json", "").Replace($"{_azureOptions.Value.AssetRootPrefix}/", "/").Replace($"{_azureOptions.Value.DeltasRootPrefix}/", "/").Replace($"{_azureOptions.Value.Deltas2RootPrefix}/", "/");
                var imageSetFileName = Path.GetFileName(imageSetPath);
                var imageSetPathOnly = Path.GetDirectoryName(imageSetPath);
                string folderHash = GetFolderHash(imageSetPathOnly);
                string uniqueId = $"{imageSetFileName}";

                string json = imageSetsRepo.GetJsonBodyByDictKey(uniqueId);
                AprimoImageSet ais = JsonConvert.DeserializeObject<AprimoImageSet>(json);

                if (ais != null)
                {
                    if (ais.PathToImageSet == null)
                    {
                        ais.PathToImageSet = imageSetPath;
                        string updatedAIS = JsonConvert.SerializeObject(ais);
                        imageSetsRepo.UpdateJsonBody(uniqueId, updatedAIS);
                        updatedCount++;
                    }

                }
                else
                {
                    _logger.LogInformation($"Cannot find ImageSet {uniqueId}");
                }
            }
            ///
            _logger.LogInformation($"updated {updatedCount} Image Sets.");
        }
        public async Task DoMinuteTasks(CancellationToken cancellationToken)
        {

            string filePath = $"{SourceDirectory}update_automatedruns.txt";
            string automatedRunsFolder = "automatedruns"; // folder with Excel files
            string queueJobsContainerName = "queuejobs";

            // Run until app shuts down
            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Task running at: {time}", DateTimeOffset.Now);

                var logsFiles = await _assetsWrapper.GetBlobListingAsync(_azureOptions.Value.LogsRootPrefix);
                var logBlobNames = logsFiles.ToList();
                _logger.LogWarning($"Found {logBlobNames.Count} Log Blobs.");

                var automatedFiles = await _assetsWrapper.GetBlobListingAsync(automatedRunsFolder);
                var runsBlobNames = automatedFiles.ToList();
                _logger.LogWarning($"Found {runsBlobNames.Count} Automated Runs Blobs.");

                // Get unique keys from log blobs
                var logKeys = new HashSet<string>(
                    logBlobNames.Select(f => GetExcelKey(f))
                );

                // Find Excel blobs whose key is NOT in log keys
                var unmatchedExcelBlobs = runsBlobNames
                    .Where(excelBlob => !logKeys.Contains(GetExcelKey(excelBlob)))
                    .ToList();

                _logger.LogWarning($"Found {unmatchedExcelBlobs.Count} Excel files to process.");

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
                    // Copy to queuefiles container with same name
                    // only queue up to X number of items
                    int queuecount = 0;
                    foreach (var blobName in unmatchedExcelBlobs)
                    {
                        if (lines.Contains(blobName))
                        {
                            _logger.LogWarning($"Blobname {blobName} is already automated.  skipping.");
                        }
                        else
                        {
                            var sourceExcelBlob = await _assetsWrapper.GetBlobClientAsync(blobName);
                            var queueFilesContainer = _blobServiceClient.GetBlobContainerClient(queueJobsContainerName);
                            string destFileName = Path.GetFileName(blobName);
                            var destBlob = queueFilesContainer.GetBlobClient(destFileName);

                            await destBlob.StartCopyFromUriAsync(sourceExcelBlob.Uri);

                            _logger.LogInformation($"Copied logs/{blobName} to {queueJobsContainerName}");

                            //await WaitForBlobToBeDeletedAsync(queueFilesContainer, destFileName);

                            await Task.Delay(TimeSpan.FromSeconds(30));

                            queuecount++;
                            lines.Add(blobName);
                        }
                        if (queuecount == 1)
                        {
                            break;
                        }
                    }
                    File.WriteAllLines(filePath, lines);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Oops! something bad happened: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);
            }
        }
        static string EscapeTagValue(string value) => (value ?? "").Replace("'", "''");

        public static bool FieldHasMoreThanOneLocalizedValue(AprimoRecord record, string fieldName)
        {
            var field = record?.Embedded?.Fields?.Items?
                .FirstOrDefault(f => string.Equals(f.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));

            var localizedCount = field?.LocalizedValues?.Count ?? 0;
            if (localizedCount > 0)
            {
                return (field.LocalizedValues[0].Values?.Count ?? 0) > 1;
            }
            return false;
        }

        public async Task FixMissingRelations(CancellationToken cancellationToken)
        {
            //
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository metaDataRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.AssetMetadata");
            MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjects");
            MappingHelperObjectsRepository imageSetsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSets");
            MappingHelperObjectsRepository imageSetsRelationsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSetsRelations");
            MappingHelperObjectsRepository mhoFlatRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjectsFlat");
            

            List<string> allISDictKeys = imageSetsRelationsRepo.GetAllDictKeys();
            int imageSetCounter = 0;
            int updateCounter = 0;
            int zeroResourcesCount = 0;
            int mismatchResourcesCount = 0;
            List<string> unMatchedResources = new List<string>();
            List<string> updatedImagesets = new List<string>();
            foreach (var dictKey in allISDictKeys)
            {
                imageSetCounter++;
                _logger.LogInformation($"{imageSetCounter}: checking imageset {dictKey}.");
                string aisaJson = imageSetsRelationsRepo.GetJsonBodyByDictKey(dictKey);
                AprimoImageSetAssets aisa = JsonConvert.DeserializeObject<AprimoImageSetAssets>(aisaJson);
                int resCount = aisa.Resources.Count();
                int aprCount = aisa.AprimoRecords.Count();
                bool aprimoContainsNull = aisa.AprimoRecords.Contains(null);
                if (resCount > 0)
                {
                    if (aprimoContainsNull) {
                        _logger.LogInformation("freaking image set relations contains a null.");
                    }
                    
                    if (resCount != aprCount || aprimoContainsNull)
                    {
                        _logger.LogInformation($"resourceCount {resCount} does not equal Aprimo count {aprCount}");
                        List<string> azureResources = new List<string>();
                        List<string> aprimoResources = new List<string>();
                        int matchesCount = 0;
                        foreach (string resource in aisa.Resources)
                        {
                            var matchedResource = await mhoFlatRepo.GetByAemAssetPathAsync(resource,cancellationToken);

                            if (matchedResource != null)
                            {
                                matchesCount++;
                                azureResources.Add(matchedResource.AzureAssetPath);
                                aprimoResources.Add(matchedResource.AprimoId);
                            } else
                            {
                                if (!unMatchedResources.Contains(resource))
                                {
                                    unMatchedResources.Add(resource);
                                    _logger.LogInformation($"found unmatched resource {resource}");
                                }
                            }
                        }

                        // update image set relation
                        aisa.AzureResources = azureResources;
                        aisa.AprimoRecords = aprimoResources;
                        string newAisa = JsonConvert.SerializeObject(aisa);
                        imageSetsRelationsRepo.UpdateJsonBody(dictKey, newAisa);
                        _logger.LogInformation($"updated imageset relations {dictKey}");
                        updatedImagesets.Add(dictKey);
                        updateCounter++;

                        if (matchesCount != aprCount)
                        {
                            mismatchResourcesCount++;
                        }
                    }

                } else
                {
                    zeroResourcesCount++;
                }

            }

            MemoryStream stream = ConvertListToMemoryStream(unMatchedResources);
            SaveStreamToFile(stream, SourceDirectory, "allUnmatchedResources.csv");
            stream.Dispose();

            MemoryStream ustream = ConvertListToMemoryStream(updatedImagesets);
            SaveStreamToFile(ustream, SourceDirectory, "updatedImageSets.csv");
            ustream.Dispose();

            _logger.LogInformation($"{zeroResourcesCount} Imagesets with Zero Resources.  {mismatchResourcesCount} Imagesets with mismatch count.");

        }

        public async Task FixMissingRelationsInMHO(CancellationToken cancellationToken)
        {
            //
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository metaDataRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.AssetMetadata");
            MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjects");
            MappingHelperObjectsRepository imageSetsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSets");
            MappingHelperObjectsRepository imageSetsRelationsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSetsRelations");
            MappingHelperObjectsRepository mhoFlatRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjectsFlat");


            List<MappingHelperObject> missingAprimoId = await mhoFlatRepo.GetWhereAprimoIdIsNullAsync(cancellationToken);
            int counter = 0;
            foreach (var missingMHO in missingAprimoId)
            {
                counter++;
                var aprimoRecord = await _aprimoClient.GetAssetByAemAssetIdAsync(missingMHO.AemAssetId, cancellationToken);

                if (aprimoRecord != null)
                {
                    await mhoFlatRepo.UpdateAprimoIdAsync(missingMHO.AemAssetId, aprimoRecord.Id, cancellationToken);
                    _logger.LogInformation($"{counter}: Updated MHO Flat with AprimoId {aprimoRecord.Id}");
                    string json = mhoRepo.GetJsonBodyByDictKey(missingMHO.AemAssetId);
                    MappingHelperObject badMho = JsonConvert.DeserializeObject<MappingHelperObject>(json);

                    if (badMho != null)
                    {
                        badMho.AprimoId = aprimoRecord.Id;
                        string newJson = JsonConvert.SerializeObject(badMho);
                        mhoRepo.UpdateJsonBody(missingMHO.AemAssetId, newJson);
                        _logger.LogInformation($"{counter}:Updated MHO with AprimoId {aprimoRecord.Id}");
                    }
                } else
                {
                    _logger.LogInformation($"{counter}: What the Heck! Can't find Aprimo asset for AemId {missingMHO.AemAssetId}");
                }

            }


            //_logger.LogInformation($"{zeroResourcesCount} Imagesets with Zero Resources.  {mismatchResourcesCount} Imagesets with mismatch count.");

        }

        public async Task FindPossibleMetadataProperties(CancellationToken cancellationToken)
        {
            //
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository metaDataRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.AssetMetadata");
            MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjects");
            MappingHelperObjectsRepository imageSetsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSets");
            MappingHelperObjectsRepository imageSetsRelationsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSetsRelations");
            //MappingHelperObjectsRepository mhoFlatRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjectsFlat");


            List<string> allISDictKeys = metaDataRepo.GetAllDictKeys(); // imageSetsRepo.GetAllDictKeys();
            int imageSetCounter = 0;

            var uniquePropertyNames = new HashSet<string>();
            
            foreach (var dictKey in allISDictKeys)
            {
                imageSetCounter++;
                _logger.LogInformation($"{imageSetCounter}: processing imageset {dictKey}.");
                string aisJson = metaDataRepo.GetJsonBodyByDictKey(dictKey); //imageSetsRepo.GetJsonBodyByDictKey(dictKey);
                var jObject = JObject.Parse(aisJson);
                var propertyNames = jObject.Properties().Select(p => p.Name).ToList();

                foreach (var name in propertyNames)
                {
                    uniquePropertyNames.Add(name); // duplicates automatically ignored
                }


            }

            var finalList = uniquePropertyNames.ToList();
            finalList.Sort();
            MemoryStream stream = ConvertListToMemoryStream(finalList);
            SaveStreamToFile(stream, SourceDirectory, "allAssetMetadataProperties.csv");
            stream.Dispose();


        }
        public async Task FixMissingImageSetCountsInMHO(CancellationToken cancellationToken)
        {
            //
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";
            MappingHelperObjectsRepository metaDataRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.AssetMetadata");
            MappingHelperObjectsRepository mhoRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjects");
            MappingHelperObjectsRepository imageSetsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSets");
            MappingHelperObjectsRepository imageSetsRelationsRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.ImageSetsRelations");
            MappingHelperObjectsRepository mhoFlatRepo = new MappingHelperObjectsRepository(connectionString, "ashley.dbo.MappingHelperObjectsFlat");


            //List<string> allISDictKeys = imageSetsRelationsRepo.GetAllDictKeys();
            List<string> allISDictKeys = imageSetsRelationsRepo.GetAllRecentDictKeys();
            int imageSetCounter = 0;
            int updateCounter = 0;
            int zeroResourcesCount = 0;
            int mismatchResourcesCount = 0;
            List<string> unMatchedResources = new List<string>();
            List<string> updatedImagesets = new List<string>();
            Dictionary<string, List<string>> mhoToImagesets = new Dictionary<string, List<string>>();
            foreach (var dictKey in allISDictKeys)
            {
                imageSetCounter++;
                _logger.LogInformation($"{imageSetCounter}: processing imageset {dictKey}.");
                string aisaJson = imageSetsRelationsRepo.GetJsonBodyByDictKey(dictKey);
                AprimoImageSetAssets aisa = JsonConvert.DeserializeObject<AprimoImageSetAssets>(aisaJson);

                if (aisa.AprimoRecords.Count() > 0)
                {
                    foreach (string aprimoId in aisa.AprimoRecords)
                    {
                        var matchedResource = await mhoFlatRepo.GetByAprimoIdAsync(aprimoId, cancellationToken);

                        if (matchedResource != null)
                        {
                            if (mhoToImagesets.ContainsKey(matchedResource.AemAssetId))
                            {
                                List<string> currentImageSets = mhoToImagesets[matchedResource.AemAssetId];
                                if (!currentImageSets.Contains(dictKey))
                                {
                                    mhoToImagesets[matchedResource.AemAssetId].Add(dictKey);
                                }
                            }
                            else
                            {
                                List<string> currentImageSets = new List<string>();
                                currentImageSets.Add(dictKey);
                                mhoToImagesets[matchedResource.AemAssetId] = currentImageSets;

                            }
                        } else
                        {
                            _logger.LogInformation($"Not good.  could not match record to aprimoId {aprimoId}");
                        }

                    }
                    
                }
            }

            foreach (string dictKey in mhoToImagesets.Keys)
            {
                string mhoJson = mhoRepo.GetJsonBodyByDictKey(dictKey);
                MappingHelperObject mho = JsonConvert.DeserializeObject<MappingHelperObject>(mhoJson);
                List<string> imageSets = mhoToImagesets[dictKey];
                mho.ImageSets = imageSets;
                string newJson = JsonConvert.SerializeObject(mho);
                mhoRepo.UpdateJsonBody(dictKey, newJson);
                await mhoFlatRepo.UpdateImageSetsAsync(dictKey, imageSets);
            }



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
        string GetExcelKey(string blobName)
        {
            string filename = Path.GetFileName(blobName);
            // For log: remove _DATE... and .log
            // For excel: remove .xlsx
            // Match up to the last "_" before the date
            var match = Regex.Match(filename, @"^(.*_\d+)");
            return match.Success ? match.Groups[1].Value : Path.GetFileNameWithoutExtension(filename);
        }
        private static string GetFolderHash(string folderPath)
        {
            string uniquePart = folderPath.Replace("/content/dam/ashley-furniture/", "");
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(uniquePart);
            byte[] hash = sha256.ComputeHash(bytes);

            return Convert.ToHexString(hash).Substring(0, 16);
        }
        public static int? GetIncrementAfterLastUnderscore(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var underscoreIndex = input.LastIndexOf('_');
            if (underscoreIndex < 0 || underscoreIndex >= input.Length - 1)
                return null;

            var suffix = input.Substring(underscoreIndex + 1);

            if (!int.TryParse(suffix, out var value))
                return null;

            return value + 1;
        }
        public static List<string> GetLocalizedValuesForField(AprimoRecord record, string fieldLabel)
        {
            List<string> ids = new List<string>();
            var field = record?.Embedded?.Fields?.Items?
                .FirstOrDefault(f => string.Equals(f.Label, fieldLabel, StringComparison.OrdinalIgnoreCase) && f.DataType.Equals("ClassificationList"));

            var localizedCount = field?.LocalizedValues?.Count ?? 0;
            if (localizedCount > 0)
            {
                if (field.LocalizedValues[0].Values != null)
                {
                    foreach (var v in field.LocalizedValues[0].Values)
                    {
                        ids.Add(v);
                    }
                }
            }
            return ids;
        }

        public static List<string> GetLocalizedValuesForFieldName(AprimoRecord record, string fieldName)
        {
            List<string> ids = new List<string>();
            var field = record?.Embedded?.Fields?.Items?
                .FirstOrDefault(f => string.Equals(f.FieldName, fieldName, StringComparison.OrdinalIgnoreCase) && !f.DataType.Equals("ClassificationList"));

            var localizedCount = field?.LocalizedValues?.Count ?? 0;
            if (localizedCount > 0)
            {
                if (field.LocalizedValues[0].Values != null)
                {
                    foreach (var v in field.LocalizedValues[0].Values)
                    {
                        ids.Add(v);
                    }
                }
                if (field.LocalizedValues[0].Value != null)
                {
                    ids.Add(field.LocalizedValues[0].Value);
                }
            }
            return ids;
        }
        public void IngestMappingHelperObjects()
        {

            string file = @"C:\Workspace\dump\baseFiles\allImageSets.json";
            string stagingTable = "dbo.AssetMetadata_Staging";

            //string connectionString = @"Server=(LocalDB)\MSSQLLocalDB;Integrated Security=true;Initial Catalog=ashley;";
            string connectionString = @"Server=LAPTOP-T3EQ1P2C;Database=ashley;User Id=sa;Password=dbMV196zt1$;Encrypt=True;TrustServerCertificate=True;";


            // Use a using statement to ensure the connection is closed and disposed
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Connection successful!");
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"Connection failed: {ex.Message}");
                }
            }
            ;
            for (int i = 1; i < 13; i++)
            {
                file = $"C:\\Workspace\\dump\\baseFiles\\allMetadataMappings\\allMetadataMappings_{i}.json";
                JsonDictionaryIngest.IngestFile(file, connectionString, stagingTable);
            }
            ;

            //JsonDictionaryIngest.IngestFile(file, connectionString, stagingTable);
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
        private static string Normalize(string value)
        {
            value = value.Trim();

            value = value.Replace("\\", "/");

            if (!value.StartsWith("/", StringComparison.Ordinal))
                value = "/" + value;

            if (value.Length > 1 && value.EndsWith("/", StringComparison.Ordinal))
                value = value.TrimEnd('/');

            return value;
        }

        public async Task PopulateImageSetAssets(CancellationToken cancellationToken)
        {
            var logOutput = new List<string>();
            string importFileName = "allProdMappings.xlsx";

            string imageSetAssetsjsonString = File.ReadAllText($"{Dump}\\baseFiles\\allImageSetAssets.json");
            Dictionary<string, AprimoImageSetAssets> allAssetImageSetAssets = JsonConvert.DeserializeObject<Dictionary<string, AprimoImageSetAssets>>(imageSetAssetsjsonString);
            _logger.LogInformation($"Found {allAssetImageSetAssets.Count()} image set assets");

            string allProdMappings = File.ReadAllText($"{Dump}\\baseFiles\\AllProdMappings.json");
            Dictionary<string, MappingHelperObject> allProdMappingObjects= JsonConvert.DeserializeObject<Dictionary<string, MappingHelperObject>>(allProdMappings);
            _logger.LogInformation($"Found {allProdMappingObjects.Count()} prod mappings");

            //List<MappingHelperObject> mhos = allProdMappingObjects.Where(x => string.IsNullOrEmpty(x.Value.AemAssetPath)).Select(v => v.Value).ToList();
            List<string> mhos = allProdMappingObjects.Where(x => string.IsNullOrEmpty(x.Value.AemAssetPath)).Select(v => v.Key).ToList();

            

            foreach (var imageSetKey in allAssetImageSetAssets.Keys)
            {
                AprimoImageSetAssets aisa = allAssetImageSetAssets[imageSetKey];
                foreach (var resource in aisa.Resources)
                {
                    MappingHelperObject mho = allProdMappingObjects.Where(x => x.Value.AemAssetPath.Equals(resource)).Select(v => v.Value).FirstOrDefault();
                    if (mho != null)
                    {
                        aisa.AzureResources.Add(mho.AzureAssetPath);
                        aisa.AprimoRecords.Add(mho.AprimoId);
                    } else
                    {
                        _logger.LogInformation($"errmmagerd! can't find matching record for path {resource} in imageset {imageSetKey}");
                        logOutput.Add($"errmmagerd! can't find matching record for path {resource} in imageset {imageSetKey}");
                    }
                }
                if (aisa.Resources.Count() != aisa.AprimoRecords.Count())
                {
                    _logger.LogInformation($"imageset {imageSetKey} : has unmatched resources. AEM: {aisa.Resources.Count()} Aprimo: {aisa.AprimoRecords.Count()}");
                    logOutput.Add($"imageset {imageSetKey} : has unmatched resources. AEM: {aisa.Resources.Count()} Aprimo: {aisa.AprimoRecords.Count()}");
                }
            }
            string jsonString = JsonConvert.SerializeObject(allAssetImageSetAssets, Formatting.None);
            File.WriteAllText($"{Dump}allImageSetAssetsMapped.json", jsonString);

            await LogToAzure(importFileName, logOutput);
        }
        public async Task<IEnumerable<Dictionary<string, string>>> ReadImportSpreadsheet(string fileName, string rootFolder, bool skipEmptyIds = false, bool skipUUIDs = false, bool skipAprimoIdColumns = false)
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
                    if (!skipAprimoIdColumns)
                    {
                        _state.SuccessTable.Columns.Add("AprimoId");
                        _state.RetryTable.Columns.Add("AprimoId");
                    }
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

        public async Task<IEnumerable<Dictionary<string, string>>> ReadImportSpreadsheet(Stream s, bool configureOutput = true)
        {

            var importDataTable = ExcelReader.LoadExcelWorksheetsToDataTables(s).FirstOrDefault() ?? new DataTable();

            // configure output tables
            if (_state.SuccessTable.Columns.Count == 0 && configureOutput)
            {
                DataRow dr = importDataTable.Rows[0];
                foreach (var column in dr.Table.Columns)
                {
                    var columnName = $"{column}";
                    _state.SuccessTable.Columns.Add(columnName);
                    _state.RetryTable.Columns.Add(columnName);
                }
                _state.RetryTable.Columns.Add("Reason");
                _state.SuccessTable.Columns.Add("AprimoId");
                _state.RetryTable.Columns.Add("AprimoId");
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
        public static string ReplaceSuffixAfterLastUnderscore(string input, int newValue)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var underscoreIndex = input.LastIndexOf('_');
            if (underscoreIndex < 0)
                return input;

            return $"{input.Substring(0, underscoreIndex + 1)}{newValue}";
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
        string ToUnicodeDebug(string input)
        {
            return string.Join(" ",
                input.Select(c => $"U+{((int)c):X4}"));
        }

        

        #endregion HELPER_METHODS
    }
}
