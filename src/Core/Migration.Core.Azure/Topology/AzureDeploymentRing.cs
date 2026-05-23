namespace Migration.Core.Azure.Topology;

/// <summary>
/// Describes the release/deployment ring used by a runtime environment.
/// </summary>
public enum AzureDeploymentRing
{
    Unknown = 0,
    Local = 1,
    InnerLoop = 2,
    NonProduction = 3,
    PreProduction = 4,
    Production = 5
}
