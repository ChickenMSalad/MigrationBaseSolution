using Amazon.S3;
using Amazon.S3.Model;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Connectors.Targets.S3.Configuration;
using Migration.Domain.Models;
using Migration.Shared.Storage.S3;

namespace Migration.Connectors.Targets.S3;

public sealed class S3TargetConnector : IAssetTargetConnector
{
    private readonly IS3ClientFactory _clientFactory;
    private readonly S3TargetOptions _options;
    private readonly ILogger<S3TargetConnector> _logger;

    public S3TargetConnector(
        IS3ClientFactory clientFactory,
        IOptions<S3TargetOptions> options,
        ILogger<S3TargetConnector> logger)
    {
        _clientFactory = clientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Type => "S3";

    public async Task<MigrationResult> UpsertAsync(
        MigrationJobDefinition job,
        AssetWorkItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(item);

        var accessKey = FirstNonEmpty(
            GetSetting(job, "S3TargetAccessKey", "TargetAccessKey", "S3AccessKey", "TargetCredential_AccessKey"),
            _options.AccessKey);

        var secretKey = FirstNonEmpty(
            GetSetting(job, "S3TargetSecretKey", "TargetSecretKey", "S3SecretKey", "TargetCredential_SecretKey"),
            _options.SecretKey);

        var region = FirstNonEmpty(
            GetSetting(job, "S3TargetRegion", "TargetRegion", "S3Region", "TargetCredential_Region"),
            _options.Region);

        var bucketName = FirstNonEmpty(
            GetSetting(job, "S3TargetBucket", "TargetBucket", "S3Bucket", "TargetCredential_Bucket"),
            _options.BucketName);

        var configuredPrefix = FirstNonEmpty(
            GetSetting(job, "S3TargetPrefix", "TargetPrefix", "S3Prefix", "TargetCredential_Prefix"),
            _options.Prefix);

        var serviceUrl = FirstNonEmpty(
            GetSetting(job, "S3TargetServiceUrl", "TargetServiceUrl", "S3ServiceUrl", "TargetCredential_ServiceUrl"),
            _options.ServiceUrl);

        var forcePathStyle = GetBool(job, _options.ForcePathStyle,
            "S3TargetForcePathStyle", "TargetForcePathStyle", "S3ForcePathStyle", "TargetCredential_ForcePathStyle");

        if (string.IsNullOrWhiteSpace(accessKey))
        {
            return Fail(item, "S3 target access key is missing. Set S3Target:AccessKey or job setting S3TargetAccessKey.");
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return Fail(item, "S3 target secret key is missing. Set S3Target:SecretKey or job setting S3TargetSecretKey.");
        }

        if (string.IsNullOrWhiteSpace(region) && string.IsNullOrWhiteSpace(serviceUrl))
        {
            return Fail(item, "S3 target region is missing. Set S3Target:Region or job setting S3TargetRegion.");
        }

        if (string.IsNullOrWhiteSpace(bucketName))
        {
            return Fail(item, "S3 target bucket is missing. Set S3Target:BucketName or job setting S3TargetBucket.");
        }

        var binary = item.TargetPayload?.Binary ?? item.SourceAsset?.Binary;
        if (binary is null || string.IsNullOrWhiteSpace(binary.SourceUri))
        {
            return Fail(item, "Target payload has no binary SourceUri to upload to S3.");
        }

        var objectKey = ResolveObjectKey(item, configuredPrefix, binary);

        using var client = _clientFactory.Create(new S3ClientOptions(
            accessKey,
            secretKey,
            region ?? string.Empty,
            serviceUrl,
            forcePathStyle));

        await using var sourceStream = await OpenBinaryStreamAsync(binary.SourceUri, cancellationToken).ConfigureAwait(false);
        if (sourceStream.CanSeek && sourceStream.Length <= 0)
        {
            return Fail(item, $"Binary stream is empty. SourceUri={binary.SourceUri}");
        }

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = sourceStream,
            AutoCloseStream = false,
            ContentType = FirstNonEmpty(binary.ContentType, GuessContentType(objectKey))
        };

        _logger.LogInformation("Uploading work item {WorkItemId} to S3 bucket {BucketName} key {ObjectKey}.", item.WorkItemId, bucketName, objectKey);
        await client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);

