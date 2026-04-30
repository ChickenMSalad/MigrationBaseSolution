
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Clients;

public interface IBlobStorageService
{
    Task UploadStreamAsync(string container, string blobPath, Stream content, string contentType, IDictionary<string,string>? tags = null, CancellationToken ct = default);
    Task UploadJsonAsync<T>(string container, string blobPath, T data, CancellationToken ct = default);
}
