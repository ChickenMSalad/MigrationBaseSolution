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
    /// If no Enabled* list is supplied, register every known connector/provider.
    /// Keep this false for worker/API hosts unless the host is intentionally validating all connector modules.
    /// </summary>
    public bool RegisterAllWhenEmpty { get; set; } = false;

    public List<string> EnabledSources { get; set; } = new();

    public List<string> EnabledTargets { get; set; } = new();

    public List<string> EnabledManifests { get; set; } = new();
}
