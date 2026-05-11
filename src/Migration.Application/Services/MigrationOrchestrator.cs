using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Application.Services;

public sealed class MigrationOrchestrator
{
    private readonly IEnumerable<IManifestProvider> _manifestProviders;
    private readonly IEnumerable<IAssetSourceConnector> _sourceConnectors;
    private readonly IEnumerable<IAssetTargetConnector> _targetConnectors;
    private readonly IMappingProfileLoader _mappingProfileLoader;
    private readonly IMapper _mapper;
    private readonly IEnumerable<ITransformStep> _transformSteps;
    private readonly IEnumerable<IValidationStep> _validationSteps;
    private readonly IJobStateStore _jobStateStore;

    public MigrationOrchestrator(
        IEnumerable<IManifestProvider> manifestProviders,
        IEnumerable<IAssetSourceConnector> sourceConnectors,
        IEnumerable<IAssetTargetConnector> targetConnectors,
        IMappingProfileLoader mappingProfileLoader,
        IMapper mapper,
        IEnumerable<ITransformStep> transformSteps,
        IEnumerable<IValidationStep> validationSteps,
        IJobStateStore jobStateStore)
    {
        _manifestProviders = manifestProviders;
        _sourceConnectors = sourceConnectors;
        _targetConnectors = targetConnectors;
        _mappingProfileLoader = mappingProfileLoader;
        _mapper = mapper;
        _transformSteps = transformSteps;
        _validationSteps = validationSteps;
        _jobStateStore = jobStateStore;
    }

    public async Task<IReadOnlyList<MigrationResult>> RunAsync(MigrationJobDefinition job, CancellationToken cancellationToken = default)
    {
        var manifestProvider = _manifestProviders.Single(x => x.Type.Equals(job.ManifestType, StringComparison.OrdinalIgnoreCase));
        var sourceConnector = _sourceConnectors.Single(x => x.Type.Equals(job.SourceType, StringComparison.OrdinalIgnoreCase));
        var targetConnector = _targetConnectors.Single(x => x.Type.Equals(job.TargetType, StringComparison.OrdinalIgnoreCase));
        var profile = await _mappingProfileLoader.LoadAsync(job.MappingProfilePath, cancellationToken);
        var manifestRows = await manifestProvider.ReadAsync(job, cancellationToken);

        var results = new List<MigrationResult>();

        foreach (var row in manifestRows)
        {
            var item = new AssetWorkItem
            {
                WorkItemId = row.RowId,
                Manifest = row,
                SourceAsset = await sourceConnector.GetAssetAsync(job, row, cancellationToken)
            };

            item.TargetPayload = _mapper.Map(item.SourceAsset, row, profile);

            foreach (var transform in _transformSteps)
            {
                await transform.ApplyAsync(item, cancellationToken);
            }

            var issues = new List<ValidationIssue>();
            foreach (var validation in _validationSteps)
            {
                issues.AddRange(await validation.ValidateAsync(item, cancellationToken));
            }

            if (issues.Any(x => x.IsError))
            {
                var failed = new MigrationResult
                {
                    WorkItemId = item.WorkItemId,
                    Success = false,
                    Message = string.Join("; ", issues.Select(x => x.Message))
                };
                results.Add(failed);
                await _jobStateStore.SaveCheckpointAsync(new CheckpointRecord
                {
                    JobName = job.JobName,
                    WorkItemId = item.WorkItemId,
                    Status = "ValidationFailed",
                    Detail = failed.Message
                }, cancellationToken);
                continue;
            }

            var result = job.DryRun
                ? new MigrationResult { WorkItemId = item.WorkItemId, Success = true, Message = "Dry run completed." }
                : await targetConnector.UpsertAsync(job, item, cancellationToken);

            results.Add(result);
            await _jobStateStore.SaveCheckpointAsync(new CheckpointRecord
            {
                JobName = job.JobName,
                WorkItemId = item.WorkItemId,
                Status = result.Success ? "Succeeded" : "Failed",
                Detail = result.Message
            }, cancellationToken);
        }

        return results;
    }
}
