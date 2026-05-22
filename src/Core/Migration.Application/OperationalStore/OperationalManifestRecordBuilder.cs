using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Migration.Application.Models.OperationalStore.Statuses;

namespace Migration.Application.OperationalStore;

public sealed class OperationalManifestRecordBuilder : IOperationalManifestRecordBuilder
{
    public MigrationManifestRecord Build(
        OperationalManifestRecordInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.SourceId))
        {
            throw new ArgumentException(
                "Manifest record input must include a source identifier.",
                nameof(input));
        }

        return new MigrationManifestRecord
        {
            ManifestRecordId = Guid.NewGuid(),
            SequenceNumber = input.SequenceNumber,
            SourceId = input.SourceId,
            SourcePath = input.SourcePath,
            SourceName = input.SourceName,
            ContentType = input.ContentType,
            ContentLength = input.ContentLength,
            Status = MigrationManifestStatuses.Created,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public IReadOnlyList<MigrationManifestRecord> BuildBatch(
        IReadOnlyCollection<OperationalManifestRecordInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        if (inputs.Count == 0)
        {
            return Array.Empty<MigrationManifestRecord>();
        }

        return inputs
            .Select(Build)
            .ToArray();
    }
}
