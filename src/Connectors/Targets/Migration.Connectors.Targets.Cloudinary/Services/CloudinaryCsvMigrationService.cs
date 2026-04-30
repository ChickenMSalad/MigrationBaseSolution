using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Connectors.Targets.Cloudinary.Clients;
using Migration.Connectors.Targets.Cloudinary.Configuration;
using Migration.Connectors.Targets.Cloudinary.Models;

namespace Migration.Connectors.Targets.Cloudinary.Services;

public sealed class CloudinaryCsvMigrationService(
    CloudinaryUploadService uploadService,
    CloudinaryStructuredMetadataService structuredMetadataService,
    CloudinaryMappingProfileLoader mappingLoader,
    ICloudinaryAdminClient adminClient,
    IOptions<CloudinaryOptions> cloudinaryOptions,
    IOptions<CloudinaryCsvMigrationOptions> migrationOptions,
    ILogger<CloudinaryCsvMigrationService> logger)
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public async Task RunMigrationAsync(CancellationToken cancellationToken = default)
    {
        var settings = ResolveRuntimeSettings();
        EnsureSafeToRun(settings);

        var rows = await LoadRowsAsync(settings.ManifestPath, cancellationToken).ConfigureAwait(false);
        var mapping = mappingLoader.Load(settings.MappingPath);
        var output = CreateRunFolder(settings.OutputRoot, settings.RunName);

        logger.LogInformation("Starting Cloudinary CSV migration. CSV: {Csv}. Mapping: {Mapping}. Output: {Output}.", settings.ManifestPath, settings.MappingPath, output);

        var reportRows = new ConcurrentBag<string>();
        var semaphore = new SemaphoreSlim(settings.MaxConcurrency);
        var logFile = Path.Combine(output, "log.jsonl");
        var tasks = rows.Select(async row =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var request = await BuildUploadRequestAsync(row, mapping, cloudinaryOptions.Value, cancellationToken).ConfigureAwait(false);
                var response = await uploadService.UploadAsync(request, cancellationToken).ConfigureAwait(false);

                var record = new CloudinaryMigrationLogRecord
                {
                    RowId = row.TryGetValue("__RowId", out var rowId) ? rowId ?? string.Empty : string.Empty,
                    Status = "MIGRATED",
                    PublicId = response.PublicId,
                    AssetId = response.AssetId,
                    SecureUrl = response.SecureUrl?.ToString(),
                    Message = response.StatusCode.ToString(),
                    Request = request,
                    Response = new
                    {
                        response.PublicId,
                        response.AssetId,
                        SecureUrl = response.SecureUrl?.ToString(),
                        response.Version
                    }
                };

                await AppendJsonLineAsync(logFile, record, cancellationToken).ConfigureAwait(false);
                reportRows.Add(ToReportCsv(record));
            }
            catch (Exception ex)
            {
                var record = new CloudinaryMigrationLogRecord
                {
                    RowId = row.TryGetValue("__RowId", out var rowId) ? rowId ?? string.Empty : string.Empty,
                    Status = "FAILED",
                    PublicId = ResolveFirst(row, mapping.PublicIdColumns),
                    Message = ex.Message
                };

                await AppendJsonLineAsync(logFile, record, cancellationToken).ConfigureAwait(false);
                reportRows.Add(ToReportCsv(record));
                logger.LogError(ex, "Cloudinary migration failed for row {RowId}.", record.RowId);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        var reportPath = Path.Combine(output, "report.csv");
        await File.WriteAllLinesAsync(reportPath,
        [
            "row_id,status,public_id,asset_id,secure_url,message",
            .. reportRows.OrderBy(x => x, StringComparer.Ordinal)
        ], cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Cloudinary migration complete. Output: {Output}.", output);
    }

    public async Task PreflightCheckAsync(CancellationToken cancellationToken = default)
    {
        var settings = ResolveRuntimeSettings();
        var rows = await LoadRowsAsync(settings.ManifestPath, cancellationToken).ConfigureAwait(false);
        var mapping = mappingLoader.Load(settings.MappingPath);
        var metadata = await structuredMetadataService.GetSchemasAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Preflight for CSV '{Csv}' with {Count} rows.", settings.ManifestPath, rows.Count);
        logger.LogInformation("Loaded {MetadataCount} structured metadata definitions from Cloudinary.", metadata.Count);

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("CSV manifest is empty.");
        }

        var first = rows[0];
        RequireColumn(first, mapping.FileColumns, "file");
        RequireColumn(first, mapping.PublicIdColumns, "public_id");
        RequireColumn(first, mapping.AssetFolderColumns, "asset_folder");

        foreach (var field in mapping.StructuredMetadata)
        {
            if (!first.ContainsKey(field.Column))
            {
                throw new InvalidOperationException($"Structured metadata source column '{field.Column}' was not found in the CSV.");
            }

            if (!metadata.ContainsKey(field.ExternalId))
            {
                throw new InvalidOperationException($"Cloudinary structured metadata field '{field.ExternalId}' does not exist.");
            }
        }

        var preview = await BuildUploadRequestAsync(first, mapping, cloudinaryOptions.Value, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Preflight preview: {Preview}", JsonSerializer.Serialize(preview, _jsonOptions));
    }

    public async Task ListMetadataFieldsAsync(CancellationToken cancellationToken = default)
    {
        var fields = await adminClient.GetMetadataFieldsAsync(cancellationToken).ConfigureAwait(false);
        foreach (var field in fields.OrderBy(x => x.ExternalId, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogInformation("Cloudinary field {ExternalId} ({Type}) datasource values: {Count}.",
                field.ExternalId,
                field.Type,
                field.DatasourceValues.Count);
        }
    }

    public async Task AuditMissingAssetsAsync(CancellationToken cancellationToken = default)
    {
        var settings = ResolveRuntimeSettings();
        var rows = await LoadRowsAsync(settings.ManifestPath, cancellationToken).ConfigureAwait(false);
        var mapping = mappingLoader.Load(settings.MappingPath);
        var output = CreateRunFolder(settings.OutputRoot, $"missing-asset-audit-{DateTime.UtcNow:yyyyMMddHHmmss}");

        var missing = new List<string>();
        var found = new List<string>();

        foreach (var row in rows)
        {
            var publicId = ResolveFirst(row, mapping.PublicIdColumns);
            if (string.IsNullOrWhiteSpace(publicId))
            {
                continue;
            }

            var exists = await adminClient.AssetExistsAsync(publicId, cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                found.Add(publicId);
            }
            else
            {
                missing.Add(publicId);
            }
        }

        await File.WriteAllLinesAsync(Path.Combine(output, "found-assets.csv"), ["public_id", .. found], cancellationToken).ConfigureAwait(false);
        await File.WriteAllLinesAsync(Path.Combine(output, "missing-assets.csv"), ["public_id", .. missing], cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(output, "summary.json"),
            JsonSerializer.Serialize(new { found = found.Count, missing = missing.Count, checkedAtUtc = DateTime.UtcNow }, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Missing asset audit complete. Found: {Found}. Missing: {Missing}. Output: {Output}.", found.Count, missing.Count, output);
    }

    public async Task DetectDuplicatePublicIdsAsync(CancellationToken cancellationToken = default)
    {
        var settings = ResolveRuntimeSettings();
        var rows = await LoadRowsAsync(settings.ManifestPath, cancellationToken).ConfigureAwait(false);
        var mapping = mappingLoader.Load(settings.MappingPath);

        var duplicates = rows
            .Select(row => ResolveFirst(row, mapping.PublicIdColumns))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x!, StringComparer.Ordinal)
            .Where(x => x.Count() > 1)
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ToList();

        foreach (var duplicate in duplicates)
        {
            logger.LogWarning("Duplicate manifest public_id '{PublicId}' appears {Count} times.", duplicate.Key, duplicate.Count());
        }

        if (duplicates.Count == 0)
        {
            logger.LogInformation("No duplicate public IDs found in the manifest.");
        }
    }

    public async Task DeleteAssetsFromManifestAsync(CancellationToken cancellationToken = default)
    {
        var settings = ResolveRuntimeSettings();
        EnsureSafeToRun(settings);

        var rows = await LoadRowsAsync(settings.ManifestPath, cancellationToken).ConfigureAwait(false);
        var mapping = mappingLoader.Load(settings.MappingPath);

        foreach (var row in rows)
        {
            var publicId = ResolveFirst(row, mapping.PublicIdColumns);
            if (string.IsNullOrWhiteSpace(publicId))
            {
                continue;
            }

            await adminClient.DeleteAsync(publicId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Delete routine completed for manifest '{ManifestPath}'.", settings.ManifestPath);
    }

    private async Task<CloudinaryUploadRequest> BuildUploadRequestAsync(
        IDictionary<string, string?> row,
        CloudinaryMappingProfile mapping,
        CloudinaryOptions options,
        CancellationToken cancellationToken)
    {
        var file = ResolveFirst(row, mapping.FileColumns);
        if (string.IsNullOrWhiteSpace(file))
        {
            throw new InvalidOperationException($"Row '{row["__RowId"]}' is missing a file reference.");
        }

        var tags = mapping.TagsColumns
            .SelectMany(column => SplitValues(row.TryGetValue(column, out var value) ? value : null, mapping.TagsSeparator))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var context = mapping.Context
            .Where(x => !string.IsNullOrWhiteSpace(x.Column) && !string.IsNullOrWhiteSpace(x.Key))
            .Select(x => new { x.Key, Value = row.TryGetValue(x.Column, out var raw) ? raw : null })
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(x => x.Key, x => x.Value!.Trim(), StringComparer.OrdinalIgnoreCase);

        var metadata = await structuredMetadataService.BuildMetadataAsync(row, mapping.StructuredMetadata, cancellationToken).ConfigureAwait(false);

        return new CloudinaryUploadRequest
        {
            File = file,
            PublicId = ResolveFirst(row, mapping.PublicIdColumns),
            AssetFolder = ResolveFirst(row, mapping.AssetFolderColumns),
            ResourceType = mapping.Defaults.ResourceType,
            Type = mapping.Defaults.Type,
            UploadPreset = mapping.Defaults.UploadPreset ?? options.UploadPreset,
            Overwrite = mapping.Defaults.Overwrite,
            Invalidate = mapping.Defaults.Invalidate,
            UniqueFilename = mapping.Defaults.UniqueFilename,
            UseFilename = mapping.Defaults.UseFilename,
            Tags = tags,
            Context = context,
            Metadata = metadata
        };
    }

    private static void RequireColumn(IDictionary<string, string?> row, IReadOnlyCollection<string> candidates, string logicalName)
    {
        if (ResolveFirst(row, candidates) is null)
        {
            throw new InvalidOperationException($"Required CSV column for '{logicalName}' was not found. Candidates: {string.Join(", ", candidates)}");
        }
    }

    private RuntimeSettings ResolveRuntimeSettings()
    {
        var value = migrationOptions.Value;

        if (string.IsNullOrWhiteSpace(value.ManifestPath))
            throw new InvalidOperationException("CloudinaryCsvMigration:ManifestPath is required.");
        if (string.IsNullOrWhiteSpace(value.MappingPath))
            throw new InvalidOperationException("CloudinaryCsvMigration:MappingPath is required.");

        return new RuntimeSettings(
            Path.GetFullPath(value.ManifestPath),
            Path.GetFullPath(value.MappingPath),
            Path.GetFullPath(value.OutputRoot ?? Path.Combine(AppContext.BaseDirectory, "output")),
            $"{value.DefaultRunPrefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            cloudinaryOptions.Value.MaxConcurrentUploads,
            value.ConfirmDeletes,
            value.ConfirmProduction);
    }

    private void EnsureSafeToRun(RuntimeSettings settings)
    {
        if (settings.ConfirmProduction && Environment.GetEnvironmentVariable("MIGRATION_CONFIRM_PROD")?.Equals("true", StringComparison.OrdinalIgnoreCase) != true)
        {
            logger.LogWarning("Production confirmation is enabled. Set MIGRATION_CONFIRM_PROD=true to proceed with destructive or write operations.");
        }
    }

    private static async Task<List<Dictionary<string, string?>>> LoadRowsAsync(string csvPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException("CSV manifest file not found.", csvPath);
        }

        var rows = new List<Dictionary<string, string?>>();
        await using var stream = File.OpenRead(csvPath);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        await foreach (var record in csv.GetRecordsAsync<dynamic>(cancellationToken))
        {
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in (IDictionary<string, object>)record)
            {
                row[pair.Key] = pair.Value?.ToString();
            }

            row["__RowId"] = rows.Count.ToString(CultureInfo.InvariantCulture);
            rows.Add(row);
        }

        return rows;
    }

    private static async Task AppendJsonLineAsync(string path, CloudinaryMigrationLogRecord record, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.AppendAllTextAsync(path, JsonSerializer.Serialize(record) + Environment.NewLine, cancellationToken).ConfigureAwait(false);
    }

    private static string CreateRunFolder(string outputRoot, string runName)
    {
        var path = Path.Combine(outputRoot, runName);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string? ResolveFirst(IDictionary<string, string?> row, IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (row.TryGetValue(candidate, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static IEnumerable<string> SplitValues(string? value, string separator)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string ToReportCsv(CloudinaryMigrationLogRecord record)
        => string.Join(",",
            Escape(record.RowId),
            Escape(record.Status),
            Escape(record.PublicId),
            Escape(record.AssetId),
            Escape(record.SecureUrl),
            Escape(record.Message));

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private sealed record RuntimeSettings(
        string ManifestPath,
        string MappingPath,
        string OutputRoot,
        string RunName,
        int MaxConcurrency,
        bool ConfirmDeletes,
        bool ConfirmProduction);
}
