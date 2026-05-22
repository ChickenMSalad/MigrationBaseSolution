using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalRunDispatchSampleRequestFactory
    : IOperationalRunDispatchSampleRequestFactory
{
    public OperationalRunDispatchRequest CreateSample(
        int manifestRecordCount = 3)
    {
        if (manifestRecordCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(manifestRecordCount),
                "Manifest record count must be greater than zero.");
        }

        var manifestRecords = Enumerable
            .Range(1, manifestRecordCount)
            .Select(index => new OperationalManifestRecordInput
            {
                SequenceNumber = index,
                SourceId = $"sample-source-{index}",
                SourcePath = $"/sample/path/{index}",
                SourceName = $"sample-file-{index}.bin",
                ContentType = "application/octet-stream",
                ContentLength = 1024 * index
            })
            .ToArray();

        return new OperationalRunDispatchRequest
        {
            SourceSystem = "SampleSource",
            TargetSystem = "SampleTarget",
            ManifestRecords = manifestRecords
        };
    }
}
