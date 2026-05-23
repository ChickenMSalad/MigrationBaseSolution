namespace Migration.Core.Azure.ResourceNaming;

public interface IAzureResourceNameBuilder
{
    AzureResourceNameResult Build(AzureResourceNameRequest request);
}
