namespace Migration.ControlPlane.Storage;

public sealed class AzureBlobStorageOptions
{
    public const string SectionName = "AzureBlobStorage";

    public string? AccountName { get; init; }

    public string? ServiceUri { get; init; }

    public string? ConnectionString { get; init; }

    public string? ContainerName { get; init; }

    public bool UseManagedIdentity { get; init; } = true;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString) ||
        !string.IsNullOrWhiteSpace(ServiceUri) ||
        !string.IsNullOrWhiteSpace(AccountName);
}
