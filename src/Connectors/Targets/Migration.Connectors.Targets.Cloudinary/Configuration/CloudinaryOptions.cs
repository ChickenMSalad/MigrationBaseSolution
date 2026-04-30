namespace Migration.Connectors.Targets.Cloudinary.Configuration;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string? UploadPreset { get; set; }
    public bool Secure { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 60;
    public int UploadLargeThresholdBytes { get; set; } = 104_857_600;
    public int UploadLargeBufferSizeBytes { get; set; } = 20 * 1024 * 1024;
    public int MaxConcurrentUploads { get; set; } = 5;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CloudName))
            throw new InvalidOperationException("Cloudinary:CloudName is required.");
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("Cloudinary:ApiKey is required.");
        if (string.IsNullOrWhiteSpace(ApiSecret))
            throw new InvalidOperationException("Cloudinary:ApiSecret is required.");
    }
}
