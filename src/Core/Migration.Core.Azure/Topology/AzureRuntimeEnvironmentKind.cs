namespace Migration.Core.Azure.Topology;

/// <summary>
/// Classifies the operational environment a migration runtime is running in.
/// </summary>
public enum AzureRuntimeEnvironmentKind
{
    Unknown = 0,
    Local = 1,
    Development = 2,
    Test = 3,
    Staging = 4,
    Production = 5
}
