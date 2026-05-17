namespace Migration.ControlPlane.Storage;

public sealed record CloudBinaryStorageProviderCapabilities(
    string Provider,
    bool SupportsStreamingWrites,
    bool SupportsMultipartUploads,
    bool SupportsObjectTags,
    bool SupportsLeases,
    bool SupportsVersioning,
    bool SupportsConditionalWrites,
    bool SupportsSignedUrls);
