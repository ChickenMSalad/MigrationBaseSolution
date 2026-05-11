using Migration.Application.Models;
using Migration.Domain.Models;

namespace Migration.Application.Abstractions;

public interface IMapper
{
    TargetAssetPayload Map(AssetEnvelope sourceAsset, ManifestRow row, MappingProfile profile);
}
