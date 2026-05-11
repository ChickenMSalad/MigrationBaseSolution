namespace Migration.Infrastructure.Taxonomy;

public sealed class TaxonomyBuilderOptions
{
    public BynderTaxonomyOptions Bynder { get; init; } = new();
    public CloudinaryTaxonomyOptions Cloudinary { get; init; } = new();
    public AprimoTaxonomyOptions Aprimo { get; init; } = new();
}

public sealed class BynderTaxonomyOptions
{
    public string BaseUrl { get; init; } = string.Empty;
    public string BearerToken { get; init; } = string.Empty;
}

public sealed class CloudinaryTaxonomyOptions
{
    public string CloudName { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
}

public sealed class AprimoTaxonomyOptions
{
    public string BaseUrl { get; init; } = string.Empty;
    public string BearerToken { get; init; } = string.Empty;
}
