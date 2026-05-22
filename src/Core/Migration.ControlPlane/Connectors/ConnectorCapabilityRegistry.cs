namespace Migration.ControlPlane.Connectors;

/// <summary>
/// Static cloud-readiness metadata for currently supported connectors.
/// These descriptors enrich the existing connector catalog without changing
/// connector runtime behavior.
/// </summary>
public static class ConnectorCapabilityRegistry
{
    public static ConnectorCapabilityEnrichment Get(string role, string key)
    {
        var normalizedRole = NormalizeRole(role);
        var normalizedKey = ConnectorDescriptorAliases.Normalize(key);

        return normalizedRole switch
        {
            ConnectorCapabilityRoles.Source => GetSource(normalizedKey),
            ConnectorCapabilityRoles.Target => GetTarget(normalizedKey),
            ConnectorCapabilityRoles.ManifestProvider => GetManifestProvider(normalizedKey),
            _ => ConnectorCapabilityEnrichment.Empty
        };
    }

    private static ConnectorCapabilityEnrichment GetSource(string key) =>
        key switch
        {
            "aem" => new(
                Description: "Exports asset references and metadata from Adobe Experience Manager folders.",
                ConfigurationFields:
                [
                    Url("baseUrl", "AEM Base URL", required: true, "AEM author or publish endpoint."),
                    MultiText("folders", "Folders", required: true, "One or more DAM folder paths to export."),
                    Boolean("recursive", "Recursive", required: false, "true", "Include child folders."),
                    Number("pageSize", "Page Size", required: false, "100", "AEM query page size.")
                ],
                CredentialRequirements:
                [
                    Secret("username", "Username", ConnectorSecretKinds.Username, required: true),
                    Secret("password", "Password", ConnectorSecretKinds.Password, required: true),
                    Secret("bearerToken", "Bearer Token", ConnectorSecretKinds.BearerToken, required: false, "Optional token for environments that do not use username/password.")
                ],
                SupportedOperations: ["discover", "manifest", "validate", "read"],
                SupportsManifestGeneration: true,
                SupportsValidation: true,
                SupportsDryRun: true),

            "contenthub" => new(
                Description: "Exports assets from Sitecore Content Hub using taxonomy filters or all assets.",
                ConfigurationFields:
                [
                    Url("endpoint", "Content Hub Endpoint", required: true, "Content Hub API endpoint."),
                    MultiText("taxonomyIds", "Taxonomy IDs", required: true, "Use '*' or 'all' to export all assets."),
                    Text("culture", "Culture", required: false, "Optional culture/language value.")
                ],
                CredentialRequirements:
                [
                    Secret("clientId", "Client ID", ConnectorSecretKinds.OAuthClientId, required: true),
                    Secret("clientSecret", "Client Secret", ConnectorSecretKinds.OAuthClientSecret, required: true),
                    Secret("bearerToken", "Bearer Token", ConnectorSecretKinds.BearerToken, required: false, "Optional pre-issued bearer token.")
                ],
                SupportedOperations: ["discover", "manifest", "validate", "read"],
                SupportsManifestGeneration: true,
                SupportsValidation: true,
                SupportsDryRun: true),

            "bynder" => new(
                Description: "Exports Bynder asset metadata and asset references.",
                ConfigurationFields:
                [
                    Url("baseUrl", "Bynder Base URL", required: true, "Bynder portal/API base URL."),
                    Text("query", "Query", required: false, "Optional Bynder query/filter."),
                    Boolean("includeArchived", "Include Archived", required: false, "false")
                ],
                CredentialRequirements:
                [
                    Secret("apiKey", "API Key", ConnectorSecretKinds.ApiKey, required: false),
                    Secret("bearerToken", "Bearer Token", ConnectorSecretKinds.BearerToken, required: false),
                    Secret("clientId", "OAuth Client ID", ConnectorSecretKinds.OAuthClientId, required: false),
                    Secret("clientSecret", "OAuth Client Secret", ConnectorSecretKinds.OAuthClientSecret, required: false)
                ],
                SupportedOperations: ["discover", "manifest", "validate", "read"],
                SupportsManifestGeneration: true,
                SupportsValidation: true,
                SupportsDryRun: true),

            "webdam" => new(
                Description: "Exports WebDam/Widen asset metadata and asset references.",
                ConfigurationFields:
                [
                    Url("baseUrl", "WebDam Base URL", required: true),
                    Text("query", "Query", required: false),
                    Text("folderId", "Folder ID", required: false)
                ],
                CredentialRequirements:
                [
                    Secret("apiKey", "API Key", ConnectorSecretKinds.ApiKey, required: false),
                    Secret("bearerToken", "Bearer Token", ConnectorSecretKinds.BearerToken, required: false)
                ],
                SupportedOperations: ["discover", "manifest", "validate", "read"],
                SupportsManifestGeneration: true,
                SupportsValidation: true,
                SupportsDryRun: true),

            "sharepoint" => new(
                Description: "Exports assets from SharePoint using Graph or rclone-backed source modes.",
                ConfigurationFields:
                [
                    Url("siteUrl", "Site URL", required: true),
                    Text("driveId", "Drive ID", required: false),
                    Path("path", "Path", required: false),
                    Select("mode", "Mode", required: false, "graph", ["graph", "rclone"])
                ],
                CredentialRequirements:
                [
                    Secret("tenantId", "Tenant ID", ConnectorSecretKinds.OAuthClientId, required: false),
                    Secret("clientId", "Client ID", ConnectorSecretKinds.OAuthClientId, required: true),
                    Secret("clientSecret", "Client Secret", ConnectorSecretKinds.OAuthClientSecret, required: true)
                ],
                SupportedOperations: ["discover", "manifest", "validate", "read"],
                SupportsManifestGeneration: true,
                SupportsValidation: true,
                SupportsDryRun: true),

            "s3" => new(
                Description: "Reads objects from an Amazon S3 bucket.",
                ConfigurationFields:
                [
                    Text("bucket", "Bucket", required: true),
                    Text("prefix", "Prefix", required: false),
                    Text("region", "Region", required: false)
                ],
                CredentialRequirements:
                [
                    Secret("accessKeyId", "Access Key ID", ConnectorSecretKinds.AccessKeyId, required: true),
                    Secret("secretAccessKey", "Secret Access Key", ConnectorSecretKinds.SecretAccessKey, required: true)
                ],
                SupportedOperations: ["discover", "manifest", "validate", "read"],
                SupportsManifestGeneration: true,
                SupportsValidation: true,
                SupportsDryRun: true),

            "azureblob" => new(
                Description: "Reads blobs from Azure Blob Storage.",
                ConfigurationFields:
                [
                    Text("container", "Container", required: true),
                    Text("prefix", "Prefix", required: false),
                    Text("accountName", "Storage Account", required: false)
                ],
                CredentialRequirements:
                [
                    Secret("connectionString", "Connection String", ConnectorSecretKinds.ConnectionString, required: false),
                    Secret("accountKey", "Account Key", ConnectorSecretKinds.ApiSecret, required: false)
                ],
                SupportedOperations: ["discover", "manifest", "validate", "read"],
                SupportsManifestGeneration: true,
                SupportsValidation: true,
                SupportsDryRun: true),

            "localstorage" => new(
                Description: "Reads files from a local filesystem path.",
                ConfigurationFields:
                [
                    Path("rootPath", "Root Path", required: true),
                    Path("relativePath", "Relative Path", required: false),
                    Boolean("recursive", "Recursive", required: false, "true")
                ],
                CredentialRequirements: [],
                SupportedOperations: ["discover", "manifest", "validate", "read"],
                SupportsManifestGeneration: true,
                SupportsValidation: true,
                SupportsDryRun: true),

            _ => ConnectorCapabilityEnrichment.Empty
        };

