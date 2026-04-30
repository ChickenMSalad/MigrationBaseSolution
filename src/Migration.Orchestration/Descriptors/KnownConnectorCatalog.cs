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
            CreateLocalStorageSourceDescriptor()
        };

        _targets = new[]
        {
            CreateBynderTargetDescriptor(),
            CreateAzureBlobTargetDescriptor(),
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
                    new ConnectorOptionDescriptor
                    {
                        Name = "ManifestPath",
                        DisplayName = "Manifest path",
                        Required = true,
                        Description = "Path to the CSV manifest file."
                    }
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
                    new ConnectorOptionDescriptor
                    {
                        Name = "ManifestPath",
                        DisplayName = "Manifest path",
                        Required = true,
                        Description = "Path to the Excel manifest file."
                    },
                    new ConnectorOptionDescriptor
                    {
                        Name = "WorksheetName",
                        DisplayName = "Worksheet name",
                        Required = false,
                        Description = "Optional worksheet name. If omitted, the first worksheet is used."
                    },
                    new ConnectorOptionDescriptor
                    {
                        Name = "HeaderRow",
                        DisplayName = "Header row",
                        Required = false,
                        DefaultValue = "1"
                    },
                    new ConnectorOptionDescriptor
                    {
                        Name = "FirstDataRow",
                        DisplayName = "First data row",
                        Required = false,
                        DefaultValue = "2"
                    }
                }
            }
        };
    }

    public IReadOnlyList<ConnectorDescriptor> GetSources()
    {
        return _sources;
    }

    public IReadOnlyList<ConnectorDescriptor> GetTargets()
    {
        return _targets;
    }

    public IReadOnlyList<ManifestProviderDescriptor> GetManifestProviders()
    {
        return _manifestProviders;
    }

    public IReadOnlyList<ConnectorDescriptor> GetAll()
    {
        return _sources.Concat(_targets).ToList();
    }

    public ConnectorDescriptor? Find(string type)
    {
        return GetAll().FirstOrDefault(x =>
            x.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    private static ConnectorDescriptor CreateWebDamSourceDescriptor()
    {
        return new ConnectorDescriptor
        {
            Type = "WebDam",
            DisplayName = "WebDam",
            Direction = ConnectorDirections.Source,
            Kind = "Source",
            Description = "Reads metadata and binaries from WebDam using either a WebDam asset id or a pre-staged binary path.",
            Capabilities =
            {
                ConnectorCapabilities.ReadAsset,
                ConnectorCapabilities.ReadBinary,
                ConnectorCapabilities.DryRun,
                ConnectorCapabilities.Resume,
                ConnectorCapabilities.Preflight
            },
            Credentials =
            {
                new CredentialDescriptor { Name = "BaseUrl", DisplayName = "Base URL", ConfigurationKey = "WebDam:BaseUrl", Required = true, Secret = false },
                new CredentialDescriptor { Name = "ClientId", DisplayName = "Client ID", ConfigurationKey = "WebDam:ClientId", Required = true, Secret = true },
                new CredentialDescriptor { Name = "ClientSecret", DisplayName = "Client Secret", ConfigurationKey = "WebDam:ClientSecret", Required = true, Secret = true },
                new CredentialDescriptor { Name = "RefreshToken", DisplayName = "Refresh Token", ConfigurationKey = "WebDam:RefreshToken", Required = true, Secret = true },
                new CredentialDescriptor { Name = "AccessToken", DisplayName = "Access Token", ConfigurationKey = "WebDam:AccessToken", Required = false, Secret = true },
                new CredentialDescriptor { Name = "Username", DisplayName = "Username", ConfigurationKey = "WebDam:Username", Required = false, Secret = true },
                new CredentialDescriptor { Name = "Password", DisplayName = "Password", ConfigurationKey = "WebDam:Password", Required = false, Secret = true }
            },
            Options =
            {
                new ConnectorOptionDescriptor { Name = "SourceBinaryMode", DisplayName = "Source binary mode", Required = false, DefaultValue = "PreferManifestPath" },
                new ConnectorOptionDescriptor { Name = "BinaryStagingDirectory", DisplayName = "Binary staging directory", Required = false },
                new ConnectorOptionDescriptor { Name = "ForceDownload", DisplayName = "Force download", Required = false, DefaultValue = "false" },
                new ConnectorOptionDescriptor { Name = "WorksheetName", DisplayName = "Excel worksheet name", Required = false },
                new ConnectorOptionDescriptor { Name = "HeaderRow", DisplayName = "Header row", Required = false, DefaultValue = "1" },
                new ConnectorOptionDescriptor { Name = "FirstDataRow", DisplayName = "First data row", Required = false, DefaultValue = "2" }
            },
            ManifestColumns =
            {
                "webdam_id",
                "webdamId",
                "WebDamId",
                "SourceAssetId",
                "AssetId",
                "asset_id",
                "Id",
                "id",
                "SourcePath",
                "sourcePath",
                "FilePath",
                "filePath",
                "DownloadUrl",
                "downloadUrl",
                "SourceUri",
                "sourceUri",
                "Url",
                "url"
            },
            Metadata =
            {
                ["SupportedBinaryModes"] = "PreferManifestPath,WebDamDownloadOnly,ManifestPathOnly,StagedOnly",
                ["PreferredProofJob"] = "Profiles/Jobs/webdam-to-bynder.ntara.json"
            }
        };
    }

    private static ConnectorDescriptor CreateBynderTargetDescriptor()
    {
        return new ConnectorDescriptor
        {
            Type = "Bynder",
            DisplayName = "Bynder",
            Direction = ConnectorDirections.Target,
            Kind = "Target",
            Description = "Uploads binaries and stamps tags/metaproperties into Bynder.",
            Capabilities =
            {
                ConnectorCapabilities.UpsertAsset,
                ConnectorCapabilities.WriteMetadata,
                ConnectorCapabilities.DryRun,
                ConnectorCapabilities.Resume,
                ConnectorCapabilities.Preflight
            },
            Credentials =
            {
                new CredentialDescriptor { Name = "BaseUrl", DisplayName = "Base URL", ConfigurationKey = "Bynder:Client:BaseUrl", Required = true, Secret = false },
                new CredentialDescriptor { Name = "ConsumerKey", DisplayName = "Consumer Key", ConfigurationKey = "Bynder:Client:ConsumerKey", Required = true, Secret = true },
                new CredentialDescriptor { Name = "ConsumerSecret", DisplayName = "Consumer Secret", ConfigurationKey = "Bynder:Client:ConsumerSecret", Required = true, Secret = true },
                new CredentialDescriptor { Name = "Token", DisplayName = "Token", ConfigurationKey = "Bynder:Client:Token", Required = true, Secret = true },
                new CredentialDescriptor { Name = "TokenSecret", DisplayName = "Token Secret", ConfigurationKey = "Bynder:Client:TokenSecret", Required = true, Secret = true }
            },
            Options =
            {
                new ConnectorOptionDescriptor { Name = "BrandStoreId", DisplayName = "Brand Store ID", Required = true },
                new ConnectorOptionDescriptor { Name = "ValidateMetaproperties", DisplayName = "Validate metaproperties", Required = false, DefaultValue = "true" }
            },
            MappingFields =
            {
                "name",
                "description",
                "tags",
                "keywords",
                "meta:<Bynder metaproperty display name>",
                "<Bynder metaproperty display name>"
            },
            Metadata =
            {
                ["ReservedFields"] = "id,mediaId,assetId,bynderId,name,filename,fileName,originalFileName,description,tags,keywords,sourceUri,downloadUrl,url,filePath,path",
                ["MetapropertyConvention"] = "Mapping target names should match Bynder metaproperty display names, or use meta:<display name>."
            }
        };
    }

    private static ConnectorDescriptor CreateAzureBlobTargetDescriptor()
    {
        return new ConnectorDescriptor
        {
            Type = "AzureBlob",
            DisplayName = "Azure Blob Storage",
            Direction = ConnectorDirections.Target,
            Kind = "Target",
            Description = "Writes DAM binaries to Azure Blob Storage, preserving source folder paths and optional JSON metadata sidecars.",
            Capabilities =
            {
                ConnectorCapabilities.UpsertAsset,
                ConnectorCapabilities.WriteMetadata,
                ConnectorCapabilities.DryRun,
                ConnectorCapabilities.Resume,
                ConnectorCapabilities.Preflight
            },
            Credentials =
            {
                new CredentialDescriptor { Name = "ConnectionString", DisplayName = "Connection string", ConfigurationKey = "AzureBlobTarget:ConnectionString", Required = true, Secret = true },
                new CredentialDescriptor { Name = "ContainerName", DisplayName = "Container name", ConfigurationKey = "AzureBlobTarget:ContainerName", Required = true, Secret = false }
            },
            Options =
            {
                new ConnectorOptionDescriptor { Name = "RootFolderPath", DisplayName = "Root folder path", Required = false, Description = "Optional prefix before the preserved DAM folder path, such as webdam-export/ntara." },
                new ConnectorOptionDescriptor { Name = "PreserveSourceFolderPath", DisplayName = "Preserve source folder path", Required = false, DefaultValue = "true", AllowedValues = { "true", "false" } },
                new ConnectorOptionDescriptor { Name = "SourceFolderPathField", DisplayName = "Source folder path field", Required = false, DefaultValue = "Folder Path" },
                new ConnectorOptionDescriptor { Name = "UniqueIdField", DisplayName = "Unique ID field", Required = false, DefaultValue = "webdam_id" },
                new ConnectorOptionDescriptor { Name = "FileNameField", DisplayName = "File name field", Required = false, DefaultValue = "File Name" },
                new ConnectorOptionDescriptor { Name = "BinaryFileNameTemplate", DisplayName = "Binary filename template", Required = false, DefaultValue = "{uniqueid}_{filename}" },
                new ConnectorOptionDescriptor { Name = "WriteMetadataSidecar", DisplayName = "Write metadata sidecar", Required = false, DefaultValue = "true", AllowedValues = { "true", "false" } },
                new ConnectorOptionDescriptor { Name = "MetadataFileNameTemplate", DisplayName = "Metadata filename template", Required = false, DefaultValue = "{uniqueid}_metadata.json" },
                new ConnectorOptionDescriptor { Name = "MetadataSidecarMode", DisplayName = "Metadata sidecar mode", Required = false, DefaultValue = "All", AllowedValues = { "All", "MappedOnly", "ManifestOnly", "SourceEnvelopeOnly", "None" } },
                new ConnectorOptionDescriptor { Name = "MetadataIncludeColumns", DisplayName = "Metadata include columns", Required = false },
                new ConnectorOptionDescriptor { Name = "MetadataExcludeColumns", DisplayName = "Metadata exclude columns", Required = false },
                new ConnectorOptionDescriptor { Name = "Overwrite", DisplayName = "Overwrite existing blobs", Required = false, DefaultValue = "false", AllowedValues = { "true", "false" } }
            },
            MappingFields =
            {
                "FileName",
                "AssetName",
                "FolderPath",
                "<metadata field name>"
            },
            Metadata =
            {
                ["DefaultBinaryName"] = "{uniqueid}_{filename}",
                ["DefaultMetadataName"] = "{uniqueid}_metadata.json",
                ["DefaultLayout"] = "{RootFolderPath}/{Folder Path}/{webdam_id}_{File Name}; {RootFolderPath}/{Folder Path}/{webdam_id}_metadata.json"
            }
        };
    }

    private static ConnectorDescriptor CreateLocalStorageSourceDescriptor()
    {
        return new ConnectorDescriptor
        {
            Type = "LocalStorage",
            DisplayName = "Local Storage",
            Direction = ConnectorDirections.Source,
            Kind = "Source",
            Description = "Reads binaries from local or network file-system paths supplied by the manifest.",
            Capabilities =
        {
            ConnectorCapabilities.ReadAsset,
            ConnectorCapabilities.ReadBinary,
            ConnectorCapabilities.DryRun,
            ConnectorCapabilities.Resume,
            ConnectorCapabilities.Preflight
        },
            Options =
        {
            new ConnectorOptionDescriptor { Name = "LocalStorageSourceRootDirectory", DisplayName = "Source root directory", Required = false },
            new ConnectorOptionDescriptor { Name = "RequireExistingFile", DisplayName = "Require existing file", Required = false, DefaultValue = "true" }
        },
            ManifestColumns =
        {
            "SourceAssetId", "sourceAssetId", "AssetId", "assetId", "id", "Id",
            "SourcePath", "sourcePath", "FilePath", "filePath", "Path", "path",
            "LocalPath", "localPath", "SourceUri", "sourceUri",
            "FileName", "fileName", "filename", "OriginalFileName", "originalFileName"
        },
            Metadata =
        {
            ["PathBehavior"] = "Relative paths are resolved under LocalStorage:Source:RootDirectory or job setting LocalStorageSourceRootDirectory. Absolute paths are used as-is."
        }
        };
    }


    private static ConnectorDescriptor CreateLocalStorageTargetDescriptor()
    {
        return new ConnectorDescriptor
        {
            Type = "LocalStorage",
            DisplayName = "Local Storage",
            Direction = ConnectorDirections.Target,
            Kind = "Target",
            Description = "Copies binaries to a local or network file-system folder and can write DAM-style metadata sidecars.",
            Capabilities =
        {
            ConnectorCapabilities.UpsertAsset,
            ConnectorCapabilities.WriteBinary,
            ConnectorCapabilities.WriteMetadata,
            ConnectorCapabilities.DryRun,
            ConnectorCapabilities.Resume,
            ConnectorCapabilities.Preflight
        },
            Options =
        {
            new ConnectorOptionDescriptor { Name = "LocalStorageTargetRootDirectory", DisplayName = "Target root directory", Required = true },
            new ConnectorOptionDescriptor { Name = "LocalStorageTargetBasePath", DisplayName = "Target base path", Required = false },
            new ConnectorOptionDescriptor { Name = "PreserveSourceFolderPath", DisplayName = "Preserve source folder path", Required = false, DefaultValue = "true" },
            new ConnectorOptionDescriptor { Name = "PrefixFileNameWithUniqueId", DisplayName = "Prefix binary with unique id", Required = false, DefaultValue = "true" },
            new ConnectorOptionDescriptor { Name = "UniqueIdField", DisplayName = "Unique id field", Required = false, DefaultValue = "SourceAssetId" },
            new ConnectorOptionDescriptor { Name = "WriteMetadataSidecar", DisplayName = "Write metadata sidecar", Required = false, DefaultValue = "true" },
            new ConnectorOptionDescriptor { Name = "MetadataSidecarMode", DisplayName = "Metadata sidecar mode", Required = false, DefaultValue = "Both", AllowedValues = { "ManifestColumns", "TargetPayloadFields", "Both" } },
            new ConnectorOptionDescriptor { Name = "Overwrite", DisplayName = "Overwrite existing files", Required = false, DefaultValue = "false" }
        },
            MappingFields =
        {
            "name",
            "description",
            "tags",
            "keywords",
            "metadata:<field>",
            "<sidecar field name>"
        },
            Metadata =
        {
            ["BinaryNaming"] = "When PrefixFileNameWithUniqueId is true, binaries are written as {uniqueid}_{filename}.",
            ["SidecarNaming"] = "When WriteMetadataSidecar is true, metadata is written as {uniqueid}_metadata.json next to the binary."
        }
        };
    }

}
