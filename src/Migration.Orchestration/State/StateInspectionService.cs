using Migration.Orchestration.Abstractions;

namespace Migration.Orchestration.State;

/// <summary>
/// Thin application service over the optional maintenance surface of the state store.
/// Useful for future API/controllers and for keeping console code simple.
/// </summary>
public sealed class StateInspectionService
{
    private readonly IMigrationExecutionStateMaintenance _maintenance;

    public StateInspectionService(IMigrationExecutionStateMaintenance maintenance)
    {
        _maintenance = maintenance;
    }

    public Task<IReadOnlyList<MigrationWorkItemState>> ListWorkItemsAsync(
        string jobName,
        CancellationToken cancellationToken = default)
    {
        return _maintenance.ListWorkItemsAsync(jobName, cancellationToken);
    }

    public Task ResetJobAsync(
        string jobName,
        CancellationToken cancellationToken = default)
    {
        return _maintenance.ResetJobAsync(jobName, cancellationToken);
    }
}
