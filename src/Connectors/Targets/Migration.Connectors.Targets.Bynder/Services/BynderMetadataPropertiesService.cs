using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Connectors.Sources.WebDam.Configuration;
using Migration.Connectors.Sources.WebDam.Models;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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
using OfficeOpenXml.Table;
using Migration.Shared.Configuration.Hosts.Bynder;


namespace Migration.Connectors.Targets.Bynder.Services
{
    public class BynderMetadataPropertiesService
    {
        private readonly ILogger<BynderMetadataPropertiesService> _logger;
        private readonly IOptions<BynderOptions> _bynderOptions;
        private readonly IBynderClient _bynderClient;
        private readonly IMemoryCache _memoryCache;
        private readonly IConsoleReaderService _reader;
        private readonly IOptions<BynderHostOptions> _hostOptions;

        private static string _logFilename;
        private static string _successRetryFilename;
        private static string _blankMetadataTemplate;
        private static string _metadataFilename;
        private static string _metadataPropertiesFilename;
        private static string _clientMetadataTemplateFilename;
        private static string _metadataTemplateFilename;

        private string _sourceDirectory;
        private string _tempDirectory;

        private Dictionary<string, BynderMetaProperty> _metaProperties;

        public BynderMetadataPropertiesService(
            ILogger<BynderMetadataPropertiesService> logger,
            IOptions<BynderOptions> bynderOptions,
            IOptions<BynderHostOptions> hostOptions,
            IBynderClient bynderClient,
            IMemoryCache memoryCache,
            IConsoleReaderService reader)
        {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bynderOptions = bynderOptions ?? throw new ArgumentNullException(nameof(bynderOptions));
            _bynderClient = bynderClient ?? throw new ArgumentNullException(nameof(bynderClient));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
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
        }

        private sealed class ParsedMetadataHeader
        {
            public string OriginalHeader { get; set; } = string.Empty;
            public string LabelPart { get; set; } = string.Empty;
            public string FieldPart { get; set; } = string.Empty;
        }

        public IReadOnlyList<WebDamMetadataSchemaRow> ReadWebDamMetadataSchema(string excelFilePath)
        {
            if (string.IsNullOrWhiteSpace(excelFilePath))
            {
                throw new ArgumentException("Excel file path is required.", nameof(excelFilePath));
            }

            var file = new FileInfo(excelFilePath);
            if (!file.Exists)
            {
                throw new FileNotFoundException("Excel file not found.", excelFilePath);
            }


            using var package = new ExcelPackage(file);
            var worksheet = package.Workbook.Worksheets["Metadata Schema"];

            if (worksheet == null)
            {
                throw new InvalidOperationException("Worksheet 'Metadata Schema' was not found.");
            }

            var headers = BuildHeaderMap(worksheet);
            var results = new List<WebDamMetadataSchemaRow>();

            var row = 2;
            while (true)
            {
                var field = GetCellString(worksheet, row, headers, "Field");
                var label = GetCellString(worksheet, row, headers, "Label");
                var name = GetCellString(worksheet, row, headers, "Name");
                var type = GetCellString(worksheet, row, headers, "Type");
                var status = GetCellString(worksheet, row, headers, "Status");
                var searchable = GetCellString(worksheet, row, headers, "Searchable");
                var position = GetCellString(worksheet, row, headers, "Position");
                var possibleValues = GetCellString(worksheet, row, headers, "Possible Values");

                if (string.IsNullOrWhiteSpace(field) &&
                    string.IsNullOrWhiteSpace(label) &&
                    string.IsNullOrWhiteSpace(name))
                {
                    break;
                }

                results.Add(new WebDamMetadataSchemaRow
                {
                    Field = field ?? string.Empty,
                    Label = label,
                    Name = name,
                    Type = type,
                    Status = status,
                    Searchable = searchable,
                    Position = position,
                    PossibleValues = possibleValues
                });

                row++;
            }

            return results;
        }

        public IReadOnlyList<BynderMetapropertyCreateRequest> BuildBynderMetapropertyRequestsFromWebDamSchema(
    IReadOnlyList<WebDamMetadataSchemaRow> schemaRows,
    string? prefix)
        {
            var activeRows = schemaRows
                .Where(x => string.Equals(x.Status, "active", StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.IsNullOrWhiteSpace(x.Field))
                .ToList();

            var results = new List<BynderMetapropertyCreateRequest>();

            foreach (var row in activeRows)
            {
                var bynderType = MapWebDamTypeToBynderType(row.Type);
                var dbName = BuildBynderSafeName(row.Field, prefix);
                var label = BuildLabel(row);

                var request = new BynderMetapropertyCreateRequest
                {
                    Name = dbName,
                    Label = label,
                    Type = bynderType,
                    Position = TryParseInt(row.Position),
                    IsSearchable = ParseBoolLike(row.Searchable),
                    Options = SupportsOptions(bynderType)
                        ? ParsePossibleValues(row.PossibleValues)
                        : new List<string>()
                };

                results.Add(request);
            }

            return results;
        }

        public async Task<BynderMetadataImportResult> CreateMetapropertiesFromWebDamExcelAsync(
            ImportWebDamMetadataOptions options,
            CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.ExcelFilePath))
            {
                throw new ArgumentException("ExcelFilePath is required.");
            }

            var result = new BynderMetadataImportResult();

            _logger.LogInformation("Reading WebDam metadata schema from {Path}", options.ExcelFilePath);

            var schemaRows = ReadWebDamMetadataSchema(options.ExcelFilePath);

