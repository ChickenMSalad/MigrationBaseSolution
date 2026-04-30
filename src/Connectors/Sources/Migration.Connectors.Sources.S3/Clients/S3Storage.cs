using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

using Microsoft.Extensions.Options;
using Migration.Connectors.Sources.S3.Configuration;

using System.Net;
using System.Net.Mime;
using System.Text;

namespace Migration.Connectors.Sources.S3.Clients
{


    public sealed class S3Storage : IS3Storage
    {
        private readonly IAmazonS3 _s3;
        private readonly S3Options _opt;

        public S3Storage(IAmazonS3 s3, IOptions<S3Options> options)
        {
            _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
            _opt = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_opt.BucketName))
                throw new InvalidOperationException("S3:BucketName is required.");
        }

        public async Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken ct = default)
        {
            var keys = new List<string>();

            string? token = null;
            do
            {
                var req = new ListObjectsV2Request
                {
                    BucketName = _opt.BucketName,
                    Prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix,
                    ContinuationToken = token
                };

                var res = await _s3.ListObjectsV2Async(req, ct).ConfigureAwait(false);
                foreach (var o in res.S3Objects)
                    keys.Add(o.Key);

                token = (bool)res.IsTruncated ? res.NextContinuationToken : null;
            }
            while (token is not null);

            return keys;
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            try
            {
                var req = new GetObjectMetadataRequest
                {
                    BucketName = _opt.BucketName,
                    Key = key
                };

                await _s3.GetObjectMetadataAsync(req, ct).ConfigureAwait(false);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<long> GetAssetSizeAsync(string key, CancellationToken ct = default)
        {
            try
            {
                var meta = new GetObjectMetadataRequest
                {
                    BucketName = _opt.BucketName,
                    Key = key
                };
                var res = await _s3.GetObjectMetadataAsync(meta, ct).ConfigureAwait(false);
                long size = res.ContentLength;
                return size;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return 0;
            }
        }

        public async Task<Stream> OpenReadAsync(string key, CancellationToken ct = default)
        {

            var res = await _s3.GetObjectAsync(_opt.BucketName, key, ct).ConfigureAwait(false);

            // Copy to a MemoryStream so callers can dispose safely without holding network resources.
            // For very large objects, you may want to return res.ResponseStream directly instead.
            var ms = new MemoryStream();
            await res.ResponseStream.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Position = 0;
            return ms;
        }

        public async Task<string> ReadTextAsync(string key, CancellationToken ct = default)
        {
            await using var s = await OpenReadAsync(key, ct).ConfigureAwait(false);
            using var reader = new StreamReader(s, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 8192, leaveOpen: false);
            return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        }

        public async Task UploadAsync(
            string key,
            Stream content,
            string contentType = MediaTypeNames.Application.Octet,
            IDictionary<string, string>? metadata = null,
            CancellationToken ct = default)
        {
            if (content is null) throw new ArgumentNullException(nameof(content));

            // Ensure stream position
            if (content.CanSeek) content.Position = 0;

            var put = new PutObjectRequest
            {
                BucketName = _opt.BucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType ?? MediaTypeNames.Application.Octet
            };

            if (metadata is not null)
            {
                foreach (var kv in metadata)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key))
                        put.Metadata[kv.Key] = kv.Value ?? "";
                }
            }

            var res = await _s3.PutObjectAsync(put, ct).ConfigureAwait(false);
            // Optionally validate res.ETag, etc.
        }

        public async Task UploadFileAsync(
            string key,
            string filePath,
            string contentType = MediaTypeNames.Application.Octet,
            IDictionary<string, string>? metadata = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is required.", nameof(filePath));

            // TransferUtility handles multipart uploads for large files.
            var tu = new TransferUtility(_s3);

            var req = new TransferUtilityUploadRequest
            {
                BucketName = _opt.BucketName,
                Key = key,
                FilePath = filePath,
                ContentType = contentType ?? MediaTypeNames.Application.Octet
            };

            if (metadata is not null)
            {
                foreach (var kv in metadata)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key))
                        req.Metadata[kv.Key] = kv.Value ?? "";
                }
            }

            await tu.UploadAsync(req, ct).ConfigureAwait(false);
        }

        public async Task UploadTextAsync(
            string key,
            string text,
            string contentType = MediaTypeNames.Text.Plain,
            IDictionary<string, string>? metadata = null,
            CancellationToken ct = default)
        {
            var bytes = Encoding.UTF8.GetBytes(text ?? "");
            await using var ms = new MemoryStream(bytes);
            await UploadAsync(key, ms, contentType, metadata, ct).ConfigureAwait(false);
        }

        public async Task CopyAsync(string sourceKey, string destinationKey, CancellationToken ct = default)
        {
            var req = new CopyObjectRequest
            {
                SourceBucket = _opt.BucketName,
                SourceKey = sourceKey,
                DestinationBucket = _opt.BucketName,
                DestinationKey = destinationKey
            };

            await _s3.CopyObjectAsync(req, ct).ConfigureAwait(false);
        }

        public async Task MoveAsync(string sourceKey, string destinationKey, CancellationToken ct = default)
        {
            await CopyAsync(sourceKey, destinationKey, ct).ConfigureAwait(false);
            await DeleteAsync(sourceKey, ct).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string key, CancellationToken ct = default)
        {
            var req = new DeleteObjectRequest
            {
                BucketName = _opt.BucketName,
                Key = key
            };

            await _s3.DeleteObjectAsync(req, ct).ConfigureAwait(false);
        }

        public Uri GetPreSignedUrl(string key, TimeSpan expiresIn, string? responseContentType = null)
        {
            var req = new GetPreSignedUrlRequest
            {
                BucketName = _opt.BucketName,
                Key = key,
                Expires = DateTime.UtcNow.Add(expiresIn),
                Verb = HttpVerb.GET
            };

            if (!string.IsNullOrWhiteSpace(responseContentType))
                req.ResponseHeaderOverrides.ContentType = responseContentType;

            var url = _s3.GetPreSignedURL(req);
            return new Uri(url);
        }


        public async Task DownloadToFileAsync(string key, string localPath, CancellationToken ct = default)
        {

            var res = await _s3.GetObjectAsync(_opt.BucketName, key, ct).ConfigureAwait(false);

            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);

            await using (res.ResponseStream)
            await using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.Read, 1024 * 128, useAsync: true))
            {
                await res.ResponseStream.CopyToAsync(fs, 1024 * 128, ct);
            }
        }
    }
}
