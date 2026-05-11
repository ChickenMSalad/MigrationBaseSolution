using Migration.Orchestration.Abstractions;

namespace Migration.Orchestration.Descriptors;

public sealed class KnownConnectorCatalog : IConnectorCatalog
{
    private readonly IReadOnlyList<ConnectorDescriptor> _sources;
    private readonly IReadOnlyList<ConnectorDescriptor> _targets;
    private readonly IReadOnlyList<ManifestProviderDescriptor> _manifestProviders;

    public KnownConnectorCatalog()
    {
        _sources = new[]
        {
            CreateWebDamSourceDescriptor(),
            CreateAzureBlobSourceDescriptor(),
            CreateS3SourceDescriptor(),
            CreateSitecoreSourceDescriptor(),
            CreateAemSourceDescriptor(),
            CreateLocalStorageSourceDescriptor()
        };

        _targets = new[]
        {
            CreateBynderTargetDescriptor(),
            CreateAzureBlobTargetDescriptor(),
            CreateCloudinaryTargetDescriptor(),
            CreateAprimoTargetDescriptor(),
            CreateLocalStorageTargetDescriptor()
        };

        _manifestProviders = new[]
        {
            new ManifestProviderDescriptor
            {
                Type = "Csv",
                DisplayName = "CSV",
                Description = "Comma-separated manifest file.",
                SupportedExtensions = { ".csv" },
                Options =
                {
                    new ConnectorOptionDescriptor { Name = "ManifestPath", DisplayName = "Manifest path", Required = true, Description = "Path to the CSV manifest file." }
                }
            },
            new ManifestProviderDescriptor
            {
                Type = "Excel",
                DisplayName = "Excel",
                Description = "Excel .xlsx/.xlsm manifest file.",
                SupportedExtensions = { ".xlsx", ".xlsm" },
                Options =
                {
                    new ConnectorOptionDescriptor { Name = "ManifestPath", DisplayName = "Manifest path", Required = true, Description = "Path to the Excel manifest file." },
                    new ConnectorOptionDescriptor { Name = "WorksheetName", DisplayName = "Worksheet name", Required = false, Description = "Optional worksheet name. If omitted, the first worksheet is used." },
                    new ConnectorOptionDescriptor { Name = "HeaderRow", DisplayName = "Header row", Required = false, DefaultValue = "1" },
                    new ConnectorOptionDescriptor { Name = "FirstDataRow", DisplayName = "First data row", Required = false, DefaultValue = "2" }
                }
            }
        };
    }

