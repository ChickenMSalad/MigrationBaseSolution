namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalGlobalFailureCatalogService
    : IOperationalGlobalFailureCatalogService
{
    private readonly IOperationalGlobalFailureService _failureService;

    public OperationalGlobalFailureCatalogService(
        IOperationalGlobalFailureService failureService)
    {
        _failureService = failureService;
    }

    public async Task<OperationalGlobalFailureCatalogResponse> GetCatalogAsync(
        int sampleLimit = 500,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(sampleLimit, 1, 500);

        var recent = await _failureService.GetRecentFailuresAsync(
            safeLimit,
            cancellationToken);

        var failures = recent.Failures.ToArray();

        var failureTypes = failures
            .Select(f => f.FailureType)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var runStatuses = failures
            .Select(f => f.RunStatus)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var sourceSystems = failures
            .Select(f => f.SourceSystem)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var targetSystems = failures
            .Select(f => f.TargetSystem)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new OperationalGlobalFailureCatalogResponse
        {
            FailureTypes = failureTypes,
            RunStatuses = runStatuses,
            SourceSystems = sourceSystems,
            TargetSystems = targetSystems,
            FailureTypeCount = failureTypes.Length,
            RunStatusCount = runStatuses.Length,
            SourceSystemCount = sourceSystems.Length,
            TargetSystemCount = targetSystems.Length,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}
