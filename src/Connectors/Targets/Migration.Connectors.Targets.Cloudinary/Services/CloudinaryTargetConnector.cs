using Migration.Application.Abstractions;
using Migration.Domain.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Migration.Connectors.Targets.Cloudinary;

public sealed class CloudinaryTargetConnector : IAssetTargetConnector
{
    public string Type => "Cloudinary";

    public async Task<MigrationResult> UpsertAsync(MigrationJobDefinition job, AssetWorkItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            var cloudName = Resolve(job.Settings, "CloudinaryCloudName", "CloudName");
            var apiKey = Resolve(job.Settings, "CloudinaryApiKey", "ApiKey");
            var apiSecret = Resolve(job.Settings, "CloudinaryApiSecret", "ApiSecret");

            if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new InvalidOperationException("Cloudinary credentials must be provided in job.Settings.");
            }

            var file = ResolveFile(item);
            if (string.IsNullOrWhiteSpace(file))
            {
                throw new InvalidOperationException("Cloudinary target connector could not resolve a source file or URL for the work item.");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            var cloudinary = new CloudinaryDotNet.Cloudinary(account);

            var payload = item.TargetPayload;
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file),
                PublicId = payload is null ? null : ResolveField(payload, "public_id"),
                Folder = payload is null ? null : (ResolveField(payload, "asset_folder") ?? ResolveField(payload, "folder")),
                Type = payload is null ? "upload" : (ResolveField(payload, "type") ?? "upload"),
                UploadPreset = payload is null ? null : ResolveField(payload, "upload_preset"),
                Overwrite = payload is not null && ResolveBool(payload, "overwrite"),
                Invalidate = payload is not null && ResolveBool(payload, "invalidate"),
                Tags = payload is null ? string.Empty : ResolveTags(payload)
            };

            CloudinarySdkCompat.TrySetUploadProperty(uploadParams, "ResourceType", payload is null ? "auto" : (ResolveField(payload, "resource_type") ?? "auto"));
            CloudinarySdkCompat.TrySetUploadProperty(uploadParams, "Context", payload is null ? new Dictionary<string, object?>() : ResolveContext(payload));
            CloudinarySdkCompat.TrySetUploadProperty(uploadParams, "Metadata", payload is null ? new Dictionary<string, object?>() : ResolveMetadata(payload));

            var result = await Task.Run(() => cloudinary.Upload(uploadParams), cancellationToken).ConfigureAwait(false);
            if (result.Error is not null)
            {
                throw new InvalidOperationException(result.Error.Message);
            }

            return new MigrationResult
            {
                WorkItemId = item.WorkItemId,
                Success = true,
                TargetAssetId = result.AssetId ?? result.PublicId,
                Message = result.SecureUrl?.ToString() ?? "Cloudinary upsert completed."
            };
        }
        catch (Exception ex)
        {
            return new MigrationResult
            {
                WorkItemId = item.WorkItemId,
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static string? Resolve(IDictionary<string, string?> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? ResolveFile(AssetWorkItem item)
    {
        if (item.TargetPayload?.Fields.TryGetValue("file", out var file) == true)
        {
            return file?.ToString();
        }

        if (!string.IsNullOrWhiteSpace(item.Manifest.SourcePath))
        {
            return item.Manifest.SourcePath;
        }

        return item.SourceAsset?.Binary?.SourceUri ?? item.SourceAsset?.Path;
    }

    private static string? ResolveField(TargetAssetPayload payload, string key)
        => payload.Fields.TryGetValue(key, out var value) ? value?.ToString() : null;

    private static bool ResolveBool(TargetAssetPayload payload, string key)
        => payload.Fields.TryGetValue(key, out var value) && bool.TryParse(value?.ToString(), out var parsed) && parsed;

    private static string ResolveTags(TargetAssetPayload payload)
    {
        if (!payload.Fields.TryGetValue("tags", out var value) || value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            IEnumerable<string> strings => string.Join(",", strings),
            string tagString => tagString,
            _ => string.Empty
        };
    }

    private static Dictionary<string, object?> ResolveContext(TargetAssetPayload payload)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (payload.Fields.TryGetValue("context", out var value) && value is IDictionary<string, object?> context)
        {
            foreach (var pair in context)
            {
                if (pair.Value is not null)
                {
                    dictionary[pair.Key] = pair.Value.ToString() ?? string.Empty;
                }
            }
        }

        return dictionary;
    }

    private static Dictionary<string, object?> ResolveMetadata(TargetAssetPayload payload)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (payload.Fields.TryGetValue("metadata", out var value) && value is IDictionary<string, object?> metadata)
        {
            foreach (var pair in metadata)
            {
                if (pair.Value is not null)
                {
                    dictionary[pair.Key] = pair.Value.ToString() ?? string.Empty;
                }
            }
        }

        return dictionary;
    }
}
