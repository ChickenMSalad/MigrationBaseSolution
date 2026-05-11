using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Shared.Storage
{
    public interface IAzureBlobWrapperAsync
    {
        Task UploadBlobAsync(string fileName, Stream content, string? folderPath = null, bool overwrite = true, int maxConcurrency = 5,
            IDictionary<string, string>? metadata = null,
            IProgress<(string BlobName, long BytesUploaded)>? progress = null,
            CancellationToken cancellationToken = default);

        Task UploadBlobsAsync(IEnumerable<(string FileName, Func<Task<Stream>> ContentFactory, IDictionary<string, string>? Metadata)> blobs,
            string? folderPath = null, bool overwrite = true, int maxConcurrency = 5,
            IProgress<(string BlobName, long BytesUploaded)>? progress = null,
            CancellationToken cancellationToken = default);

        Task<List<BlobUploadResult>> UploadBlobsAsync(IEnumerable<(string FileName, Func<Task<MemoryStream>> ContentFactory, IDictionary<string, string>? Metadata)> blobs,
            string? folderPath = null, bool overwrite = true, int maxConcurrency = 5,
            IProgress<(string BlobName, long BytesUploaded)>? progress = null,
            CancellationToken cancellationToken = default);

        Task<bool> BlobExistsAsync(string fileName, string? folderPath = null, CancellationToken cancellationToken = default);

        Task<Stream?> DownloadBlobAsync(string fileName, string? folderPath = null, CancellationToken cancellationToken = default);

        Task DownloadBlobToFileAsync(string fileName, string localPath, string? folderPath = null, CancellationToken cancellationToken = default);

        Task DeleteBlobAsync(string fileName, string? folderPath = null, CancellationToken cancellationToken = default);

        Task MoveBlobAsync(string? sourceName, string targetName, CancellationToken cancellationToken = default);
    }

}
