using Migration.Connectors.Targets.Cloudinary.Models;

namespace Migration.Connectors.Targets.Cloudinary.Clients;

public interface ICloudinaryAdminClient
{
    Task<IReadOnlyList<CloudinaryMetadataFieldSchema>> GetMetadataFieldsAsync(CancellationToken cancellationToken = default);
    Task<bool> AssetExistsAsync(string publicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> FindDuplicatePublicIdsAsync(IEnumerable<string> publicIds, CancellationToken cancellationToken = default);
    Task DeleteAsync(string publicId, string resourceType = "image", string type = "upload", CancellationToken cancellationToken = default);
}
