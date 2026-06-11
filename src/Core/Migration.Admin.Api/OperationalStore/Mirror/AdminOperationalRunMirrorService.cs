using System.Text.Json;
using Migration.ControlPlane.Models;
using Migration.Infrastructure.Sql.Records;
using Migration.Infrastructure.Sql.Stores;

namespace Migration.Admin.Api.OperationalStore;

public sealed class AdminOperationalRunMirrorService : IAdminOperationalRunMirrorService
{
    private readonly ISqlOperationalBackboneStore _backboneStore;
    private readonly IOperationalMirrorEnablementGuard _enablementGuard;
    private readonly OperationalMirrorInvocationState _invocationState;
    private readonly ILogger<AdminOperationalRunMirrorService> _logger;

    public AdminOperationalRunMirrorService(
        ISqlOperationalBackboneStore backboneStore,
        IOperationalMirrorEnablementGuard enablementGuard,
        OperationalMirrorInvocationState invocationState,
        ILogger<AdminOperationalRunMirrorService> logger)
    {
        _backboneStore = backboneStore;
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

        var guard = await _enablementGuard.EvaluateAsync(cancellationToken).ConfigureAwait(false);

        if (!guard.CanMirror)
        {
            var reason = string.Join("; ", guard.Messages);
            _invocationState.RecordSkipped(run.RunId, reason);

            _logger.LogWarning(
                "Operational run mirror skipped for run {RunId}. Reasons: {Reasons}",
                run.RunId,
                reason);

            return;
        }

        try
        {
            var operationalRunId = await MirrorRunCoreAsync(project, run, cancellationToken)
                .ConfigureAwait(false);

            _invocationState.RecordMirrored(run.RunId, operationalRunId);

            _logger.LogInformation(
                "Mirrored legacy run {RunId} into SQL operational backbone as {OperationalRunId}.",
                run.RunId,
                operationalRunId);
        }
        catch (Exception ex)
        {
            _invocationState.RecordFailed(run.RunId, ex);

            _logger.LogError(
                ex,
                "Operational run mirror failed for run {RunId}.",
                run.RunId);

            throw;
        }
    }

    private async Task<Guid> MirrorRunCoreAsync(
        MigrationProjectRecord project,
        MigrationRunControlRecord run,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var operationalRunId = OperationalMirrorIdFactory.CreateGuid(run.RunId);
        var operationalProjectId = OperationalMirrorIdFactory.CreateGuid(project.ProjectId);

        var existing = await _backboneStore.GetRunAsync(operationalRunId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            await _backboneStore.CreateProjectAsync(
                new SqlMigrationProjectRecord(
                    ProjectId: operationalProjectId,
                    ProjectKey: project.ProjectId,
                    ProjectName: string.IsNullOrWhiteSpace(project.DisplayName)
                        ? project.ProjectId
                        : project.DisplayName,
                    Status: "Active",
                    SettingsJson: null,
                    CreatedAtUtc: now,
                    UpdatedAtUtc: now),
                cancellationToken).ConfigureAwait(false);

            await _backboneStore.CreateRunAsync(
                new SqlMigrationRunRecord(
                    RunId: operationalRunId,
                    ProjectId: operationalProjectId,
                    RunKey: run.RunId,
                    RunName: run.JobName,
                    SourceSystem: run.Job.SourceType,
                    TargetSystem: run.Job.TargetType,
                    Status: "Queued",
                    StatusReason: null,
                    EnvironmentName: null,
                    IsDryRun: run.DryRun,
                    CoordinatorOwner: null,
                    CoordinationLeaseExpiresUtc: null,
                    RequestedAtUtc: now,
                    StartedAtUtc: null,
                    CompletedAtUtc: null,
                    CreatedAtUtc: now,
                    UpdatedAtUtc: now),
                cancellationToken).ConfigureAwait(false);
        }

        var payloadJson = JsonSerializer.Serialize(new
        {
            legacyRunId = run.RunId,
            legacyJobName = run.JobName,
            projectId = project.ProjectId,
            manifestPath = run.Job.ManifestPath,
            mappingProfilePath = run.Job.MappingProfilePath,
            sourceType = run.Job.SourceType,
            targetType = run.Job.TargetType,
            dryRun = run.DryRun,
            job = run.Job
        });

        var sqlNow = DateTime.UtcNow;
        await _backboneStore.UpsertManifestRowsAsync(
            new[]
            {
                new SqlMigrationManifestRowRecord(
                    ManifestRowId: 0,
                    RunId: operationalRunId,
                    SourceRowNumber: 1,
                    SourceExternalId: run.RunId,
                    SourcePath: run.Job.ManifestPath,
                    ContentHash: null,
                    Operation: "Migrate",
                    ManifestStatus: "Queued",
                    PayloadJson: payloadJson,
                    ValidationJson: null,
                    CreatedAtUtc: sqlNow,
                    UpdatedAtUtc: sqlNow)
            },
            cancellationToken).ConfigureAwait(false);

        await _backboneStore.EnqueueWorkItemsAsync(
            new[]
            {
                new SqlMigrationWorkItemRecord(
                    WorkItemId: 0,
                    RunId: operationalRunId,
                    ManifestRowId: null,
                    WorkType: "MigrationRun",
                    Status: "Queued",
                    Priority: 0,
                    AttemptCount: 0,
                    MaxAttempts: 3,
                    AvailableAtUtc: sqlNow,
                    ClaimedBy: null,
                    ClaimedAtUtc: null,
                    LeaseExpiresAtUtc: null,
                    StartedAtUtc: null,
                    CompletedAtUtc: null,
                    IdempotencyKey: $"legacy-run:{run.RunId}",
                    PayloadJson: payloadJson,
                    ResultJson: null,
                    LastErrorCode: null,
                    LastErrorMessage: null,
                    CreatedAtUtc: sqlNow,
                    UpdatedAtUtc: sqlNow,
                    PartitionKey: operationalRunId.ToString("N"),
                    NotBeforeUtc: sqlNow,
                    LeaseExpiresUtc: null,
                    CreatedUtc: sqlNow,
                    LeaseOwner: null,
                    UpdatedUtc: sqlNow,
                    WorkItemType: "MigrationRun",
                    DispatchedAtUtc: null)
            },
            cancellationToken).ConfigureAwait(false);

        return operationalRunId;
    }
}