    private static ConnectorCapabilityEnrichment GetTarget(string key) =>
        key switch
        {
            "aprimo" => new(
                Description: "Writes assets and mapped metadata into Aprimo DAM.",
                ConfigurationFields:
                [
                    Url("baseUrl", "Aprimo Base URL", required: true),
                    Text("classificationId", "Classification ID", required: false),
                    Text("recordStatus", "Record Status", required: false),
                    Boolean("uploadFiles", "Upload Files", required: false, "true")
                ],
                CredentialRequirements:
                [
                    Secret("clientId", "Client ID", ConnectorSecretKinds.OAuthClientId, required: true),
                    Secret("clientSecret", "Client Secret", ConnectorSecretKinds.OAuthClientSecret, required: true),
                    Secret("tenant", "Tenant", ConnectorSecretKinds.ApiKey, required: false)
                ],
                SupportedOperations: ["validate", "write", "dryRun", "report"],
                SupportsManifestGeneration: false,
                SupportsValidation: true,
                SupportsDryRun: true),

            "bynder" => new(
                Description: "Writes assets and mapped metadata into Bynder.",
                ConfigurationFields:
                [
                    Url("baseUrl", "Bynder Base URL", required: true),
                    Text("brandId", "Brand ID", required: false),
                    Boolean("uploadFiles", "Upload Files", required: false, "true")
                ],
                CredentialRequirements:
                [
                    Secret("apiKey", "API Key", ConnectorSecretKinds.ApiKey, required: false),
                    Secret("bearerToken", "Bearer Token", ConnectorSecretKinds.BearerToken, required: false),
                    Secret("clientId", "OAuth Client ID", ConnectorSecretKinds.OAuthClientId, required: false),
                    Secret("clientSecret", "OAuth Client Secret", ConnectorSecretKinds.OAuthClientSecret, required: false)
                ],
                SupportedOperations: ["validate", "write", "dryRun", "metadataSchema", "report"],
                SupportsManifestGeneration: false,
                SupportsValidation: true,
                SupportsDryRun: true),

            "cloudinary" => new(
                Description: "Writes assets and mapped metadata into Cloudinary.",
                ConfigurationFields:
                [
                    Text("cloudName", "Cloud Name", required: true),
                    Text("folder", "Folder", required: false),
                    Boolean("overwrite", "Overwrite", required: false, "false")
                ],
                CredentialRequirements:
                [
                    Secret("apiKey", "API Key", ConnectorSecretKinds.ApiKey, required: true),
                    Secret("apiSecret", "API Secret", ConnectorSecretKinds.ApiSecret, required: true)
                ],
                SupportedOperations: ["validate", "write", "dryRun", "metadataSchema", "report"],
                SupportsManifestGeneration: false,
                SupportsValidation: true,
                SupportsDryRun: true),

            "s3" => new(
                Description: "Writes files to an Amazon S3 bucket.",
                ConfigurationFields:
                [
                    Text("bucket", "Bucket", required: true),
                    Text("prefix", "Prefix", required: false),
                    Text("region", "Region", required: false)
                ],
                CredentialRequirements:
                [
                    Secret("accessKeyId", "Access Key ID", ConnectorSecretKinds.AccessKeyId, required: true),
                    Secret("secretAccessKey", "Secret Access Key", ConnectorSecretKinds.SecretAccessKey, required: true)
                ],
                SupportedOperations: ["validate", "write", "dryRun", "report"],
                SupportsManifestGeneration: false,
                SupportsValidation: true,
                SupportsDryRun: true),

            "azureblob" => new(
                Description: "Writes files to Azure Blob Storage.",
                ConfigurationFields:
                [
                    Text("container", "Container", required: true),
                    Text("prefix", "Prefix", required: false),
                    Text("accountName", "Storage Account", required: false)
                ],
                CredentialRequirements:
                [
                    Secret("connectionString", "Connection String", ConnectorSecretKinds.ConnectionString, required: false),
                    Secret("accountKey", "Account Key", ConnectorSecretKinds.ApiSecret, required: false)
                ],
                SupportedOperations: ["validate", "write", "dryRun", "report"],
                SupportsManifestGeneration: false,
                SupportsValidation: true,
                SupportsDryRun: true),

            "localstorage" => new(
                Description: "Writes files to a local filesystem path.",
                ConfigurationFields:
                [
                    Path("rootPath", "Root Path", required: true),
                    Path("relativePath", "Relative Path", required: false)
                ],
                CredentialRequirements: [],
                SupportedOperations: ["validate", "write", "dryRun", "report"],
                SupportsManifestGeneration: false,
                SupportsValidation: true,
                SupportsDryRun: true),

            _ => ConnectorCapabilityEnrichment.Empty
        };

