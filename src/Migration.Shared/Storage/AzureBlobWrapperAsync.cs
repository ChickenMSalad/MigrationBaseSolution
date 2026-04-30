using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

using Microsoft.Extensions.Logging;


namespace Migration.Shared.Storage
{
    public class AzureBlobWrapperAsync : IAzureBlobWrapperAsync
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<AzureBlobWrapperAsync> _logger;

        public AzureBlobWrapperAsync(string connectionString, string containerName, ILogger<AzureBlobWrapperAsync> logger)
        {
            _logger = logger;

            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(60) // ⏰ override global timeout
            };

            var transport = new HttpClientTransport(httpClient);

            var options = new BlobClientOptions
            {
                Retry =
            {
                MaxRetries = 3,
                Delay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromSeconds(60),
                Mode = Azure.Core.RetryMode.Exponential,
                NetworkTimeout = TimeSpan.FromMinutes(60),
            },
                Transport = transport
            };

            _containerClient = new BlobContainerClient(connectionString, containerName, options);
        }

        private static string CombinePath(string? folder, string fileName)
        {
            return string.IsNullOrWhiteSpace(folder)
                ? fileName
                : $"{folder.TrimEnd('/')}/{fileName}";
        }

        public async Task UploadBlobAsync(
            string fileName, Stream content, string? folderPath = null, bool overwrite = true, int maxConcurrency = 5,
            IDictionary<string, string>? metadata = null,
            IProgress<(string BlobName, long BytesUploaded)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var blobName = CombinePath(folderPath, fileName);
            var blobClient = _containerClient.GetBlobClient(blobName);

            _logger.LogInformation("Uploading blob: {BlobName}", blobName);

            var progressHandler = progress != null
                ? new Progress<long>(bytes => progress.Report((blobName, bytes)))
                : null;


            var resp = await blobClient.UploadAsync(content, new BlobUploadOptions
            {
                Metadata = metadata,
                ProgressHandler = progressHandler,
                TransferOptions = new StorageTransferOptions
                {
                    MaximumConcurrency = maxConcurrency,
                    //MaximumTransferLength = 4 * 1024 * 1024
                }
            }, cancellationToken);

        }

        public async Task UploadBlobsAsync(
            IEnumerable<(string FileName, Func<Task<Stream>> ContentFactory, IDictionary<string, string>? Metadata)> blobs,
            string? folderPath = null, bool overwrite = true,
            int maxConcurrency = 5,
            IProgress<(string BlobName, long BytesUploaded)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = cancellationToken
            };

            var tasks = blobs.Select(async blob =>
            {
                using var stream = await blob.ContentFactory();
                await UploadBlobAsync(blob.FileName, stream, folderPath, overwrite, maxConcurrency: maxConcurrency, blob.Metadata, progress, cancellationToken);
                ;
            });

            await Task.WhenAll(tasks);
        }

        public async Task<List<BlobUploadResult>> UploadBlobsAsync(
            IEnumerable<(string FileName, Func<Task<MemoryStream>> ContentFactory, IDictionary<string, string>? Metadata)> blobs,
            string? folderPath = null, bool overwrite = true, int maxConcurrency = 5,
            IProgress<(string BlobName, long BytesUploaded)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = cancellationToken
            };

            var tasks = blobs.Select(async blob =>
            {
                try
                {
                    using var stream = await blob.ContentFactory();
                    _logger.LogInformation($"Stream.CanRead: {stream.CanRead}, Length: {stream.Length}");
                    await UploadBlobAsync(blob.FileName, stream, folderPath, overwrite, maxConcurrency: maxConcurrency, blob.Metadata, progress, cancellationToken);
                    return new BlobUploadResult { FileName = blob.FileName, Success = true };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to upload blob: {blob.FileName}");
                    return new BlobUploadResult { FileName = blob.FileName, Success = false, Exception = ex };
                }

                ;
            });

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        public async Task<bool> BlobExistsAsync(string fileName, string? folderPath = null, CancellationToken cancellationToken = default)
        {
            var blobName = CombinePath(folderPath, fileName);
            var blobClient = _containerClient.GetBlobClient(blobName);
            var exists = await blobClient.ExistsAsync(cancellationToken);
            _logger.LogInformation("Blob {BlobName} exists: {Exists}", blobName, exists.Value);
            return exists.Value;
        }

        public async Task<Stream?> DownloadBlobAsync(string fileName, string? folderPath = null, CancellationToken cancellationToken = default)
        {
            var blobName = CombinePath(folderPath, fileName);
            var blobClient = _containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Blob not found: {BlobName}", blobName);
                return null;
            }

            //var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            //_logger.LogInformation("Downloaded blob: {BlobName}", blobName);
            //return response.Value.Content;

            var seekableStream = new MemoryStream();
            await blobClient.DownloadToAsync(seekableStream, cancellationToken);
            seekableStream.Position = 0;
            return seekableStream;
        }

        public async Task DownloadBlobToFileAsync(string fileName, string localPath, string? folderPath = null, CancellationToken cancellationToken = default)
        {
            var blobName = CombinePath(folderPath, fileName);
            var blobClient = _containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Blob not found: {BlobName}", blobName);
                return;
            }

            await blobClient.DownloadToAsync(localPath, cancellationToken);
            _logger.LogInformation("Blob {BlobName} downloaded to file {LocalPath}", blobName, localPath);
        }


        public Task<DateTimeOffset?> GetBlobLastModified(string fileName, string? folderPath = null, CancellationToken cancellationToken = default)
        {
            var blobName = CombinePath(folderPath, fileName);
            var blobClient = _containerClient.GetBlobClient(blobName);
            return GetBlobLastModified(blobClient, cancellationToken);
        }

        public async Task<DateTimeOffset?> GetBlobLastModified(BlobClient blobClient, CancellationToken cancellationToken = default)
        {
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return null;
            }
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return properties.Value.LastModified;
        }

        public Task<long?> GetBlobSize(string fileName, string? folderPath = null, CancellationToken cancellationToken = default)
        {
            var blobName = CombinePath(folderPath, fileName);
            var blobClient = _containerClient.GetBlobClient(blobName);
            return GetBlobSize(blobClient, cancellationToken);
        }

        public async Task<long?> GetBlobSize(BlobClient blobClient, CancellationToken cancellationToken = default)
        {
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return null;
            }
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return properties.Value.ContentLength;
        }

        public Task<BlobClient> GetBlobClientAsync(string fileName, string? folderPath = null)
        {
            var blobName = CombinePath(folderPath, fileName);
            return Task.FromResult(_containerClient.GetBlobClient(blobName));
        }

        public async Task SetTagsAsync(string blobName, IDictionary<string, string> tags, CancellationToken ct = default)
        {
            var blob = _containerClient.GetBlobClient(blobName);
            await blob.SetTagsAsync(tags, cancellationToken: ct);
        }

        public Task<BlobClient> GetNewBlobClientAsync(string fileName, string? folderPath = null)
        {
            var blobName = CombinePath(folderPath, fileName);
            return Task.FromResult(_containerClient.GetBlobClient(blobName));
        }

        public async Task MoveBlobAsync(BlobClient sourceBlob, BlobClient targetBlob, bool deleteSourceAfterCopy = true, CancellationToken cancellationToken = default)
        {
            if (!await sourceBlob.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Cannot move, source blob does not exist: {Source}", sourceBlob.Name);
                return;
            }

            await targetBlob.StartCopyFromUriAsync(sourceBlob.Uri, cancellationToken: cancellationToken);
            if (deleteSourceAfterCopy)
            {
                await sourceBlob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }
            _logger.LogInformation("Moved blob from {Source} to {Target}", sourceBlob.Name, targetBlob.Name);
        }
        public async Task DeleteBlobAsync(string fileName, string? folderPath = null, CancellationToken cancellationToken = default)
        {
            var blobName = CombinePath(folderPath, fileName);
            var blobClient = _containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("Deleted blob (if existed): {BlobName}", blobName);
        }

        public async Task MoveBlobAsync(string sourceName, string targetName, CancellationToken cancellationToken = default)
        {

            var sourceBlob = _containerClient.GetBlobClient(sourceName);
            var targetBlob = _containerClient.GetBlobClient(targetName);

            if (!await sourceBlob.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Cannot move, source blob does not exist: {Source}", sourceName);
                return;
            }

            await targetBlob.StartCopyFromUriAsync(sourceBlob.Uri, cancellationToken: cancellationToken);
            await sourceBlob.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Moved blob from {Source} to {Target}", sourceName, targetName);
        }

        public async Task<string[]> GetBlobListingAsync(string? folderPath = null, CancellationToken cancellationToken = default)
        {
            var prefix = string.IsNullOrEmpty(folderPath)
                ? folderPath
                : folderPath.TrimEnd('/') + "/";

            var blobNames = new List<string>();
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                var name = blobItem.Name;
                blobNames.Add(name);
            }

            return blobNames.ToArray();
        }

        public async Task<string[]> SearchBlobListingByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {

            var blobNames = new List<string>();
            await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                var name = blobItem.Name;
                _logger.LogInformation("Blob {BlobName} exists: {Exists}", name, true);
                blobNames.Add(name);
            }

            return blobNames.ToArray();
        }

        public async Task UploadLargeFileAsync(
                string blobName,
                string filePath,
                IProgress<(string BlobName, long BytesUploaded)>? progress = null,
                CancellationToken cancellationToken = default)
        {
            using var fileStream = File.OpenRead(filePath);

            _logger.LogInformation($"Upload large file {blobName}");

            var blockBlobClient = _containerClient.GetBlockBlobClient(blobName);
            var blockIds = new List<string>();

            const int blockSize = 4 * 1024 * 1024;
            int blockNumber = 0;
            long totalBytesUploaded = 0;

            byte[] buffer = new byte[blockSize];
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"block-{blockNumber:D6}"));
                using var blockStream = new MemoryStream(buffer, 0, bytesRead);

                await blockBlobClient.StageBlockAsync(blockId, blockStream, cancellationToken: cancellationToken);
                blockIds.Add(blockId);
                totalBytesUploaded += bytesRead;
                progress?.Report((blobName, totalBytesUploaded));

                blockNumber++;
            }

            await blockBlobClient.CommitBlockListAsync(blockIds, cancellationToken: cancellationToken);
        }
    }


}
