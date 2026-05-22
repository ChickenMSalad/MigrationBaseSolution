using Migration.Application.Models;
using Migration.Domain.Models;

namespace Migration.Application.Abstractions;

/// <summary>
/// Applies field-level mapping transforms declared on MappingProfile.FieldMappings[].Transform.
/// This runs while the canonical target payload is being built, before validation and before the target connector is called.
/// </summary>
public interface IMappingValueTransformer
{
    object? Transform(MappingValueTransformContext context);
}

public sealed class MappingValueTransformContext
{
    public required string? TransformName { get; init; }
    public required object? Value { get; init; }
    public required FieldMap FieldMap { get; init; }
    public required MappingProfile Profile { get; init; }
    public required AssetEnvelope SourceAsset { get; init; }
    public required ManifestRow ManifestRow { get; init; }
}
