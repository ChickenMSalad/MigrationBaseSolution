using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Infrastructure.State.OperationalStore.Sql;

public sealed class SqlOperationalStore : IOperationalStore
{
    public SqlOperationalStore(
        IMigrationRunStore runs,
        IMigrationManifestStore manifestRecords,
        IMigrationWorkItemStore workItems,
        IMigrationFailureStore failures,
        IMigrationCheckpointStore checkpoints,
        IMigrationIdentifierMapStore identifierMaps)
    {
        Runs = runs;
        ManifestRecords = manifestRecords;
        WorkItems = workItems;
        Failures = failures;
        Checkpoints = checkpoints;
        IdentifierMaps = identifierMaps;
    }

    public IMigrationRunStore Runs { get; }

    public IMigrationManifestStore ManifestRecords { get; }

    public IMigrationWorkItemStore WorkItems { get; }

    public IMigrationFailureStore Failures { get; }

    public IMigrationCheckpointStore Checkpoints { get; }

    public IMigrationIdentifierMapStore IdentifierMaps { get; }
}
