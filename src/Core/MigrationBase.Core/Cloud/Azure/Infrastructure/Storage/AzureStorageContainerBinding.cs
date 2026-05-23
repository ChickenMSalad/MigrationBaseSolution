namespace MigrationBase.Core.Cloud.Azure.Infrastructure.Storage;

public sealed class AzureStorageContainerBinding
{
    public required string Name { get; init; }
    public required string StorageAccountBindingName { get; init; }
    public required string ContainerName { get; init; }
    public string Purpose { get; init; } = "OperationalArtifact";
    public bool RequiresPrivateEndpoint { get; init; } = true;
    public bool AllowPublicAccess { get; init; }
}
