namespace Migration.Shared.Storage.S3;

public sealed record S3ClientOptions(
    string AccessKeyId,
    string SecretAccessKey,
    string Region,
    string? ServiceUrl = null,
    bool ForcePathStyle = false,
    string? SessionToken = null);
