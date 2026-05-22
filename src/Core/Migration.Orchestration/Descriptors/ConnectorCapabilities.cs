namespace Migration.Orchestration.Descriptors;

[Flags]
public enum ConnectorCapabilities
{
    None = 0,

    // Existing (preserve values for compatibility)
    ReadAsset = 1 << 0,
    ReadBinary = 1 << 1,

    WriteAsset = 1 << 2,
    WriteMetadata = 1 << 3,

    DryRun = 1 << 4,
    Resume = 1 << 5,

    Validate = 1 << 6,
    Preflight = 1 << 7,

    // Added (start after existing values)
    WriteBinary = 1 << 8,

    ReadMetadata = 1 << 9,

    ReadFolderPath = 1 << 10,
    WriteFolderPath = 1 << 11,

    ReadSidecarMetadata = 1 << 12,
    WriteSidecarMetadata = 1 << 13,

    ValidateCredentials = 1 << 14,
    ValidateAssetExists = 1 << 15,
    ValidateMetadataSchema = 1 << 16,

    // Compatibility aliases
    UpsertAsset = WriteAsset,
    DownloadBinary = ReadBinary,
    UploadBinary = WriteBinary,

    // Convenience groups
    SourceAssetRead =
        ReadAsset |
        ReadBinary |
        ReadMetadata,

    TargetAssetWrite =
        WriteAsset |
        WriteBinary |
        WriteMetadata,

    FolderAware =
        ReadFolderPath |
        WriteFolderPath,

    SidecarMetadata =
        ReadSidecarMetadata |
        WriteSidecarMetadata,

    PreflightReady =
        DryRun |
        Preflight
}