    private static ConnectorCapabilityEnrichment GetManifestProvider(string key) =>
        key switch
        {
            "csv" => ManifestProvider("CSV", "Loads migration manifests from comma-separated value files.", "path", "CSV Path"),
            "excel" => ManifestProvider("Excel", "Loads migration manifests from Excel workbooks.", "path", "Workbook Path"),
            "sql" => new(
                Description: "Loads migration manifests from SQL queries.",
                ConfigurationFields:
                [
                    Text("connectionName", "Connection Name", required: true),
                    Text("query", "Query", required: true),
                    Text("schema", "Schema", required: false)
                ],
                CredentialRequirements:
                [
                    Secret("connectionString", "Connection String", ConnectorSecretKinds.ConnectionString, required: true)
                ],
                SupportedOperations: ["load", "validate", "schema"],
                SupportsManifestGeneration: false,
                SupportsValidation: true,
                SupportsDryRun: true),
            "sqlite" => ManifestProvider("SQLite", "Loads migration manifests from SQLite database queries.", "databasePath", "Database Path"),
            _ => ConnectorCapabilityEnrichment.Empty
        };

    private static ConnectorCapabilityEnrichment ManifestProvider(string displayName, string description, string pathName, string pathLabel) =>
        new(
            Description: description,
            ConfigurationFields:
            [
                Path(pathName, pathLabel, required: true),
                Text("sheetName", "Sheet/Table Name", required: false),
                Boolean("hasHeaderRow", "Has Header Row", required: false, "true")
            ],
            CredentialRequirements: [],
            SupportedOperations: ["load", "validate", "schema"],
            SupportsManifestGeneration: false,
            SupportsValidation: true,
            SupportsDryRun: true);

