namespace Migration.Orchestration.Preflight;

public interface IMigrationPreflightService
{
    Task<PreflightResult> RunAsync(PreflightRequest request, CancellationToken cancellationToken = default);
}
