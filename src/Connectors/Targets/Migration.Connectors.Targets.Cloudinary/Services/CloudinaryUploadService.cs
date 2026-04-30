using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Connectors.Targets.Cloudinary.Configuration;
using Migration.Connectors.Targets.Cloudinary;
using Migration.Connectors.Targets.Cloudinary.Models;

namespace Migration.Connectors.Targets.Cloudinary.Services;

public sealed class CloudinaryUploadService(
    CloudinaryClientFactory clientFactory,
    IOptions<CloudinaryOptions> options,
    ILogger<CloudinaryUploadService> logger)
{
    public async Task<ImageUploadResult> UploadAsync(CloudinaryUploadRequest request, CancellationToken cancellationToken = default)
    {
        var uploadParams = CreateUploadParams(request);
        var client = clientFactory.Create();

        ImageUploadResult result;
        if (ShouldUseLargeUpload(request.File))
        {
            logger.LogInformation("Uploading large asset '{File}' using Cloudinary large-upload support when available.", request.File);
            result = await TryUploadLargeAsync(client, uploadParams, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            result = await Task.Run(() => client.Upload(uploadParams), cancellationToken).ConfigureAwait(false);
        }

        if (result.Error is not null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
        }

        return result;
    }

    private ImageUploadParams CreateUploadParams(CloudinaryUploadRequest request)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(request.File),
            PublicId = request.PublicId,
            Folder = request.AssetFolder,
            Tags = string.Join(",", request.Tags),
            Overwrite = request.Overwrite,
            Invalidate = request.Invalidate,
            UploadPreset = request.UploadPreset,
            Type = request.Type,
            UniqueFilename = request.UniqueFilename,
            UseFilename = request.UseFilename
        };

        CloudinarySdkCompat.TrySetUploadProperty(uploadParams, "ResourceType", request.ResourceType);
        CloudinarySdkCompat.TrySetUploadProperty(uploadParams, "Context", request.Context);
        CloudinarySdkCompat.TrySetUploadProperty(uploadParams, "Metadata", request.Metadata);

        return uploadParams;
    }

    private async Task<ImageUploadResult> TryUploadLargeAsync(CloudinaryDotNet.Cloudinary client, ImageUploadParams uploadParams, CancellationToken cancellationToken)
    {
        var method = client.GetType().GetMethods()
            .FirstOrDefault(x => string.Equals(x.Name, "UploadLarge", StringComparison.Ordinal)
                              && x.GetParameters().Length >= 1);

        if (method is not null)
        {
            return await Task.Run(() =>
            {
                object?[] parameters = method.GetParameters().Length switch
                {
                    1 => new object?[] { uploadParams },
                    2 => new object?[] { uploadParams, options.Value.UploadLargeBufferSizeBytes },
                    3 => new object?[] { uploadParams, options.Value.UploadLargeBufferSizeBytes, options.Value.MaxConcurrentUploads },
                    _ => new object?[] { uploadParams }
                };

                var response = method.Invoke(client, parameters);
                return response is ImageUploadResult typed
                    ? typed
                    : throw new InvalidOperationException("Cloudinary UploadLarge returned an unexpected response type.");
            }, cancellationToken).ConfigureAwait(false);
        }

        return await Task.Run(() => client.Upload(uploadParams), cancellationToken).ConfigureAwait(false);
    }


    private bool ShouldUseLargeUpload(string file)
    {
        if (Uri.TryCreate(file, UriKind.Absolute, out var uri) && !uri.IsFile)
        {
            return false;
        }

        if (!File.Exists(file))
        {
            return false;
        }

        var length = new FileInfo(file).Length;
        return length > options.Value.UploadLargeThresholdBytes;
    }
}