        return new MigrationResult
        {
            WorkItemId = item.WorkItemId,
            Success = true,
            TargetAssetId = $"s3://{bucketName}/{objectKey}",
            Message = $"Uploaded to S3 key '{objectKey}'."
        };
    }

    private static string ResolveObjectKey(AssetWorkItem item, string? configuredPrefix, AssetBinary binary)
    {
        var candidate = FirstNonEmpty(
            GetValue(item.TargetPayload?.Fields, "targetPath", "TargetPath", "objectKey", "ObjectKey", "s3Key", "S3Key", "key", "Key"),
            GetValue(item.Manifest.Columns, "targetPath", "TargetPath", "objectKey", "ObjectKey", "s3Key", "S3Key", "key", "Key"),
            item.Manifest.SourcePath,
            item.SourceAsset?.Path,
            binary.FileName,
            item.TargetPayload?.Name,
            item.Manifest.SourceAssetId,
            item.WorkItemId) ?? item.WorkItemId;

        candidate = NormalizeKey(candidate);
        if (string.IsNullOrWhiteSpace(Path.GetExtension(candidate))
            && !string.IsNullOrWhiteSpace(binary.FileName))
        {
            candidate = CombineKey(candidate, Path.GetFileName(binary.FileName)) ?? candidate;
        }

        return CombineKey(configuredPrefix, candidate) ?? SanitizeSegment(item.WorkItemId);
    }

    private static async Task<Stream> OpenBinaryStreamAsync(string sourceUri, CancellationToken cancellationToken)
    {
        if (Uri.TryCreate(sourceUri, UriKind.Absolute, out var uri))
        {
            if (uri.IsFile)
            {
                return File.OpenRead(uri.LocalPath);
            }

            if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                var http = new HttpClient();
                return await http.GetStreamAsync(uri, cancellationToken).ConfigureAwait(false);
            }
        }

        return File.OpenRead(sourceUri);
    }

    private static string NormalizeKey(string value) => value.Replace('\\', '/').Trim('/');

    private static string? CombineKey(params string?[] parts)
    {
        var clean = new List<string>();

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var normalized = part.Replace('\\', '/');
            foreach (var rawSegment in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                var segment = SanitizeSegment(rawSegment);
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    clean.Add(segment);
                }
            }
        }

        return clean.Count == 0 ? null : string.Join('/', clean);
    }

    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    private static string? GetSetting(MigrationJobDefinition job, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (job.Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? GetValue(IReadOnlyDictionary<string, string>? values, params string[] keys)
    {
        if (values is null)
        {
            return null;
        }

        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? GetValue(IReadOnlyDictionary<string, object>? values, params string[] keys)
    {
        if (values is null)
        {
            return null;
        }

        foreach (var key in keys)
        {
            if (!values.TryGetValue(key, out var raw) || raw is null)
            {
                continue;
            }

            var value = raw switch
            {
                string text => text,
                JsonElement json when json.ValueKind == JsonValueKind.String => json.GetString(),
                JsonElement json when json.ValueKind == JsonValueKind.Number => json.ToString(),
                JsonElement json when json.ValueKind == JsonValueKind.True => bool.TrueString,
                JsonElement json when json.ValueKind == JsonValueKind.False => bool.FalseString,
                _ => raw.ToString()
            };

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static bool GetBool(MigrationJobDefinition job, bool fallback, params string[] keys)
    {
        var value = GetSetting(job, keys);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static MigrationResult Fail(AssetWorkItem item, string message) => new()
    {
        WorkItemId = item.WorkItemId,
        Success = false,
        Message = message
    };

    private static string GuessContentType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".json" => "application/json",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
