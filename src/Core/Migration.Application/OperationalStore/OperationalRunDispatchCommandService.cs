using Migration.Application.Abstractions.OperationalStore;

namespace Migration.Application.OperationalStore;

public sealed class OperationalRunDispatchCommandService : IOperationalRunDispatchCommandService
{
    private readonly IOperationalManifestRecordBuilder _manifestRecordBuilder;
    private readonly IOperationalRunDispatchService _runDispatchService;

    public OperationalRunDispatchCommandService(
        IOperationalManifestRecordBuilder manifestRecordBuilder,
        IOperationalRunDispatchService runDispatchService)
    {
        _manifestRecordBuilder = manifestRecordBuilder;
        _runDispatchService = runDispatchService;
    }

    public Task<OperationalRunDispatchResult> DispatchAsync(
        OperationalRunDispatchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.SourceSystem))
        {
            throw new ArgumentException(
                "Operational run dispatch command must include a source system.",
                nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.TargetSystem))
        {
            throw new ArgumentException(
                "Operational run dispatch command must include a target system.",
                nameof(command));
        }

        if (command.ManifestRecords.Count == 0)
        {
            throw new ArgumentException(
                "Operational run dispatch command must include at least one manifest record.",
                nameof(command));
        }

        var manifestRecords = _manifestRecordBuilder.BuildBatch(
            command.ManifestRecords);

        return _runDispatchService.DispatchAsync(
            command.SourceSystem,
            command.TargetSystem,
            manifestRecords,
            cancellationToken);
    }
}
