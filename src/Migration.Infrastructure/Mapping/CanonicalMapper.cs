using Migration.Application.Abstractions;
using Migration.Application.Models;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Infrastructure.Mapping;

public sealed class CanonicalMapper : IMapper
{
    private readonly IMappingValueTransformer _valueTransformer;

    public CanonicalMapper()
        : this(new DefaultMappingValueTransformer())
    {
    }

    public CanonicalMapper(IMappingValueTransformer valueTransformer)
    {
        _valueTransformer = valueTransformer;
    }

    public TargetAssetPayload Map(AssetEnvelope sourceAsset, ManifestRow row, MappingProfile profile)
    {
        var payload = new TargetAssetPayload
        {
            TargetType = Enum.TryParse<ConnectorType>(profile.TargetType, true, out var parsed) ? parsed : default,
            Name = sourceAsset.Metadata.TryGetValue("name", out var name) ? name : sourceAsset.SourceAssetId,
            Binary = sourceAsset.Binary
        };

        foreach (var fieldMap in profile.FieldMappings)
        {
            sourceAsset.Metadata.TryGetValue(fieldMap.Source, out var value);
            if (value is null && row.Columns.TryGetValue(fieldMap.Source, out var manifestValue))
            {
                value = manifestValue;
            }

            var transformedValue = _valueTransformer.Transform(new MappingValueTransformContext
            {
                TransformName = fieldMap.Transform,
                Value = value,
                FieldMap = fieldMap,
                Profile = profile,
                SourceAsset = sourceAsset,
                ManifestRow = row
            });

            payload.Fields[fieldMap.Target] = transformedValue;
        }

        return payload;
    }
}
