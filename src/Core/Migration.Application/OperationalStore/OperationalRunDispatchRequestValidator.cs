using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalRunDispatchRequestValidator : IOperationalRunDispatchRequestValidator
{
    public void Validate(
        OperationalRunDispatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.SourceSystem))
        {
            throw new ArgumentException(
                "Operational run dispatch request must include a source system.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.TargetSystem))
        {
            throw new ArgumentException(
                "Operational run dispatch request must include a target system.",
                nameof(request));
        }

        if (request.ManifestRecords.Count == 0)
        {
            throw new ArgumentException(
                "Operational run dispatch request must include at least one manifest record.",
                nameof(request));
        }

        var sequenceNumbers = new HashSet<long>();
        var sourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifestRecord in request.ManifestRecords)
        {
            if (manifestRecord.SequenceNumber <= 0)
            {
                throw new ArgumentException(
                    "Operational manifest records must use positive sequence numbers.",
                    nameof(request));
            }

            if (!sequenceNumbers.Add(manifestRecord.SequenceNumber))
            {
                throw new ArgumentException(
                    $"Duplicate manifest sequence number detected: {manifestRecord.SequenceNumber}.",
                    nameof(request));
            }

            if (string.IsNullOrWhiteSpace(manifestRecord.SourceId))
            {
                throw new ArgumentException(
                    "Operational manifest records must include source identifiers.",
                    nameof(request));
            }

            if (!sourceIds.Add(manifestRecord.SourceId))
            {
                throw new ArgumentException(
                    $"Duplicate manifest source identifier detected: {manifestRecord.SourceId}.",
                    nameof(request));
            }

            if (manifestRecord.ContentLength < 0)
            {
                throw new ArgumentException(
                    "Operational manifest records cannot have negative content lengths.",
                    nameof(request));
            }
        }
    }
}
