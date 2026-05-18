using Migration.Application.Models.OperationalStore;
using Migration.Application.OperationalStore;

namespace Migration.Application.Abstractions.OperationalStore;

public interface IOperationalManifestRecordBuilder
{
    MigrationManifestRecord Build(
        OperationalManifestRecordInput input);

    IReadOnlyList<MigrationManifestRecord> BuildBatch(
        IReadOnlyCollection<OperationalManifestRecordInput> inputs);
}
