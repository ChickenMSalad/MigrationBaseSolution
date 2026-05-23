namespace Migration.Core.Azure.ResourceNaming;

public interface IAzureResourceTagBuilder
{
    IReadOnlyDictionary<string, string> Build(string environmentName, string workloadName);
}
