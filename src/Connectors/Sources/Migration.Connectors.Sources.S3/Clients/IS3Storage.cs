using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mime;

namespace Migration.Connectors.Sources.S3.Clients
{

    public interface IS3Storage
    {
        Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken ct = default);

        Task<bool> ExistsAsync(string key, CancellationToken ct = default);

        Task<long> GetAssetSizeAsync(string key, CancellationToken ct = default);

        Task<Stream> OpenReadAsync(string key, CancellationToken ct = default);
        Task<string> ReadTextAsync(string key, CancellationToken ct = default);

        Task UploadAsync(
            string key,
            Stream content,
            string contentType = MediaTypeNames.Application.Octet,
            IDictionary<string, string>? metadata = null,
            CancellationToken ct = default);

        Task UploadFileAsync(
            string key,
            string filePath,
            string contentType = MediaTypeNames.Application.Octet,
            IDictionary<string, string>? metadata = null,
            CancellationToken ct = default);

        Task UploadTextAsync(
            string key,
            string text,
            string contentType = MediaTypeNames.Text.Plain,
            IDictionary<string, string>? metadata = null,
            CancellationToken ct = default);

        Task CopyAsync(string sourceKey, string destinationKey, CancellationToken ct = default);
        Task MoveAsync(string sourceKey, string destinationKey, CancellationToken ct = default);

        Task DeleteAsync(string key, CancellationToken ct = default);

        Uri GetPreSignedUrl(string key, TimeSpan expiresIn, string? responseContentType = null);
    }

}
