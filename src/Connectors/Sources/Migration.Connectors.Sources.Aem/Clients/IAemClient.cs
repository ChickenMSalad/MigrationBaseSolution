
using Migration.Connectors.Sources.Aem.Models;
using Migration.Connectors.Sources.Aem.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Clients
{
    public interface IAemClient
    {
        Task<AemFolder> GetFolderAsync(string folderPath, ILogger log, CancellationToken ct = default, bool useLastModifiedOnly = false);
        IAsyncEnumerable<AemAsset> EnumerateAssetsAsync(string folderPath, bool recursive, ILogger log, CancellationToken ct = default, bool useLastModifiedOnly = false);
        Task<Stream> GetOriginalAsync(string assetPath, CancellationToken ct = default);
        Task<Stream> GetRelatedAsync(string assetPath, CancellationToken ct = default);
        Task<Stream> GetMetadataAsync(string assetPath, CancellationToken ct = default);

        Task<AemAsset> GetAssetByUUID(string uuid, ILogger log, CancellationToken ct = default);
        Task<AemAsset> GetAssetByPath(string path, ILogger log, CancellationToken ct = default);
        Task<Stream> GetJcrContentAsync(string assetPath, CancellationToken ct = default);

        Task<Stream> GetRenditionsAsync(string assetPath, string assetName, CancellationToken ct = default);

        Task<Stream> GetRenditionsFolderAsync(string assetPath, CancellationToken ct = default);
    }
};


