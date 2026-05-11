namespace Migration.Connectors.Targets.S3.Configuration;

public sealed class S3TargetOptions
{
    public const string SectionName = "S3Target";

    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
    public string? Region { get; init; }
    public string? BucketName { get; init; }
    public string? Prefix { get; init; }
    public string? ServiceUrl { get; init; }
    public bool ForcePathStyle { get; init; }
    public bool Overwrite { get; init; } = true;
}