            var activeRows = schemaRows
                .Where(x => string.Equals(x.Status, "active", StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.IsNullOrWhiteSpace(x.Field))
                .OrderBy(x => TryParseInt(x.Position) ?? int.MaxValue)
                .ThenBy(x => x.Field, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogInformation("Active schema fields found: {Count}", activeRows.Count);

            var metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            var existingLookup = await metaFactoryApi
                .CreateMetapropertyLookupApi(_bynderOptions.Value.Client)
                .ConfigureAwait(false);

            foreach (var row in activeRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mapped = MapSchemaRowToBynderMetaProperty(row, options.Prefix);

                if (options.SkipExisting && existingLookup.ContainsKey(mapped.Name))
                {
                    _logger.LogInformation("Skipping existing metaproperty: {Name}", mapped.Name);
                    result.SkippedMetaproperties.Add(mapped.Name);
                    continue;
                }

                _logger.LogInformation(
                    "Creating metaproperty from WebDam field. Field={Field}, Name={Name}, Label={Label}, Type={Type}, Prefix={Prefix}, Position={Position}",
                    row.Field,
                    mapped.Name,
                    mapped.Label,
                    mapped.Type,
                    options.Prefix ?? "(none)",
                    mapped.ZIndex);

                var created = await metaFactoryApi
                    .CreateMetapropertyApi(_bynderOptions.Value.Client, mapped)
                    .ConfigureAwait(false);

                if (created == null)
                {
                    _logger.LogWarning("Failed creating metaproperty: {Name}", mapped.Name);
                    continue;
                }

                var effectiveName = !string.IsNullOrWhiteSpace(created.Name) ? created.Name : mapped.Name;
                var effectiveLabel = !string.IsNullOrWhiteSpace(created.Label) ? created.Label : mapped.Label;
                var effectiveType = !string.IsNullOrWhiteSpace(created.Type) ? created.Type : mapped.Type;

                result.CreatedMetaproperties.Add(effectiveName);

                _logger.LogInformation(
                    "Created metaproperty: {Name} ({Label}) type={Type}",
                    effectiveName,
                    effectiveLabel,
                    effectiveType);

                if (SupportsOptions(mapped))
                {
                    var possibleValues = ParsePossibleValues(row.PossibleValues);

                    _logger.LogInformation(
                        "Field {Name} has {OptionCount} possible values.",
                        mapped.Name,
                        possibleValues.Count);

                    if (possibleValues.Count > 0)
                    {
                        var metapropertyId = created.Id;

                        // If create response did not deserialize the id, look it up again by name.
                        if (string.IsNullOrWhiteSpace(metapropertyId))
                        {
                            var refreshedLookup = await metaFactoryApi
                                .CreateMetapropertyLookupApi(_bynderOptions.Value.Client)
                                .ConfigureAwait(false);

                            if (refreshedLookup.TryGetValue(mapped.Name, out var refreshedProperty))
                            {
                                metapropertyId = refreshedProperty.Id;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(metapropertyId))
                        {
                            _logger.LogWarning(
                                "Could not resolve Bynder metaproperty id for {Name}, so options could not be created.",
                                mapped.Name);
                        }
                        else
                        {
                            foreach (var option in possibleValues)
                            {
                                var createdOption = await metaFactoryApi
                                    .CreateMetapropertyOptionApi(_bynderOptions.Value.Client, metapropertyId, option)
                                    .ConfigureAwait(false);

                                if (createdOption != null)
                                {
                                    result.CreatedOptions.Add($"{mapped.Name}:{option}");

                                    _logger.LogInformation(
                                        "Created option '{Option}' for {Name}",
                                        option,
                                        mapped.Name);
                                }
                                else
                                {
                                    _logger.LogWarning(
                                        "Failed creating option '{Option}' for {Name}",
                                        option,
                                        mapped.Name);
                                }
                            }
                        }
                    }
                }
            }

            // ✅ Create webdam_id field
            if (options.CreateWebDamIdField)
            {
                var webDamIdName = BuildBynderSafeName("webdam_id", options.Prefix);

                if (!options.SkipExisting || !existingLookup.ContainsKey(webDamIdName))
                {
                    var webdamField = new BynderMetaProperty
                    {
                        Name = webDamIdName,
                        Label = webDamIdName,
                        Type = "text",
                        IsSearchable = true,
                        IsMultiSelect = false,
                        ZIndex = 999999
                    };

                    var created = await metaFactoryApi
                        .CreateMetapropertyApi(_bynderOptions.Value.Client, webdamField)
                        .ConfigureAwait(false);

                    if (created != null)
                    {
                        result.CreatedMetaproperties.Add(created.Name);

                        _logger.LogInformation("Created WebDam id field: {Name}", created.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Failed creating WebDam id field: {Name}", webDamIdName);
                    }
                }
                else
                {
                    result.SkippedMetaproperties.Add(webDamIdName);
                }
            }

            _logger.LogInformation(
                "Import complete. Created={Created}, Skipped={Skipped}, Options={Options}",
                result.CreatedMetaproperties.Count,
                result.SkippedMetaproperties.Count,
                result.CreatedOptions.Count);

            return result;
        }

        private async Task EnsureWebDamIdMetapropertyAsync(global::Bynder.Sdk.Settings.Configuration configuration, string? prefix)
        {
            var metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            var existingLookup = await metaFactoryApi
                .CreateMetapropertyLookupApi(configuration)
                .ConfigureAwait(false);

            var name = BuildBynderSafeName("webdam_id", prefix);

            if (existingLookup.ContainsKey(name))
            {
                _logger.LogInformation("webdam_id metaproperty already exists as {Name}", name);
                return;
            }

            var metaproperty = new BynderMetaProperty
            {
                Name = name,
                Label = name,
                Type = "text",
                IsSearchable = true,
                ZIndex = 999999,
                IsMultiSelect = false
            };

            var created = await metaFactoryApi
                .CreateMetapropertyApi(configuration, metaproperty)
                .ConfigureAwait(false);

            if (created != null)
            {
                _logger.LogInformation("Created WebDam id metaproperty: {Name}", created.Name);
            }
            else
            {
                _logger.LogWarning("Failed creating WebDam id metaproperty: {Name}", name);
            }
        }

        private static BynderMetaProperty MapSchemaRowToBynderMetaProperty(
            WebDamMetadataSchemaRow row,
            string? prefix)
        {
            var type = MapWebDamTypeToBynderType(row.Type);

            return new BynderMetaProperty
            {
                Name = BuildBynderSafeName(row.Field, prefix),
                Label = !string.IsNullOrWhiteSpace(row.Label)
                    ? row.Label!
                    : !string.IsNullOrWhiteSpace(row.Name)
                        ? row.Name!
                        : row.Field,
                Type = type,
                ZIndex = TryParseInt(row.Position) ?? 0,
                IsSearchable = ParseBoolLike(row.Searchable),
                IsMultiSelect = string.Equals(type, "select", StringComparison.OrdinalIgnoreCase)
                                && LooksMultiSelect(row.Type)
            };
        }

        private static bool SupportsOptions(BynderMetaProperty property)
        {
            if (string.IsNullOrWhiteSpace(property.Type))
            {
                return false;
            }

            var normalized = property.Type.Trim().ToLowerInvariant();

            return normalized == "select"
                || normalized == "single_select"
                || normalized == "multi_select";
        }

        private static bool LooksMultiSelect(string? webDamType)
        {
            if (string.IsNullOrWhiteSpace(webDamType))
            {
                return false;
            }

            var normalized = webDamType.Trim().ToLowerInvariant();

            return normalized is "multiselect" or "multichoice" or "checkbox" or "list";
        }

        private static string MapWebDamTypeToBynderType(string? webDamType)
        {
            if (string.IsNullOrWhiteSpace(webDamType))
            {
                return "text";
            }

            switch (webDamType.Trim().ToLowerInvariant())
            {
                case "textfield":
                case "text":
                case "textarea":
                case "string":
                    return "text";

                case "date":
                case "datetime":
                    return "date";

                case "select":
                case "dropdown":
                case "choice":
                case "radio":
                case "multiselect":
                case "multichoice":
                case "checkbox":
                case "list":
                    return "select";

                case "number":
                case "integer":
                case "decimal":
                    return "text";

                default:
                    return "text";
            }
        }

        private static string BuildBynderSafeName(string sourceField, string? prefix)
        {
            var value = sourceField.Trim()
                .Replace(":", "_")
                .Replace("-", "_")
                .Replace(" ", "_");

            value = Regex.Replace(value, @"[^A-Za-z0-9_]+", "_");
            value = Regex.Replace(value, @"_+", "_").Trim('_');
            value = value.ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(prefix))
            {
                value = prefix!.Trim() + value;
            }

            return value;
        }

        private static int? TryParseInt(string? value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static bool ParseBoolLike(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> ParsePossibleValues(string? possibleValues)
        {
            if (string.IsNullOrWhiteSpace(possibleValues))
            {
                return new List<string>();
            }

            return possibleValues
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool SupportsOptions(string bynderType)
        {
            return string.Equals(bynderType, "select", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildLabel(WebDamMetadataSchemaRow row)
        {
            if (!string.IsNullOrWhiteSpace(row.Label))
            {
                return row.Label!;
            }

            if (!string.IsNullOrWhiteSpace(row.Name))
            {
                return row.Name!;
            }

            return row.Field;
        }

        private static Dictionary<string, int> BuildHeaderMap(ExcelWorksheet worksheet)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var value = worksheet.Cells[1, col].Text?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    map[value] = col;
                }
            }

            return map;
        }

        private static string? GetCellString(
            ExcelWorksheet worksheet,
            int row,
            IReadOnlyDictionary<string, int> headers,
            string headerName)
        {
            if (!headers.TryGetValue(headerName, out var col))
            {
                return null;
            }

            return worksheet.Cells[row, col].Text?.Trim();
        }

        public async Task ImportWebDamMetadataSchema(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Enter the full path to the WebDam export Excel file:");
            var excelPath = (await _reader.ReadInputAsync())?.Trim() ?? string.Empty;

            _logger.LogInformation("Enter optional prefix for the Bynder fields (example: wd_), or press Enter for none:");
            var prefix = (await _reader.ReadInputAsync())?.Trim();

            var result = await CreateMetapropertiesFromWebDamExcelAsync(
                new ImportWebDamMetadataOptions
                {
                    ExcelFilePath = excelPath,
                    Prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix,
                    SkipExisting = true,
                    CreateWebDamIdField = true
                },
                cancellationToken);

            _logger.LogInformation(
                "Created {CreatedCount} metaproperties, skipped {SkippedCount}, created {OptionCount} options.",
                result.CreatedMetaproperties.Count,
                result.SkippedMetaproperties.Count,
                result.CreatedOptions.Count);
        }

        public async Task ImportWebDamMetadataSchemaFromExcel(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Enter the full path to the WebDam export Excel file:");
            var excelPath = (await _reader.ReadInputAsync())?.Trim();

            if (string.IsNullOrWhiteSpace(excelPath))
            {
                _logger.LogWarning("No Excel file path was provided. Operation canceled.");
                return;
            }

            _logger.LogInformation("Enter an optional prefix for the Bynder fields (example: wd_), or press Enter for none:");
            var prefix = (await _reader.ReadInputAsync())?.Trim();

            _logger.LogInformation("Create a searchable webdam_id field too? (y/n)");
            var createWebDamIdInput = (await _reader.ReadInputAsync())?.Trim().ToLowerInvariant();
            var createWebDamId = createWebDamIdInput == "y";

            _logger.LogInformation("Skip fields that already exist in Bynder? (y/n)");
            var skipExistingInput = (await _reader.ReadInputAsync())?.Trim().ToLowerInvariant();
            var skipExisting = skipExistingInput != "n";

            _logger.LogInformation("Proceed with creating active WebDam metadata properties in Bynder? (y/n)");
            var confirmation = (await _reader.ReadInputAsync())?.Trim().ToLowerInvariant();

            if (confirmation != "y")
            {
                _logger.LogInformation("Operation canceled.");
                return;
            }

            try
            {


                await CreateMetapropertiesFromWebDamExcelAsync(
                new ImportWebDamMetadataOptions
                {
                    ExcelFilePath = excelPath,
                    Prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix,
                    SkipExisting = true,
                    CreateWebDamIdField = true
                },
                    cancellationToken);

                _logger.LogInformation("WebDam metadata import completed.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("WebDam metadata import was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing WebDam metadata schema from Excel.");
            }
        }

        private static string NormalizeFieldName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            value = value.Trim();

            // Match same transformations used when creating fields
            value = value.Replace(":", "_")
                         .Replace("-", "_")
                         .Replace(" ", "_");

            value = Regex.Replace(value, @"[^A-Za-z0-9_]+", "_");
            value = Regex.Replace(value, @"_+", "_").Trim('_');

            return value.ToLowerInvariant();
        }
        private static IReadOnlyDictionary<string, BynderMetaProperty> BuildBynderFieldMap(
            IReadOnlyDictionary<string, BynderMetaProperty> bynderLookup)
        {
            var map = new Dictionary<string, BynderMetaProperty>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in bynderLookup)
            {
                var metaProperty = kvp.Value;
                if (metaProperty == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(metaProperty.Name))
                {
                    var normalizedName = NormalizeFieldName(metaProperty.Name);
                    if (!map.ContainsKey(normalizedName))
                    {
                        map[normalizedName] = metaProperty;
                    }
                }

                if (!string.IsNullOrWhiteSpace(metaProperty.Label))
                {
                    var normalizedLabel = NormalizeLabel(metaProperty.Label);
                    if (!map.ContainsKey(normalizedLabel))
                    {
                        map[normalizedLabel] = metaProperty;
                    }
                }
            }

            return map;
        }

        public async Task CreateBynderImportExcelFromWebDamExportAsync(
            CreateBynderImportFileOptions options,
            CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.SourceExcelFilePath))
            {
                throw new ArgumentException("SourceExcelFilePath is required.", nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.OutputExcelFilePath))
            {
                throw new ArgumentException("OutputExcelFilePath is required.", nameof(options));
            }

            _logger.LogInformation("Reading WebDam export from {Path}", options.SourceExcelFilePath);

            var assetRows = ReadAssetsWorksheet(options.SourceExcelFilePath);
            var metadataRows = ReadMetadataWorksheet(options.SourceExcelFilePath);

            _logger.LogInformation(
                "Read {AssetCount} asset rows and {MetadataCount} metadata rows from WebDam export.",
                assetRows.Count,
                metadataRows.Count);
            MetapropertyOptionBuilderFactoryApi metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            var bynderLookup = await metaFactoryApi
                .CreateMetapropertyLookupApi(_bynderOptions.Value.Client)
                .ConfigureAwait(false);

            _logger.LogInformation("Loaded {Count} Bynder metaproperties.", bynderLookup.Count);

            var bynderFieldMap = BuildBynderFieldMap(bynderLookup);

            var mergedRows = MergeAssetsAndMetadata(assetRows, metadataRows, bynderFieldMap, options.Prefix);

            WriteBynderImportWorksheet(options.OutputExcelFilePath, mergedRows, bynderLookup);

            _logger.LogInformation(
                "Created Bynder import Excel file at {Path} with {Count} rows.",
                options.OutputExcelFilePath,
                mergedRows.Count);
        }

        public IReadOnlyList<WebDamAssetSheetRow> ReadAssetsWorksheet(string excelFilePath)
        {
            var fileInfo = new FileInfo(excelFilePath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("Excel file not found.", excelFilePath);
            }


            using var package = new ExcelPackage(fileInfo);
            var worksheet = package.Workbook.Worksheets["Assets"];

            if (worksheet == null)
            {
                throw new InvalidOperationException("Worksheet 'Assets' was not found.");
            }

            var headers = BuildHeaderMap(worksheet);
            var rows = new List<WebDamAssetSheetRow>();

            for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var assetId = GetCellString(worksheet, row, headers, "Asset Id");
                if (string.IsNullOrWhiteSpace(assetId))
                {
                    continue;
                }

                rows.Add(new WebDamAssetSheetRow
                {
                    AssetId = assetId,
                    FileName = GetCellString(worksheet, row, headers, "File Name") ?? string.Empty,
                    AssetName = GetCellString(worksheet, row, headers, "Asset Name"),
                    SizeBytes = TryParseLong(GetCellString(worksheet, row, headers, "Size Bytes")),
                    FileType = GetCellString(worksheet, row, headers, "File Type"),
                    FolderId = GetCellString(worksheet, row, headers, "Folder Id"),
                    FolderPath = GetCellString(worksheet, row, headers, "Folder Path")
                });
            }

            return rows;
        }

