namespace Migration.GenericRuntime.Options;

/// <summary>
/// Controls which concrete connectors/providers are registered for a given host.
///
/// This prevents cloud/worker processes from eagerly registering connectors they do not need
/// and therefore avoids requiring every source/target secret just to start the process.
///
/// Example:
/// {
///   "GenericMigrationRuntime": {
///     "RegisterAllWhenEmpty": false,
///     "EnabledSources": [ "LocalStorage" ],
///     "EnabledTargets": [ "LocalStorage" ],
///     "EnabledManifests": [ "Csv" ]
///   }
/// }
/// </summary>
public sealed class GenericMigrationRuntimeOptions
{
    public const string SectionName = "GenericMigrationRuntime";

    /// <summary>
    /// Backward-compatible default. If no Enabled* list is supplied, register every known connector/provider.
    /// Set this to false in worker/API smoke tests or narrowly scoped deployments.
    /// </summary>
    public bool RegisterAllWhenEmpty { get; init; } = true;

    public List<string> EnabledSources { get; init; } = new();

    public List<string> EnabledTargets { get; init; } = new();

    public List<string> EnabledManifests { get; init; } = new();
}
