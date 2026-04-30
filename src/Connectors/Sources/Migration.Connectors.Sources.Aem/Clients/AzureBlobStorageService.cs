
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Migration.Connectors.Sources.Aem.Clients;
using Migration.Connectors.Sources.Aem.Configuration;
using Migration.Shared.Configuration.Hosts.Aem;
using Migration.Shared.Configuration.Infrastructure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Migration.Connectors.Sources.Aem.Clients;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly AzureBlobStorageOptions _opt;
    private readonly BlobServiceClient _svc;

    public AzureBlobStorageService(AzureBlobStorageOptions opt)
    {
        _opt = opt;
        _svc = new BlobServiceClient(_opt.ConnectionString);
    }

    public async Task UploadStreamAsync(string container, string blobPath, Stream content, string contentType, IDictionary<string,string>? tags = null, CancellationToken ct = default)
    {
        var cont = _svc.GetBlobContainerClient(container);
        await cont.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var client = cont.GetBlobClient(blobPath);
        var headers = new BlobHttpHeaders { ContentType = contentType };
        await client.UploadAsync(content, new BlobUploadOptions { HttpHeaders = headers, Tags = tags }, ct);
    }

    public async Task UploadJsonAsync<T>(string container, string blobPath, T data, CancellationToken ct = default)
    {
        var cont = _svc.GetBlobContainerClient(container);
        await cont.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var client = cont.GetBlobClient(blobPath);
        var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        using var ms = new MemoryStream(bytes);
        await client.UploadAsync(ms, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" } }, ct);
    }
}
