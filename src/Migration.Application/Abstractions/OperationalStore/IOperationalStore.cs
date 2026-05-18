namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalStore
{
    IMigrationRunStore Runs { get; }

    IMigrationManifestStore ManifestRecords { get; }

    IMigrationWorkItemStore WorkItems { get; }

    IMigrationFailureStore Failures { get; }

    IMigrationCheckpointStore Checkpoints { get; }

    IMigrationIdentifierMapStore IdentifierMaps { get; }
}
