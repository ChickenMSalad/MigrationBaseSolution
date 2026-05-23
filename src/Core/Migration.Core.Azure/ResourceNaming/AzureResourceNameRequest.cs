namespace Migration.Core.Azure.ResourceNaming;

public sealed class AzureResourceNameRequest
{
    public string ResourceType { get; init; } = string.Empty;

    public string Workload { get; init; } = string.Empty;

    public string? Instance { get; init; }

    public int? MaxLength { get; init; }

    public bool StorageAccountSafe { get; init; }
}
