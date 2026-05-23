namespace MigrationBase.Core.Cloud.Azure.Configuration;

/// <summary>
/// A small immutable descriptor used by hosts, validators, and deployment scripts to agree on
/// environment-specific cloud runtime expectations.
/// </summary>
public sealed record AzureRuntimeEnvironmentProfile(
    string EnvironmentName,
    string ConfigurationFileName,
    bool IsProductionLike,
    bool RequiresManagedIdentity,
    bool RequiresDurableSqlOperationalStore,
    bool RequiresTelemetry);