        private static ParsedMetadataHeader ParseMetadataHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return new ParsedMetadataHeader();
            }

            var trimmed = header.Trim();
            var openParen = trimmed.LastIndexOf('(');
            var closeParen = trimmed.LastIndexOf(')');

            if (openParen >= 0 && closeParen > openParen)
            {
                return new ParsedMetadataHeader
                {
                    OriginalHeader = trimmed,
                    LabelPart = trimmed.Substring(0, openParen).Trim(),
                    FieldPart = trimmed.Substring(openParen + 1, closeParen - openParen - 1).Trim()
                };
            }

            return new ParsedMetadataHeader
            {
                OriginalHeader = trimmed,
                LabelPart = trimmed,
                FieldPart = trimmed
            };
        }

        private static string NormalizeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            value = value.Trim().ToLowerInvariant();
            value = Regex.Replace(value, @"\s+", " ");
            return value;
        }

        public IReadOnlyDictionary<string, Dictionary<string, string>> ReadMetadataWorksheet(string excelFilePath)
        {
            var fileInfo = new FileInfo(excelFilePath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("Excel file not found.", excelFilePath);
            }

            using var package = new ExcelPackage(fileInfo);
            var worksheet = package.Workbook.Worksheets["Metadata"];

            if (worksheet == null)
            {
                throw new InvalidOperationException("Worksheet 'Metadata' was not found.");
            }

            var headers = BuildHeaderMap(worksheet);
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            var parsedHeaders = headers.Keys
                .Where(x => !string.Equals(x, "Asset Id", StringComparison.OrdinalIgnoreCase))
                .Select(ParseMetadataHeader)
                .ToList();

            for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var assetId = GetCellString(worksheet, row, headers, "Asset Id");
                if (string.IsNullOrWhiteSpace(assetId))
                {
                    continue;
                }

                var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var parsedHeader in parsedHeaders)
                {
                    var value = GetCellString(worksheet, row, headers, parsedHeader.OriginalHeader);
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    // Store both forms so later mapping can use either label or field.
                    if (!string.IsNullOrWhiteSpace(parsedHeader.FieldPart))
                    {
                        metadata[parsedHeader.FieldPart] = value;
                    }

                    if (!string.IsNullOrWhiteSpace(parsedHeader.LabelPart) &&
                        !metadata.ContainsKey(parsedHeader.LabelPart))
                    {
                        metadata[parsedHeader.LabelPart] = value;
                    }
                }

                result[assetId] = metadata;
            }

            return result;
        }

        private BynderMetaProperty? ResolveBynderMetapropertyForWebDamColumn(
    string webDamColumnToken,
    string? prefix,
    IReadOnlyDictionary<string, BynderMetaProperty> bynderFieldMap)
        {
            if (string.IsNullOrWhiteSpace(webDamColumnToken))
            {
                return null;
            }

            // 1. Preferred: prefixed database name, because that's how fields were created in Bynder.
            var prefixedDbName = BuildBynderSafeName(webDamColumnToken, prefix);
            var normalizedPrefixedDbName = NormalizeFieldName(prefixedDbName);

            if (bynderFieldMap.TryGetValue(normalizedPrefixedDbName, out var match))
            {
                return match;
            }

            // 2. Fallback: unprefixed database name.
            var rawDbName = BuildBynderSafeName(webDamColumnToken, null);
            var normalizedRawDbName = NormalizeFieldName(rawDbName);

            if (bynderFieldMap.TryGetValue(normalizedRawDbName, out match))
            {
                return match;
            }

            // 3. Fallback: Bynder label match.
            var normalizedLabel = NormalizeLabel(webDamColumnToken);

            if (bynderFieldMap.TryGetValue(normalizedLabel, out match))
            {
                return match;
            }

            return null;
        }

        private string ConvertToBynderOptionDatabaseNameIfPossible(
    BynderMetaProperty metaProperty,
    string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            if (metaProperty.Options == null || !metaProperty.Options.Any())
            {
                return rawValue;
            }

            var splitValues = rawValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!splitValues.Any())
            {
                return rawValue;
            }

            var convertedValues = new List<string>();

            foreach (var value in splitValues)
            {
                var matchedOption = metaProperty.Options.FirstOrDefault(option =>
                    string.Equals(option.Label, value, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(option.Name, value, StringComparison.OrdinalIgnoreCase));

                if (matchedOption != null && !string.IsNullOrWhiteSpace(matchedOption.Name))
                {
                    convertedValues.Add(matchedOption.Name);
                }
                else
                {
                    convertedValues.Add(value);
                }
            }

            return string.Join(",", convertedValues);
        }
        private List<BynderImportRow> MergeAssetsAndMetadata(
            IReadOnlyList<WebDamAssetSheetRow> assets,
            IReadOnlyDictionary<string, Dictionary<string, string>> metadataByAssetId,
            IReadOnlyDictionary<string, BynderMetaProperty> bynderLookup,
            string? prefix)
        {
            var rows = new List<BynderImportRow>();
            var bynderFieldMap = BuildBynderFieldMap(bynderLookup);

            foreach (var asset in assets)
            {
                var row = new BynderImportRow
                {
                    AssetId = asset.AssetId,
                    FileName = asset.FileName,
                    AssetName = asset.AssetName,
                    SizeBytes = asset.SizeBytes,
                    FileType = asset.FileType,
                    FolderId = asset.FolderId,
                    FolderPath = asset.FolderPath,
                    Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                };

                var webDamIdFieldName = BuildBynderSafeName("webdam_id", prefix);
                var normalizedWebDamId = NormalizeFieldName(webDamIdFieldName);

                if (bynderFieldMap.TryGetValue(normalizedWebDamId, out var webDamIdMetaProperty))
                {
                    row.Metadata[webDamIdMetaProperty.Name] = asset.AssetId;
                }
                else
                {
                    _logger.LogWarning("webdam_id field ({Field}) was not found in Bynder.", webDamIdFieldName);
                }

                if (metadataByAssetId.TryGetValue(asset.AssetId, out var metadata))
                {
                    foreach (var kvp in metadata)
                    {
                        var matchedMetaProperty = ResolveBynderMetapropertyForWebDamColumn(
                            kvp.Key,
                            prefix,
                            bynderFieldMap);

                        if (matchedMetaProperty != null)
                        {
                            var convertedValue = ConvertToBynderOptionDatabaseNameIfPossible(
                                matchedMetaProperty,
                                kvp.Value);

                            row.Metadata[matchedMetaProperty.Name] = convertedValue;
                        }
                        else
                        {
                            _logger.LogDebug(
                                "Skipping metadata column token {Token} because no Bynder metaproperty match was found.",
                                kvp.Key);
                        }
                    }
                }

                rows.Add(row);
            }

            return rows;
        }

        private void WriteBynderImportWorksheet(
            string outputExcelFilePath,
            IReadOnlyList<BynderImportRow> rows,
            IReadOnlyDictionary<string, BynderMetaProperty> bynderLookup)
        {
            var fileInfo = new FileInfo(outputExcelFilePath);
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            using var package = new ExcelPackage();

            var worksheet = package.Workbook.Worksheets.Add("Bynder Import");

            var fixedHeaders = new List<string>
    {
        "Id",
        "Asset Id",
        "File Name",
        "Asset Name",
        "Size Bytes",
        "File Type",
        "Folder Id",
        "Folder Path"
    };

            var metadataHeaders = rows
                .SelectMany(x => x.Metadata.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var allHeaders = fixedHeaders.Concat(metadataHeaders).ToList();

            for (var col = 0; col < allHeaders.Count; col++)
            {
                worksheet.Cells[1, col + 1].Value = allHeaders[col];
            }

            using (var headerRange = worksheet.Cells[1, 1, 1, allHeaders.Count])
            {
                headerRange.Style.Font.Bold = true;
            }

            var rowIndex = 2;
            foreach (var row in rows)
            {
                var col = 1;

                // Placeholder Id column for later Bynder use
                worksheet.Cells[rowIndex, col++].Value = string.Empty;

                worksheet.Cells[rowIndex, col++].Value = row.AssetId;
                worksheet.Cells[rowIndex, col++].Value = row.FileName;
                worksheet.Cells[rowIndex, col++].Value = row.AssetName ?? string.Empty;
                worksheet.Cells[rowIndex, col++].Value = row.SizeBytes;
                worksheet.Cells[rowIndex, col++].Value = row.FileType ?? string.Empty;
                worksheet.Cells[rowIndex, col++].Value = row.FolderId ?? string.Empty;
                worksheet.Cells[rowIndex, col++].Value = row.FolderPath ?? string.Empty;

                foreach (var metadataHeader in metadataHeaders)
                {
                    row.Metadata.TryGetValue(metadataHeader, out var value);
                    worksheet.Cells[rowIndex, col++].Value = value ?? string.Empty;
                }

                rowIndex++;
            }

            if (worksheet.Dimension != null)
            {
                var range = worksheet.Cells[1, 1, Math.Max(2, rowIndex - 1), allHeaders.Count];
                var table = worksheet.Tables.Add(range, "BynderImportTable");
                table.TableStyle = TableStyles.Medium2;

                worksheet.View.FreezePanes(2, 1);
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            }

            package.SaveAs(fileInfo);
        }

        private static string ExtractSourceFieldNameFromMetadataHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return string.Empty;
            }

            // Header might be:
            // "Caption/Description (wd_caption)"
            // or just "wd_caption"
            var openParen = header.LastIndexOf('(');
            var closeParen = header.LastIndexOf(')');

            if (openParen >= 0 && closeParen > openParen)
            {
                return header.Substring(openParen + 1, closeParen - openParen - 1).Trim();
            }

            return header.Trim();
        }

        private static long? TryParseLong(string? value)
        {
            if (long.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        public async Task CreateBynderImportExcelFromWebDamExport(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Enter the full path to the WebDam export Excel file:");
            var sourcePath = (await _reader.ReadInputAsync())?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                _logger.LogWarning("No source file path was provided.");
                return;
            }

            _logger.LogInformation("Enter the full output path for the Bynder import Excel file:");
            var outputPath = (await _reader.ReadInputAsync())?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                _logger.LogWarning("No output file path was provided.");
                return;
            }

            _logger.LogInformation("Enter optional prefix for the Bynder fields (example: wd_), or press Enter for none:");
            var prefix = (await _reader.ReadInputAsync())?.Trim();

            await CreateBynderImportExcelFromWebDamExportAsync(
                new CreateBynderImportFileOptions
                {
                    SourceExcelFilePath = sourcePath,
                    OutputExcelFilePath = outputPath,
                    Prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix
                },
                cancellationToken);

            _logger.LogInformation("Bynder import Excel file created successfully.");
        }

        public async Task CreateMetadataPropertiesFile()
        {
            MetapropertyOptionBuilderFactoryApi metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            var metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);

            var dataTables = GetMetaPropertiesDataTables(metaProperties);

            // write out the file
            var stream = ExcelWriter.WriteDataTables(dataTables);
            var fileName = $"{_sourceDirectory}{_metadataPropertiesFilename}";

            using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            stream.WriteTo(fs);
        }

        private static List<DataTable> GetMetaPropertiesDataTables(Dictionary<string, BynderMetaProperty> metaProperties)
        {
            var metaPropertiesDataTable = CreateMetaPropertyDataTable();
            var optionDataTables = new List<DataTable>();

            foreach (var key in metaProperties.Keys)
            {
                var metaProperty = metaProperties[key];
                var rowArray = new List<object>
            {
                metaProperty.Id,
                metaProperty.Name,
                metaProperty.Label,
                metaProperty.IsMultiSelect,
                metaProperty.IsRequired,
                metaProperty.IsFilterable,
                metaProperty.IsMainfilter,
                metaProperty.IsEditable,
                metaProperty.ZIndex, /// add more props below
                metaProperty.IsDisplayField,
                metaProperty.IsMultifilter,
                metaProperty.ShowInGridView,
                metaProperty.ShowInListView,
                metaProperty.IsApiField,
                metaProperty.ShowInDuplicateView,
                metaProperty.Type,
                metaProperty.IsSearchable,
                metaProperty.IsDrilldown,
                metaProperty.UseDependencies


            };

                if (metaProperty.Options.Count > 0)
                {
                    var metaPropertyOptionsDataTable = CreateMetaPropertyOptionsDataTable(metaProperty.Name);

                    foreach (var metaPropertyOption in metaProperty.Options)
                    {
                        var rowOptionArray = new List<object>
                    {
                        metaPropertyOption.Id,
                        metaPropertyOption.Name,
                        metaPropertyOption.Label,
                        metaPropertyOption.ZIndex,
                        metaPropertyOption.IsSelectable,
                        string.Join("|", metaPropertyOption.LinkedOptionIds)
                    };

                        metaPropertyOptionsDataTable.Rows.Add(rowOptionArray.ToArray());
                    }

                    optionDataTables.Add(metaPropertyOptionsDataTable);
                }

                metaPropertiesDataTable.Rows.Add(rowArray.ToArray());
            }

            var dataTables = new List<DataTable> { metaPropertiesDataTable };
            dataTables.AddRange(optionDataTables);

            return dataTables;
        }

        private static DataTable CreateMetaPropertyDataTable()
        {
            var dataTable = new DataTable("MetaProperties");

            dataTable.Columns.Add("Id");
            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("Label");
            dataTable.Columns.Add("IsMultiSelect");
            dataTable.Columns.Add("IsRequired");
            dataTable.Columns.Add("IsFilterable");
            dataTable.Columns.Add("IsMainfilter");
            dataTable.Columns.Add("IsEditable");
            dataTable.Columns.Add("ZIndex");
            // add more props below
            dataTable.Columns.Add("IsDisplayField");
            dataTable.Columns.Add("IsMultiFilter");
            dataTable.Columns.Add("ShowInGridView");
            dataTable.Columns.Add("ShowInListView");
            dataTable.Columns.Add("IsApiField");
            dataTable.Columns.Add("ShowInDuplicateView");
            dataTable.Columns.Add("Type");
            dataTable.Columns.Add("IsSearchable");
            dataTable.Columns.Add("IsDrilldown");
            dataTable.Columns.Add("UseDependencies");

            return dataTable;
        }

        private static DataTable CreateMetaPropertyOptionsDataTable(string nameOfMetaProperty)
        {
            var dataTable = new DataTable(nameOfMetaProperty);

            dataTable.Columns.Add("Id");
            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("Label");
            dataTable.Columns.Add("ZIndex");
            dataTable.Columns.Add("IsSelectable");
            dataTable.Columns.Add("LinkedOptionIds");

            return dataTable;
        }


        // Templating

        public async Task CreateBlankMetadataTemplate()
        {
            MetapropertyOptionBuilderFactoryApi metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            var metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);

            var dataTable = CreateMetaPropertyDataTable(metaProperties);

            // write out the file
            var stream = ExcelWriter.WriteDataTable(dataTable);
            var fileName = $"{_sourceDirectory}{_blankMetadataTemplate}";

            using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            stream.WriteTo(fs);
        }

        private static DataTable CreateMetaPropertyDataTable(Dictionary<string, BynderMetaProperty> metaProperties)
        {
            var dataTable = new DataTable("Metadata");

            dataTable.Columns.Add("Id"); // if present this is an upsert, otherwise its always a new asset
            dataTable.Columns.Add("Collection");  // collection to put asset into
            dataTable.Columns.Add("Filename");
            dataTable.Columns.Add("Tags");

            foreach (var key in metaProperties.Keys)
            {
                var metaProperty = metaProperties[key];
                dataTable.Columns.Add(metaProperty.Name);
            }

            return dataTable;
        }

        public async Task CreateMetadataTemplateFromClientFile()
        {

            var clientMetadataFilename = await PromptForFilenameAsync("Please enter the Client Metadata filename from Azure", _clientMetadataTemplateFilename);
            var outputTemplateFilename = await PromptForFilenameAsync("Please enter the Output Template filename", _metadataTemplateFilename);

            MetapropertyOptionBuilderFactoryApi metaFactoryApi = new MetapropertyOptionBuilderFactoryApi(_bynderClient, _memoryCache);
            var metaProperties = await metaFactoryApi.CreateMetapropertyLookupApi(_bynderOptions.Value.Client);

            // read file to stream
            _logger.LogInformation($"Reading {clientMetadataFilename} from Azure.");

            var metadataTable = ExcelReader.LoadExcelWorksheetsToDataTables(new FileInfo($"{_sourceDirectory}{clientMetadataFilename}")).FirstOrDefault() ?? new DataTable();

            var dataTable = metadataTable.Clone();

            DataRow dr = metadataTable.Rows[0];

            foreach (DataRow dataRow in metadataTable.Rows)
            {
                DataRow newDataRow = dataTable.NewRow();
                foreach (var column in dataRow.Table.Columns)
                {
                    var columnName = $"{column}";
                    var value = dataRow[columnName].ToString() ?? string.Empty;
                    if (metaProperties.Keys.Contains(columnName))
                    {
                        // get possible values
                        var options = metaProperties[columnName].Options;
                        List<string> cleanValues = value
                            .Split(',')
                            .Select(s => s.Trim())
                            .ToList();

                        List<string> optionLabels = options.Select(o => o.Label).ToList();

                        // turns out its possible for a comma delimited entry to actually be the value as well.
                        bool containsAll = cleanValues.All(item => optionLabels.Contains(item));
                        string option = options.Where(o => o.Label.Equals(value.Trim())).Select(x => x.Name).FirstOrDefault();

                        if (!string.IsNullOrEmpty(option))
                        {
                            newDataRow[columnName] = option;

                        }
                        else if (containsAll)
                        {
                            List<string> optionValues = options.Where(o => cleanValues.Contains(o.Label)).Select(v => v.Name).ToList();
                            string optionValue = string.Join(",", optionValues);
                            newDataRow[columnName] = optionValue;
                        }
                        else
                        {
                            newDataRow[columnName] = value;
                        }

                    }
                    else
                    {
                        newDataRow[columnName] = value;
                    }
                }

                dataTable.Rows.Add(newDataRow);
            }


            // write out the file
            var stream = ExcelWriter.WriteDataTable(dataTable);
            var fileName = $"{_sourceDirectory}{outputTemplateFilename}";

            using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            stream.WriteTo(fs);
        }

        private async Task<string> PromptForFilenameAsync(string promptMessage, string defaultFilename)
        {
            _logger.LogInformation($"{promptMessage} (Default is {defaultFilename})");
            var input = await _reader.ReadInputAsync();
            return string.IsNullOrWhiteSpace(input) ? defaultFilename : input;
        }

        private string GetAssetSubType(string blobName, string extension)
        {
            string assetSubType = string.Empty;

            string ext = extension.ToLower(); ;
            string[] font = new string[] { "ttf", "otf", "woff", "woff2", "eot", "svg" };

            if (font.Contains(ext))
            {
                assetSubType = "Font";
            }
            else if (ext == "ico")
            {
                assetSubType = "Icon";
            }
            else
            {

                string[] folderParts = blobName.Split("/");
                // best guess here
                var metaPropertyOptions = _metaProperties["Asset_Sub-Type"].Options;
                Array.Reverse(folderParts);
                foreach (var part in folderParts)  // this is so the lowest level part takes precedence so if the folder is like lifestyle-something/product-shot/something.txt then it will be a product-shot and not a lifestyle.
                {
                    foreach (var metaProperty in metaPropertyOptions)
                    {
                        string label = metaProperty.Label; // may contain space or -
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
                            string label = metaProperty.Label; // may contain space or -
                            string cleanLabel = label.Replace("-", "");
                            string[] labelParts = cleanLabel.Split(" ");

                            // try to match like this first
                            var valueNames = labelParts
                                .Where(o =>
                                    (Regex.IsMatch(
                                        blobName,
                                        $@"(^|[_\-\s/]){Regex.Escape(o)}([_\-\s/]|$)",
                                        RegexOptions.IgnoreCase)) || (o.Contains("shot", StringComparison.OrdinalIgnoreCase) && part.Contains("shoot", StringComparison.OrdinalIgnoreCase)))
                                .Select(o => o)
                                .ToList();

                            //bool containsAny = labelParts.Any(t => part.Contains(t, StringComparison.OrdinalIgnoreCase) || (t.Contains("shot", StringComparison.OrdinalIgnoreCase) && part.Contains("shoot", StringComparison.OrdinalIgnoreCase)));

                            if (valueNames.Count() > 0)
                            {
                                assetSubType = metaProperty.Name;
                                break;
                            }
                        }
                    }
                }


            }

            return assetSubType == string.Empty ? "Marketing_Asset" : assetSubType;  // use default as Marketing_Asset
        }
        private string GetAssetType(string blobName, string extension)
        {
            string assetSubType = string.Empty;

            string ext = extension.ToLower();
            string[] audio = new string[] { "mp3", "wav", "aac", "flac", "aiff", "ogg" };
            string[] graphics = new string[] { "svg", "ai", "ait", "eps", "gif" };
            string[] photography = new string[] { "tiff", "tif", "heic", "heif", "jpeg", "jpg", "webp", "indd" };
            string[] document = new string[] { "doc", "docx", "odt", "pdf", "rtf", "txt", "htm", "html", "xls", "xlsx", "ods", "ppt", "pptx", "zip", "csv", "json", "xml", "srt" };
            string[] video = new string[] { "mp4", "mov", "avi", "wmv", "mkv", "webm", "flv" };

            if (blobName.ToLower().Contains("logo"))
            {
                assetSubType = "Logos";
            }
            else if (audio.Contains(ext))
            {
                assetSubType = "Audio";
            }
            else if (graphics.Contains(ext))
            {
                assetSubType = "Graphics";
            }
            else if (photography.Contains(ext))
            {
                assetSubType = "Photography";
            }
            else if (document.Contains(ext))
            {
                assetSubType = "Documents";
            }
            else if (video.Contains(ext))
            {
                assetSubType = "Videos";
            }
            else
            {
                assetSubType = "Photography";
            }
            return assetSubType;
        }

        private string GetMatchingValue(string blobName, string metaPropertyName)
        {
            string value = string.Empty;
            var metaPropertyOptions = _metaProperties[metaPropertyName].Options;

            // try to match like this first
            var valueNames = metaPropertyOptions
                .Where(o =>
                    Regex.IsMatch(
                        blobName,
                        $@"(^|[_\-\s/]){Regex.Escape(o.Label)}([_\-\s/]|$)",
                        RegexOptions.IgnoreCase))
                .Select(o => o.Name)
                .ToList();

            value = GetFirstNotOnOrOnlyEntry(valueNames);

            return value;
        }

        private string GetFirstNotOnOrOnlyEntry(List<string> items)
        {
            if (items == null || items.Count == 0)
                return string.Empty;

            if (items.Count == 1)
                return items[0];

            // More than one entry: return first not "on"
            return items.FirstOrDefault(s => !string.Equals(s, "on", StringComparison.OrdinalIgnoreCase));
        }

        private static void SplitExcelFile(string sourcePath, string outputDirectory, string prefix, int rowsPerFile = 500)
        {

            using (var package = new ExcelPackage(new FileInfo(sourcePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Assuming first worksheet
                int totalRows = worksheet.Dimension.End.Row;
                int totalColumns = worksheet.Dimension.End.Column;

                // Read header row
                var header = new object[totalColumns];
                for (int col = 1; col <= totalColumns; col++)
                {
                    header[col - 1] = worksheet.Cells[1, col].Value;
                }

                int fileIndex = 1;
                for (int startRow = 2; startRow <= totalRows; startRow += rowsPerFile)
                {
                    string newFile = Path.Combine(outputDirectory, $"{prefix}_azureassets_retry{fileIndex}_take1.xlsx");
                    using (var newPackage = new ExcelPackage())
                    {
                        var newWorksheet = newPackage.Workbook.Worksheets.Add("Sheet1");

                        // Copy header
                        for (int col = 1; col <= totalColumns; col++)
                        {
                            newWorksheet.Cells[1, col].Value = header[col - 1];
                        }

                        // Copy rows
                        int endRow = Math.Min(startRow + rowsPerFile - 1, totalRows);
                        int newRow = 2;
                        for (int row = startRow; row <= endRow; row++, newRow++)
                        {
                            for (int col = 1; col <= totalColumns; col++)
                            {
                                newWorksheet.Cells[newRow, col].Value = worksheet.Cells[row, col].Value;
                            }
                        }

                        newPackage.SaveAs(new FileInfo(newFile));
                    }
                    fileIndex++;
                }
            }
        }

        public void CreateBatchExcelFiles()
        {
            SplitExcelFile(
                $"{_sourceDirectory}restamp\\updates\\logs\\combined-retry-import.xlsx",
                $"{_sourceDirectory}restamp\\updates\\logs\\retry",
                "update_bynder",
                2000 // Optional, defaults to 500 rows per file
            );
        }

        public void CreateBetterStateCityMetadata()
        {
            StateCityFiller.ProcessExcel(
                $"{_sourceDirectory}states.txt",
                $"{_sourceDirectory}cities.txt",
                $"{_sourceDirectory}\\imports\\logs\\combined-success-import.xlsx",
                $"{_sourceDirectory}\\imports\\logs\\update_bynder_metadata.xlsx"
            );
        }

        public void MergeSuccess()
        {
            string mainSuccess = $"{_sourceDirectory}\\imports\\logs\\combined-success-import.xlsx";
            string retrySuccess = $"{_sourceDirectory}\\imports\\retry\\logs\\combined-success-import.xlsx";
            string outSuccess = $"{_sourceDirectory}\\imports\\retry\\logs\\total-success-import.xlsx";

            MergeExcelsOnOriginId(mainSuccess, retrySuccess, outSuccess);

        }

        public static void MergeExcelsOnOriginId(string fileAPath, string fileBPath, string outputPath)
        {
            using var packageA = new ExcelPackage(new FileInfo(fileAPath));
            using var packageB = new ExcelPackage(new FileInfo(fileBPath));

            var wsA = packageA.Workbook.Worksheets[0];
            var wsB = packageB.Workbook.Worksheets[0];

            // Find the column index for "OriginId"
            var colCount = wsA.Dimension.End.Column;
            int originIdColA = Enumerable.Range(1, colCount)
                .First(i => wsA.Cells[1, i].Text == "OriginId");
            int originIdColB = Enumerable.Range(1, wsB.Dimension.End.Column)
                .First(i => wsB.Cells[1, i].Text == "OriginId");

            // Get all OriginIds from A
            var originIdsA = new HashSet<string>(
                Enumerable.Range(2, wsA.Dimension.End.Row - 1)
                .Select(row => wsA.Cells[row, originIdColA].Text)
            );

            // Where to start appending in A
            int nextRowA = wsA.Dimension.End.Row + 1;

            // Loop over B's rows, add if OriginId is not in A
            for (int rowB = 2; rowB <= wsB.Dimension.End.Row; rowB++)
            {
                var originIdB = wsB.Cells[rowB, originIdColB].Text;
                if (!originIdsA.Contains(originIdB))
                {
                    for (int col = 1; col <= wsB.Dimension.End.Column; col++)
                    {
                        wsA.Cells[nextRowA, col].Value = wsB.Cells[rowB, col].Value;
                    }
                    nextRowA++;
                }
            }

            packageA.SaveAs(new FileInfo(outputPath));
        }


        private static string GetSha256Hash(string blobName)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(blobName);
                var hashBytes = sha256.ComputeHash(bytes);

                // Convert to hex string
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public async Task TestBynderAsset()
        {

            var mediaInformationQuery = new MediaInformationQuery
            {
                MediaId = "530179C7-1AD0-4807-B287BE6DF803B00F"
            };
            var mediaInfo = await _bynderClient.GetAssetService().GetMediaInfoAsync(mediaInformationQuery);
            ;

            string zoriginId = "4a94699024fcab7af89fe2b26d37421629715521297ac389cb46e5865aea7235";
            var foundAssets = await _bynderClient.GetAssetService().GetMediaListAsync(new MediaQuery()
            {
                MetaProperties = new Dictionary<string, IList<string>>
                    {
                        {
                            "OriginId", [zoriginId]
                        }
                    }
            });

            if (foundAssets.Any())
            {
                ;


            }
        }
    }

}