    private static string NormalizeRole(string role)
    {
        if (string.Equals(role, "source", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "sources", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectorCapabilityRoles.Source;
        }

        if (string.Equals(role, "target", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "targets", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectorCapabilityRoles.Target;
        }

        if (string.Equals(role, "manifest", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "manifests", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "manifestProvider", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "manifestProviders", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectorCapabilityRoles.ManifestProvider;
        }

        return role;
    }

    private static ConnectorConfigurationFieldDescriptor Text(
        string name,
        string label,
        bool required,
        string? description = null) =>
        new(name, label, ConnectorFieldTypes.Text, required, description);

    private static ConnectorConfigurationFieldDescriptor Url(
        string name,
        string label,
        bool required,
        string? description = null) =>
        new(name, label, ConnectorFieldTypes.Url, required, description);

    private static ConnectorConfigurationFieldDescriptor Path(
        string name,
        string label,
        bool required,
        string? description = null) =>
        new(name, label, ConnectorFieldTypes.Path, required, description);

    private static ConnectorConfigurationFieldDescriptor MultiText(
        string name,
        string label,
        bool required,
        string? description = null) =>
        new(name, label, ConnectorFieldTypes.MultiText, required, description);

    private static ConnectorConfigurationFieldDescriptor Boolean(
        string name,
        string label,
        bool required,
        string? defaultValue,
        string? description = null) =>
        new(name, label, ConnectorFieldTypes.Boolean, required, description, defaultValue);

    private static ConnectorConfigurationFieldDescriptor Number(
        string name,
        string label,
        bool required,
        string? defaultValue,
        string? description = null) =>
        new(name, label, ConnectorFieldTypes.Number, required, description, defaultValue);

    private static ConnectorConfigurationFieldDescriptor Select(
        string name,
        string label,
        bool required,
        string? defaultValue,
        IReadOnlyList<string> options,
        string? description = null) =>
        new(name, label, ConnectorFieldTypes.Select, required, description, defaultValue, options);

    private static ConnectorCredentialRequirementDescriptor Secret(
        string name,
        string label,
        string secretKind,
        bool required,
        string? description = null) =>
        new(name, label, secretKind, required, description);
}

public sealed record ConnectorCapabilityEnrichment(
    string? Description,
    IReadOnlyList<ConnectorConfigurationFieldDescriptor> ConfigurationFields,
    IReadOnlyList<ConnectorCredentialRequirementDescriptor> CredentialRequirements,
    IReadOnlyList<string> SupportedOperations,
    bool SupportsManifestGeneration,
    bool SupportsValidation,
    bool SupportsDryRun)
{
    public static ConnectorCapabilityEnrichment Empty { get; } =
        new(
            Description: null,
            ConfigurationFields: Array.Empty<ConnectorConfigurationFieldDescriptor>(),
            CredentialRequirements: Array.Empty<ConnectorCredentialRequirementDescriptor>(),
            SupportedOperations: Array.Empty<string>(),
            SupportsManifestGeneration: false,
            SupportsValidation: true,
            SupportsDryRun: true);
}