    public IReadOnlyList<ConnectorDescriptor> GetSources() => _sources;
    public IReadOnlyList<ConnectorDescriptor> GetTargets() => _targets;
    public IReadOnlyList<ManifestProviderDescriptor> GetManifestProviders() => _manifestProviders;
    public IReadOnlyList<ConnectorDescriptor> GetAll() => _sources.Concat(_targets).ToList();
    public ConnectorDescriptor? Find(string type) => GetAll().FirstOrDefault(x => x.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

    private static ConnectorDescriptor CreateWebDamSourceDescriptor() => new()
    {
        Type = "WebDam",
        DisplayName = "WebDam",
        Direction = ConnectorDirections.Source,
        Kind = "Source",
        Description = "Reads metadata and binaries from WebDam using either a WebDam asset id or a pre-staged binary path.",
        Capabilities = { ConnectorCapabilities.ReadAsset, ConnectorCapabilities.ReadBinary, ConnectorCapabilities.ReadMetadata, ConnectorCapabilities.DryRun, ConnectorCapabilities.Resume, ConnectorCapabilities.Preflight },
        Credentials =
        {
            Credential("BaseUrl", "Base URL", "WebDam:BaseUrl", true, false),
            Credential("ClientId", "Client ID", "WebDam:ClientId", true),
            Credential("ClientSecret", "Client Secret", "WebDam:ClientSecret", true),
            Credential("RefreshToken", "Refresh Token", "WebDam:RefreshToken", false),
            Credential("AccessToken", "Access Token", "WebDam:AccessToken", false),
            Credential("Username", "Username", "WebDam:Username", false),
            Credential("Password", "Password", "WebDam:Password", false)
        },
        Options =
        {
            Option("SourceBinaryMode", "Source binary mode", false, "PreferManifestPath"),
            Option("BinaryStagingDirectory", "Binary staging directory"),
            Option("ForceDownload", "Force download", false, "false")
        },
        ManifestColumns = { "webdam_id", "webdamId", "WebDamId", "SourceAssetId", "AssetId", "asset_id", "Id", "id", "SourcePath", "sourcePath", "FilePath", "filePath", "DownloadUrl", "downloadUrl", "SourceUri", "sourceUri", "Url", "url" },
        Metadata = { ["SupportedBinaryModes"] = "PreferManifestPath,WebDamDownloadOnly,ManifestPathOnly,StagedOnly" }
    };

    private static ConnectorDescriptor CreateAzureBlobSourceDescriptor() => new()
    {
        Type = "AzureBlob",
        DisplayName = "Azure Blob Storage",
        Direction = ConnectorDirections.Source,
        Kind = "Source",
        Description = "Reads staged binaries from Azure Blob Storage. Manifest rows can provide BlobName, SourcePath, SourceUri, or Url.",
        Capabilities = { ConnectorCapabilities.ReadAsset, ConnectorCapabilities.ReadBinary, ConnectorCapabilities.ReadMetadata, ConnectorCapabilities.ReadFolderPath, ConnectorCapabilities.DryRun, ConnectorCapabilities.Resume, ConnectorCapabilities.Preflight },
        Credentials =
        {
            Credential("ConnectionString", "Connection string", "AzureBlobSource:ConnectionString", true),
            Credential("ContainerName", "Container name", "AzureBlobSource:ContainerName", true, false),
            Credential("AccountName", "Account name", "AzureBlobSource:AccountName", false, false),
            Credential("SasToken", "SAS token", "AzureBlobSource:SasToken", false)
        },
        Options =
        {
            Option("AzureBlobSourceRootPrefix", "Root prefix"),
            Option("AzureBlobSourceBlobNameField", "Blob name field", false, "BlobName"),
            Option("AzureBlobSourceFileNameField", "File name field", false, "FileName")
        },
        ManifestColumns = { "BlobName", "blobName", "SourcePath", "sourcePath", "SourceUri", "sourceUri", "Url", "url", "FileName", "filename", "ContentType", "contentType", "Length", "length" },
        Metadata = { ["BinaryResolution"] = "Use SourceUri/Url when supplied; otherwise use BlobName/SourcePath with bound source credentials." }
    };

    private static ConnectorDescriptor CreateS3SourceDescriptor() => new()
    {
        Type = "S3",
        DisplayName = "Amazon S3",
        Direction = ConnectorDirections.Source,
        Kind = "Source",
        Description = "Reads staged binaries from Amazon S3 or S3-compatible storage. Manifest rows can provide S3Key, SourcePath, SourceUri, or Url.",
        Capabilities = { ConnectorCapabilities.ReadAsset, ConnectorCapabilities.ReadBinary, ConnectorCapabilities.ReadMetadata, ConnectorCapabilities.ReadFolderPath, ConnectorCapabilities.DryRun, ConnectorCapabilities.Resume, ConnectorCapabilities.Preflight },
        Credentials =
        {
            Credential("AccessKeyId", "Access key ID", "S3:AccessKeyId", true),
            Credential("SecretAccessKey", "Secret access key", "S3:SecretAccessKey", true),
            Credential("Region", "Region", "S3:Region", true, false),
            Credential("BucketName", "Bucket name", "S3:BucketName", true, false),
            Credential("ServiceUrl", "S3-compatible service URL", "S3:ServiceUrl", false, false),
            Credential("SessionToken", "Session token", "S3:SessionToken", false)
        },
        Options =
        {
            Option("S3SourceRootPrefix", "Root prefix"),
            Option("S3SourceKeyField", "S3 key field", false, "S3Key"),
            Option("S3SourceFileNameField", "File name field", false, "FileName")
        },
        ManifestColumns = { "S3Key", "s3Key", "Key", "key", "SourcePath", "sourcePath", "SourceUri", "sourceUri", "Url", "url", "FileName", "filename", "ContentType", "contentType", "Length", "length" },
        Metadata = { ["BinaryResolution"] = "Use SourceUri/Url when supplied; otherwise use S3Key/SourcePath with bound source credentials." }
    };

    private static ConnectorDescriptor CreateSitecoreSourceDescriptor() => new()
    {
        Type = "Sitecore",
        DisplayName = "Sitecore",
        Direction = ConnectorDirections.Source,
        Kind = "Source",
        Description = "Reads asset references and metadata exported from Sitecore. Manifest rows should include an item/media id and either a source URL or staged file path.",
        Capabilities = { ConnectorCapabilities.ReadAsset, ConnectorCapabilities.ReadMetadata, ConnectorCapabilities.DryRun, ConnectorCapabilities.Resume, ConnectorCapabilities.Preflight },
        Credentials =
        {
            Credential("BaseUrl", "Base URL", "Sitecore:BaseUrl", true, false),
            Credential("ClientId", "Client ID", "Sitecore:ClientId", false),
            Credential("ClientSecret", "Client Secret", "Sitecore:ClientSecret", false),
            Credential("Username", "Username", "Sitecore:Username", false),
            Credential("Password", "Password", "Sitecore:Password", false),
            Credential("ApiKey", "API key", "Sitecore:ApiKey", false)
        },
        Options =
        {
            Option("SitecoreIdField", "Sitecore id field", false, "SitecoreId"),
            Option("SitecorePathField", "Sitecore path field", false, "SitecorePath")
        },
        ManifestColumns = { "SitecoreId", "ItemId", "MediaId", "SourceAssetId", "SitecorePath", "SourcePath", "SourceUri", "Url", "FileName" },
        Metadata = { ["Status"] = "Credential schema and manifest-driven source envelope are enabled. Native Sitecore API download can be implemented behind the SitecoreSourceConnector." }
    };

    private static ConnectorDescriptor CreateAemSourceDescriptor() => new()
    {
        Type = "Aem",
        DisplayName = "Adobe Experience Manager",
        Direction = ConnectorDirections.Source,
        Kind = "Source",
        Description = "Reads asset references and metadata exported from AEM. Manifest rows should include an AEM path/id and either a source URL or staged file path.",
        Capabilities = { ConnectorCapabilities.ReadAsset, ConnectorCapabilities.ReadMetadata, ConnectorCapabilities.DryRun, ConnectorCapabilities.Resume, ConnectorCapabilities.Preflight },
        Credentials =
        {
            Credential("BaseUrl", "Base URL", "Aem:BaseUrl", true, false),
            Credential("Username", "Username", "Aem:Username", false),
            Credential("Password", "Password", "Aem:Password", false),
            Credential("ClientId", "Client ID", "Aem:ClientId", false),
            Credential("ClientSecret", "Client Secret", "Aem:ClientSecret", false),
            Credential("AccessToken", "Access token", "Aem:AccessToken", false)
        },
        Options =
        {
            Option("AemAssetPathField", "AEM asset path field", false, "AemPath"),
            Option("AemAssetIdField", "AEM asset id field", false, "AemId")
        },
        ManifestColumns = { "AemId", "AemPath", "AssetPath", "SourceAssetId", "SourcePath", "SourceUri", "Url", "FileName" },
        Metadata = { ["Status"] = "Credential schema and manifest-driven source envelope are enabled. Native AEM API download can be implemented behind the AemSourceConnector." }
    };

    private static ConnectorDescriptor CreateBynderTargetDescriptor() => new()
    {
        Type = "Bynder",
        DisplayName = "Bynder",
        Direction = ConnectorDirections.Target,
        Kind = "Target",
        Description = "Uploads binaries and stamps tags/metaproperties into Bynder.",
        Capabilities = { ConnectorCapabilities.UpsertAsset, ConnectorCapabilities.WriteMetadata, ConnectorCapabilities.DryRun, ConnectorCapabilities.Resume, ConnectorCapabilities.Preflight },
        Credentials =
        {
            Credential("BaseUrl", "Base URL", "Bynder:Client:BaseUrl", true, false),
            Credential("ClientId", "Client ID", "Bynder:Client:ClientId", true),
            Credential("ClientSecret", "Client Secret", "Bynder:Client:ClientSecret", true),
            Credential("Scopes", "Scopes", "Bynder:Client:Scopes", true, false),
            Credential("BrandStoreId", "Brand Store ID", "Bynder:BrandStoreId", false, false)
        },
        Options = { Option("ValidateMetaproperties", "Validate metaproperties", false, "true") },
        MappingFields = { "name", "description", "tags", "keywords", "meta:", "" },
        Metadata = { ["MetapropertyConvention"] = "Mapping target names should match Bynder metaproperty display names, or use meta:." }
    };

    private static ConnectorDescriptor CreateAzureBlobTargetDescriptor() => new()
    {
        Type = "AzureBlob",
        DisplayName = "Azure Blob Storage",
        Direction = ConnectorDirections.Target,
        Kind = "Target",
        Description = "Writes DAM binaries to Azure Blob Storage, preserving source folder paths and optional JSON metadata sidecars.",
        Capabilities = { ConnectorCapabilities.UpsertAsset, ConnectorCapabilities.WriteBinary, ConnectorCapabilities.WriteMetadata, ConnectorCapabilities.WriteFolderPath, ConnectorCapabilities.WriteSidecarMetadata, ConnectorCapabilities.DryRun, ConnectorCapabilities.Resume, ConnectorCapabilities.Preflight },
        Credentials =
        {
            Credential("ConnectionString", "Connection string", "AzureBlobTarget:ConnectionString", true),
            Credential("ContainerName", "Container name", "AzureBlobTarget:ContainerName", true, false)
        },
        Options =
        {
            Option("RootFolderPath", "Root folder path"),
            Option("PreserveSourceFolderPath", "Preserve source folder path", false, "true", "true", "false"),
            Option("SourceFolderPathField", "Source folder path field", false, "Folder Path"),
            Option("UniqueIdField", "Unique ID field", false, "webdam_id"),
            Option("FileNameField", "File name field", false, "File Name"),
            Option("BinaryFileNameTemplate", "Binary filename template", false, "{uniqueid}_{filename}"),
            Option("WriteMetadataSidecar", "Write metadata sidecar", false, "true", "true", "false"),
            Option("MetadataFileNameTemplate", "Metadata filename template", false, "{uniqueid}_metadata.json"),
            Option("Overwrite", "Overwrite existing blobs", false, "false", "true", "false")
        },
        MappingFields = { "FileName", "AssetName", "FolderPath", "" }
    };

    private static ConnectorDescriptor CreateCloudinaryTargetDescriptor() => new()
    {
        Type = "Cloudinary",
        DisplayName = "Cloudinary",
        Direction = ConnectorDirections.Target,
        Kind = "Target",
        Description = "Uploads assets into Cloudinary with support for public id, asset folder, tags, context, and structured metadata.",
        Capabilities = { ConnectorCapabilities.UpsertAsset, ConnectorCapabilities.WriteBinary, ConnectorCapabilities.WriteMetadata, ConnectorCapabilities.ValidateMetadataSchema, ConnectorCapabilities.DryRun, ConnectorCapabilities.Resume, ConnectorCapabilities.Preflight },
        Credentials =
        {
            Credential("CloudName", "Cloud name", "Cloudinary:CloudName", true, false),
            Credential("ApiKey", "API key", "Cloudinary:ApiKey", true),
            Credential("ApiSecret", "API secret", "Cloudinary:ApiSecret", true),
            Credential("UploadPreset", "Upload preset", "Cloudinary:UploadPreset", false, false)
        },
        Options =
        {
            Option("CloudinaryFolderField", "Asset folder field", false, "asset_folder"),
            Option("CloudinaryPublicIdField", "Public ID field", false, "public_id"),
            Option("CloudinaryResourceType", "Resource type", false, "auto", "auto", "image", "video", "raw"),
            Option("Overwrite", "Overwrite existing assets", false, "true", "true", "false"),
            Option("Invalidate", "Invalidate CDN cache", false, "false", "true", "false")
        },
        MappingFields = { "public_id", "asset_folder", "folder", "resource_type", "type", "upload_preset", "tags", "context", "metadata", "file", "overwrite", "invalidate" },
        Metadata = { ["Credentials"] = "CloudName, ApiKey, ApiSecret" }
    };

    private static ConnectorDescriptor CreateAprimoTargetDescriptor() => new()
    {
        Type = "Aprimo",
        DisplayName = "Aprimo",
        Direction = ConnectorDirections.Target,
        Kind = "Target",
        Description = "Target connector shell for Aprimo asset creation and metadata writes.",
        Capabilities = { ConnectorCapabilities.UpsertAsset, ConnectorCapabilities.WriteBinary, ConnectorCapabilities.WriteMetadata, ConnectorCapabilities.DryRun, ConnectorCapabilities.Preflight },
        Credentials =
        {
            Credential("BaseUrl", "Base URL", "Aprimo:BaseUrl", true, false),
            Credential("Tenant", "Tenant", "Aprimo:Tenant", false, false),
            Credential("ClientId", "Client ID", "Aprimo:ClientId", true),
            Credential("ClientSecret", "Client Secret", "Aprimo:ClientSecret", true),
            Credential("ApiKey", "API key", "Aprimo:ApiKey", false)
        },
        Options =
        {
            Option("AprimoClassificationId", "Classification ID"),
            Option("AprimoRecordStatus", "Record status", false, "Draft"),
            Option("AprimoFileNameField", "File name field", false, "FileName")
        },
        MappingFields = { "title", "description", "classificationId", "recordStatus", "fields", "file", "filename" },
        Metadata = { ["Status"] = "Registered credential-aware target connector. Native upload implementation can now be completed behind AprimoTargetConnector." }
    };

    private static ConnectorDescriptor CreateLocalStorageSourceDescriptor() => new()
    {
        Type = "LocalStorage",
        DisplayName = "Local Storage",
        Direction = ConnectorDirections.Source,
        Kind = "Source",
        Description = "Reads binaries from local or network file-system paths supplied by the manifest.",
        Capabilities = { ConnectorCapabilities.ReadAsset, ConnectorCapabilities.ReadBinary, ConnectorCapabilities.DryRun, ConnectorCapabilities.Resume, ConnectorCapabilities.Preflight },
        Options =
        {
            Option("LocalStorageSourceRootDirectory", "Source root directory"),
            Option("RequireExistingFile", "Require existing file", false, "true")
        },
        ManifestColumns = { "SourceAssetId", "sourceAssetId", "AssetId", "assetId", "id", "Id", "SourcePath", "sourcePath", "FilePath", "filePath", "Path", "path", "LocalPath", "localPath", "SourceUri", "sourceUri", "FileName", "fileName", "filename", "OriginalFileName", "originalFileName" }
    };

    private static ConnectorDescriptor CreateLocalStorageTargetDescriptor() => new()
    {
        Type = "LocalStorage",
        DisplayName = "Local Storage",
        Direction = ConnectorDirections.Target,
        Kind = "Target",
        Description = "Copies binaries to a local or network file-system folder and can write DAM-style metadata sidecars.",
        Capabilities = { ConnectorCapabilities.UpsertAsset, ConnectorCapabilities.WriteBinary, ConnectorCapabilities.WriteMetadata, ConnectorCapabilities.DryRun, ConnectorCapabilities.Resume, ConnectorCapabilities.Preflight },
        Options =
        {
            Option("LocalStorageTargetRootDirectory", "Target root directory", true),
            Option("LocalStorageTargetBasePath", "Target base path"),
            Option("PreserveSourceFolderPath", "Preserve source folder path", false, "true"),
            Option("PrefixFileNameWithUniqueId", "Prefix binary with unique id", false, "true"),
            Option("UniqueIdField", "Unique id field", false, "SourceAssetId"),
            Option("WriteMetadataSidecar", "Write metadata sidecar", false, "true"),
            Option("Overwrite", "Overwrite existing files", false, "false")
        },
        MappingFields = { "name", "description", "tags", "keywords", "metadata:", "" }
    };

    private static CredentialDescriptor Credential(string name, string displayName, string configurationKey, bool required, bool secret = true) => new()
    {
        Name = name,
        DisplayName = displayName,
        ConfigurationKey = configurationKey,
        Required = required,
        Secret = secret
    };

    private static ConnectorOptionDescriptor Option(string name, string displayName, bool required = false, string? defaultValue = null, params string[] allowedValues) => new()
    {
        Name = name,
        DisplayName = displayName,
        Required = required,
        DefaultValue = defaultValue,
        AllowedValues = allowedValues.ToList()
    };
}
