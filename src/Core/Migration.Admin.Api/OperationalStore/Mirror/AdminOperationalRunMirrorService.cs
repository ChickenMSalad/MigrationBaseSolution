using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.Models.OperationalStore;
using Migration.Application.Models.OperationalStore.Statuses;
using Migration.ControlPlane.Models;

namespace Migration.Admin.Api.OperationalStore;

public sealed class AdminOperationalRunMirrorService : IAdminOperationalRunMirrorService
{
    private readonly IOperationalStore _operationalStore;
    private readonly IOperationalMirrorEnablementGuard _enablementGuard;
    private readonly OperationalMirrorInvocationState _invocationState;
    private readonly ILogger<AdminOperationalRunMirrorService> _logger;

    public AdminOperationalRunMirrorService(
        IOperationalStore operationalStore,
        IOperationalMirrorEnablementGuard enablementGuard,
        OperationalMirrorInvocationState invocationState,
        ILogger<AdminOperationalRunMirrorService> logger)
    {
        _operationalStore = operationalStore;
        _enablementGuard = enablementGuard;
        _invocationState = invocationState;
        _logger = logger;
    }

    public async Task MirrorRunAsync(
        MigrationProjectRecord project,
        MigrationRunControlRecord run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(run);

        _logger.LogInformation(
            "Operational mirror hook invoked for legacy run {RunId}.",
            run.RunId);

        var guard = await _enablementGuard.EvaluateAsync(
            cancellationToken);

        if (!guard.CanMirror)
        {
            var reason = string.Join("; ", guard.Messages);

            _invocationState.RecordSkipped(
                run.RunId,
                reason);

            _logger.LogWarning(
                "Operational run mirror skipped for run {RunId}. Reasons: {Reasons}",
                run.RunId,
                reason);

            return;
        }

        try
        {
            var operationalRunId = await MirrorRunCoreAsync(
                project,
                run,
                cancellationToken);

            _invocationState.RecordMirrored(
                run.RunId,
                operationalRunId);

            _logger.LogInformation(
                "Mirrored legacy run {RunId} into operational store as {OperationalRunId}.",
                run.RunId,
                operationalRunId);
        }
        catch (Exception ex)
        {
            _invocationState.RecordFailed(
                run.RunId,
                ex);

            _logger.LogError(
                ex,
                "Operational run mirror failed for run {RunId}. Legacy run flow will continue.",
                run.RunId);
        }
    }

    private async Task<Guid> MirrorRunCoreAsync(
        MigrationProjectRecord project,
        MigrationRunControlRecord run,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var operationalRunId = OperationalMirrorIdFactory.CreateGuid(
            $"legacy-run:{run.RunId}");

        var existingRun = await _operationalStore.Runs.GetAsync(
            operationalRunId,
            cancellationToken);

        if (existingRun is null)
        {
            await _operationalStore.Runs.CreateAsync(
                new MigrationRunRecord
                {
                    RunId = operationalRunId,
                    SourceSystem = project.SourceType,
                    TargetSystem = project.TargetType,
                    Status = MigrationRunStatuses.Created,
                    CreatedAt = now
                },
                cancellationToken);
        }



            await _operationalStore.ManifestRecords.AddAsync(
                new MigrationManifestRecord
                {
                    RunId = operationalRunId,
                    SequenceNumber = 1,
                    SourceId = run.Job.ManifestPath,
                    SourcePath = run.Job.ManifestPath,
                    SourceName = Path.GetFileName(run.Job.ManifestPath),
                    Status = MigrationManifestStatuses.Created,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                cancellationToken);
        



            await _operationalStore.WorkItems.AddAsync(
                new MigrationWorkItemRecord
                {
                    RunId = operationalRunId,
                    Status = MigrationWorkItemStatuses.Created,
                    AttemptCount = 0,
                    CreatedAt = now
                },
                cancellationToken);
        

        await _operationalStore.Checkpoints.UpsertAsync(
            new MigrationCheckpointRecord
            {
                CheckpointId = OperationalMirrorIdFactory.CreateGuid(
                    $"legacy-run:{run.RunId}:checkpoint:legacy-run-id"),
                RunId = operationalRunId,
                CheckpointName = "LegacyRunId",
                CheckpointValue = run.RunId,
                CreatedAt = now,
                UpdatedAt = now
            },
            cancellationToken);

        await _operationalStore.Checkpoints.UpsertAsync(
            new MigrationCheckpointRecord
            {
                CheckpointId = OperationalMirrorIdFactory.CreateGuid(
                    $"legacy-run:{run.RunId}:checkpoint:legacy-job-name"),
                RunId = operationalRunId,
                CheckpointName = "LegacyJobName",
                CheckpointValue = run.JobName,
                CreatedAt = now,
                UpdatedAt = now
            },
            cancellationToken);

        await _operationalStore.Checkpoints.UpsertAsync(
            new MigrationCheckpointRecord
            {
                CheckpointId = OperationalMirrorIdFactory.CreateGuid(
                    $"legacy-run:{run.RunId}:checkpoint:legacy-project-id"),
                RunId = operationalRunId,
                CheckpointName = "LegacyProjectId",
                CheckpointValue = project.ProjectId,
                CreatedAt = now,
                UpdatedAt = now
            },
            cancellationToken);

        return operationalRunId;
    }
}
