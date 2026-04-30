using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Application.Models;
using Migration.Domain.Models;
using Migration.Orchestration.Abstractions;
using Migration.Orchestration.Options;
using Migration.Orchestration.State;

namespace Migration.Orchestration.Execution;

public sealed class GenericMigrationJobRunner : IMigrationJobRunner
{
    private readonly IEnumerable<IManifestProvider> _manifestProviders;
    private readonly IEnumerable<IAssetSourceConnector> _sourceConnectors;
    private readonly IEnumerable<IAssetTargetConnector> _targetConnectors;
    private readonly IMappingProfileLoader _mappingProfileLoader;
    private readonly IMapper _mapper;
    private readonly IEnumerable<ITransformStep> _transformSteps;
    private readonly IEnumerable<IValidationStep> _validationSteps;
    private readonly IMigrationExecutionStateStore _stateStore;
    private readonly IMigrationProgressReporter _progressReporter;
    private readonly IMigrationRetryPolicy _retryPolicy;
    private readonly MigrationExecutionOptions _options;
    private readonly ILogger<GenericMigrationJobRunner> _logger;

    public GenericMigrationJobRunner(
        IEnumerable<IManifestProvider> manifestProviders,
        IEnumerable<IAssetSourceConnector> sourceConnectors,
        IEnumerable<IAssetTargetConnector> targetConnectors,
        IMappingProfileLoader mappingProfileLoader,
        IMapper mapper,
        IEnumerable<ITransformStep> transformSteps,
        IEnumerable<IValidationStep> validationSteps,
        IMigrationExecutionStateStore stateStore,
        IMigrationProgressReporter progressReporter,
        IMigrationRetryPolicy retryPolicy,
        IOptions<MigrationExecutionOptions> options,
        ILogger<GenericMigrationJobRunner> logger)
    {
        _manifestProviders = manifestProviders;
        _sourceConnectors = sourceConnectors;
        _targetConnectors = targetConnectors;
        _mappingProfileLoader = mappingProfileLoader;
        _mapper = mapper;
        _transformSteps = transformSteps;
        _validationSteps = validationSteps;
        _stateStore = stateStore;
        _progressReporter = progressReporter;
        _retryPolicy = retryPolicy;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MigrationRunSummary> RunAsync(MigrationJobDefinition job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        var stateJobName = JobStateKeyFactory.BuildStateJobName(job);
        var manifestFingerprint = JobStateKeyFactory.ComputeManifestFingerprint(job.ManifestPath);
        var executionMode = job.DryRun ? "DryRun" : "Live";
        var runId = CreateRunId(job.JobName);
        var started = DateTimeOffset.UtcNow;

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["RunId"] = runId,
            ["JobName"] = job.JobName,
            ["SourceType"] = job.SourceType,
            ["TargetType"] = job.TargetType,
            ["DryRun"] = job.DryRun,
            ["StateJobName"] = stateJobName,
            ["ManifestFingerprint"] = manifestFingerprint,
            ["ExecutionMode"] = executionMode
        });

        await ReportAsync(runId, job, MigrationProgressEvents.RunStarted, null, null, null, $"Starting migration job '{job.JobName}'.", cancellationToken).ConfigureAwait(false);

        var manifestProvider = ResolveSingle(_manifestProviders, x => x.Type, job.ManifestType, "manifest provider");
        var sourceConnector = ResolveSingle(_sourceConnectors, x => x.Type, job.SourceType, "source connector");
        var targetConnector = ResolveSingle(_targetConnectors, x => x.Type, job.TargetType, "target connector");
        var profile = await _mappingProfileLoader.LoadAsync(job.MappingProfilePath, cancellationToken).ConfigureAwait(false);
        var manifestRows = await _retryPolicy.ExecuteAsync("Manifest.Read", token => manifestProvider.ReadAsync(job, token), cancellationToken).ConfigureAwait(false);

        var total = manifestRows.Count;
        await _stateStore.StartRunAsync(new MigrationRunRecord
        {
            RunId = runId,
            JobName = stateJobName,
            SourceType = job.SourceType,
            TargetType = job.TargetType,
            DryRun = job.DryRun,
            StartedUtc = started,
            Status = "Running",
            TotalWorkItems = total
        }, cancellationToken).ConfigureAwait(false);

        await ReportAsync(runId, job, MigrationProgressEvents.ManifestLoaded, null, 0, total, $"Loaded {total} manifest rows.", cancellationToken).ConfigureAwait(false);

        var results = new ConcurrentBag<MigrationResult>();
        var succeeded = 0;
        var failed = 0;
        var skipped = 0;
        var validationFailed = 0;
        var completed = 0;
        var parallelism = Math.Max(1, job.Parallelism > 0 ? job.Parallelism : _options.MaxDegreeOfParallelism);
        var loopOptions = new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = parallelism };

        try
        {
            await Parallel.ForEachAsync(manifestRows, loopOptions, async (row, token) =>
            {
                token.ThrowIfCancellationRequested();
                var itemResult = await ProcessRowAsync(runId, stateJobName, manifestFingerprint, executionMode, job, row, sourceConnector, targetConnector, profile, total, token).ConfigureAwait(false);

                if (itemResult.Skipped)
                {
                    Interlocked.Increment(ref skipped);
                }
                else if (itemResult.ValidationFailed)
                {
                    Interlocked.Increment(ref validationFailed);
                    Interlocked.Increment(ref failed);
                }
                else if (itemResult.Result.Success)
                {
                    Interlocked.Increment(ref succeeded);
                }
                else
                {
                    Interlocked.Increment(ref failed);
                    if (_options.StopOnFirstError)
                    {
                        throw new InvalidOperationException($"Migration failed for work item '{itemResult.Result.WorkItemId}': {itemResult.Result.Message}");
                    }
                }

                results.Add(itemResult.Result);
                var done = Interlocked.Increment(ref completed);
                await ReportAsync(runId, job, itemResult.Result.Success ? MigrationProgressEvents.WorkItemSucceeded : MigrationProgressEvents.WorkItemFailed,
                    itemResult.Result.WorkItemId, done, total, itemResult.Result.Message, token).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Migration job {JobName} was canceled.", job.JobName);
            throw;
        }
        finally
        {
            var elapsed = DateTimeOffset.UtcNow - started;
            await _stateStore.CompleteRunAsync(new MigrationRunRecord
            {
                RunId = runId,
                JobName = stateJobName,
                SourceType = job.SourceType,
                TargetType = job.TargetType,
                DryRun = job.DryRun,
                StartedUtc = started,
                CompletedUtc = DateTimeOffset.UtcNow,
                Status = failed == 0 ? (job.DryRun ? "DryRunSucceeded" : "Succeeded") : "CompletedWithFailures",
                TotalWorkItems = total,
                Succeeded = succeeded,
                Failed = failed,
                Skipped = skipped,
                ValidationFailed = validationFailed
            }, CancellationToken.None).ConfigureAwait(false);

            await ReportAsync(runId, job, MigrationProgressEvents.RunCompleted, null, completed, total,
                $"Completed in {elapsed:g}. Succeeded={succeeded}; Failed={failed}; ValidationFailed={validationFailed}; Skipped={skipped}.", CancellationToken.None).ConfigureAwait(false);
        }

        var ordered = results.OrderBy(x => x.WorkItemId, StringComparer.OrdinalIgnoreCase).ToList();
        return new MigrationRunSummary(runId, job.JobName, total, succeeded, failed, skipped, DateTimeOffset.UtcNow - started, ordered);
    }

    private async Task<RowProcessResult> ProcessRowAsync(
        string runId,
        string stateJobName,
        string manifestFingerprint,
        string executionMode,
        MigrationJobDefinition job,
        ManifestRow row,
        IAssetSourceConnector sourceConnector,
        IAssetTargetConnector targetConnector,
        MappingProfile profile,
        int total,
        CancellationToken cancellationToken)
    {
        var workItemId = GetStateWorkItemId(row);
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["WorkItemId"] = workItemId,
            ["SourceAssetId"] = row.SourceAssetId
        });

        await ReportAsync(runId, job, MigrationProgressEvents.WorkItemStarted, workItemId, null, total, "Processing work item.", cancellationToken).ConfigureAwait(false);

        var existingState = await _stateStore.GetWorkItemAsync(stateJobName, workItemId, cancellationToken).ConfigureAwait(false);
        if (_options.ResumeCompletedWorkItems && !job.DryRun)
        {
            if (existingState?.IsTerminalSuccess == true)
            {
                var skipped = new MigrationResult { WorkItemId = workItemId, Success = true, TargetAssetId = existingState.TargetAssetId, Message = "Skipped; work item already succeeded in real-run state store." };
                await ReportAsync(runId, job, MigrationProgressEvents.WorkItemSkipped, workItemId, null, total, skipped.Message, cancellationToken).ConfigureAwait(false);
                return new RowProcessResult(skipped, true, false);
            }
        }

        var attemptCount = (existingState?.AttemptCount ?? 0) + 1;

        await _stateStore.SaveWorkItemAsync(new MigrationWorkItemState
        {
            RunId = runId,
            JobName = stateJobName,
            WorkItemId = workItemId,
            SourceAssetId = row.SourceAssetId,
            Status = MigrationWorkItemStatuses.Running,
            DryRun = job.DryRun,
            StartedUtc = DateTimeOffset.UtcNow,
            AttemptCount = attemptCount,
            Properties = CreateStateProperties(job, row, manifestFingerprint, executionMode)
        }, cancellationToken).ConfigureAwait(false);

        try
        {
            var source = await _retryPolicy.ExecuteAsync($"Source.GetAsset.{workItemId}", token => sourceConnector.GetAssetAsync(job, row, token), cancellationToken).ConfigureAwait(false);
            await ReportAsync(runId, job, MigrationProgressEvents.SourceReadCompleted, workItemId, null, total, "Source asset loaded.", cancellationToken).ConfigureAwait(false);

            var item = new AssetWorkItem
            {
                WorkItemId = workItemId,
                Manifest = row,
                SourceAsset = source,
                TargetPayload = _mapper.Map(source, row, profile)
            };

            foreach (var transform in _transformSteps)
            {
                await transform.ApplyAsync(item, cancellationToken).ConfigureAwait(false);
            }

            var issues = new List<ValidationIssue>();
            foreach (var validation in _validationSteps)
            {
                var stepIssues = await validation.ValidateAsync(item, cancellationToken).ConfigureAwait(false);
                issues.AddRange(stepIssues);
            }

            if (_options.Validation.TreatWarningsAsErrors)
            {
                issues = issues.Select(x => x.IsError ? x : new ValidationIssue(x.Code, x.Message, true)).ToList();
            }

            var errors = issues.Where(x => x.IsError).ToList();
            var warnings = issues
                .Where(x => !x.IsError)
                .Select(x => $"{x.Code}: {x.Message}")
                .ToList();

            if (errors.Count > 0)
            {
                var message = string.Join("; ", errors.Select(x => $"{x.Code}: {x.Message}"));
                var result = new MigrationResult
                {
                    WorkItemId = workItemId,
                    Success = false,
                    Message = message,
                    Warnings = warnings
                };

                await _stateStore.SaveWorkItemAsync(new MigrationWorkItemState
                {
                    RunId = runId,
                    JobName = stateJobName,
                    WorkItemId = workItemId,
                    SourceAssetId = source.SourceAssetId,
                    DryRun = job.DryRun,
                    AttemptCount = attemptCount,
                    Status = MigrationWorkItemStatuses.ValidationFailed,
                    CompletedUtc = DateTimeOffset.UtcNow,
                    Message = message,
                    LastError = message,
                    Properties = CreateStateProperties(job, row, manifestFingerprint, executionMode)
                }, cancellationToken).ConfigureAwait(false);
                return new RowProcessResult(result, false, true);
            }

            var migrationResult = job.DryRun
                ? new MigrationResult
                {
                    WorkItemId = workItemId,
                    Success = true,
                    Message = warnings.Count == 0
                        ? "Dry run validation completed. No target write attempted."
                        : $"Dry run validation completed with {warnings.Count} warning(s). No target write attempted.",
                    Warnings = warnings
                }
                : await _retryPolicy.ExecuteAsync($"Target.Upsert.{workItemId}", token => targetConnector.UpsertAsync(job, item, token), cancellationToken).ConfigureAwait(false);

            if (!job.DryRun && warnings.Count > 0)
            {
                migrationResult = WithWarnings(migrationResult, warnings);
            }

            if (!job.DryRun && migrationResult.Success && string.IsNullOrWhiteSpace(migrationResult.TargetAssetId))
            {
                migrationResult = new MigrationResult
                {
                    WorkItemId = workItemId,
                    Success = false,
                    Message = "Target connector reported success but returned no TargetAssetId. Refusing false success."
                };
            }

            await ReportAsync(runId, job, MigrationProgressEvents.TargetUpsertCompleted, workItemId, null, total, migrationResult.Message, cancellationToken).ConfigureAwait(false);
            await _stateStore.SaveWorkItemAsync(new MigrationWorkItemState
            {
                RunId = runId,
                JobName = stateJobName,
                WorkItemId = workItemId,
                SourceAssetId = source.SourceAssetId,
                DryRun = job.DryRun,
                AttemptCount = attemptCount,
                Status = job.DryRun ? MigrationWorkItemStatuses.DryRunSucceeded : (migrationResult.Success ? MigrationWorkItemStatuses.Succeeded : MigrationWorkItemStatuses.TargetFailed),
                TargetAssetId = migrationResult.TargetAssetId,
                CompletedUtc = DateTimeOffset.UtcNow,
                Message = migrationResult.Message,
                LastError = migrationResult.Success ? null : migrationResult.Message,
                Properties = CreateStateProperties(job, row, manifestFingerprint, executionMode)
            }, cancellationToken).ConfigureAwait(false);

            return new RowProcessResult(migrationResult, false, false);
        }
        catch (OperationCanceledException)
        {
            await _stateStore.SaveWorkItemAsync(new MigrationWorkItemState { RunId = runId, JobName = stateJobName, WorkItemId = workItemId, Status = MigrationWorkItemStatuses.Canceled, DryRun = job.DryRun, AttemptCount = attemptCount, CompletedUtc = DateTimeOffset.UtcNow, Message = "Canceled.", LastError = "Canceled.", Properties = CreateStateProperties(job, row, manifestFingerprint, executionMode) }, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Work item {WorkItemId} failed.", workItemId);
            await _stateStore.SaveWorkItemAsync(new MigrationWorkItemState { RunId = runId, JobName = stateJobName, WorkItemId = workItemId, Status = MigrationWorkItemStatuses.TargetFailed, DryRun = job.DryRun, AttemptCount = attemptCount, CompletedUtc = DateTimeOffset.UtcNow, Message = ex.Message, LastError = ex.Message, Properties = CreateStateProperties(job, row, manifestFingerprint, executionMode) }, cancellationToken).ConfigureAwait(false);
            return new RowProcessResult(new MigrationResult { WorkItemId = workItemId, Success = false, Message = ex.Message }, false, false);
        }
    }

    private static string GetStateWorkItemId(ManifestRow row) =>
        !string.IsNullOrWhiteSpace(row.SourceAssetId)
            ? row.SourceAssetId
            : row.RowId;

    private static Dictionary<string, string?> CreateStateProperties(MigrationJobDefinition job, ManifestRow row, string manifestFingerprint, string executionMode) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["OriginalJobName"] = job.JobName,
            ["ManifestFingerprint"] = manifestFingerprint,
            ["ExecutionMode"] = executionMode,
            ["ManifestPath"] = job.ManifestPath,
            ["ManifestRowId"] = row.RowId
        };

    private Task ReportAsync(string runId, MigrationJobDefinition job, string eventName, string? workItemId, int? completed, int? total, string? message, CancellationToken cancellationToken)
    {
        return _progressReporter.ReportAsync(new MigrationProgressEvent
        {
            RunId = runId,
            JobName = job.JobName,
            EventName = eventName,
            WorkItemId = workItemId,
            Completed = completed,
            Total = total,
            Message = message
        }, cancellationToken);
    }

    private static T ResolveSingle<T>(IEnumerable<T> values, Func<T, string> typeSelector, string requestedType, string serviceName)
    {
        var matches = values.Where(x => typeSelector(x).Equals(requestedType, StringComparison.OrdinalIgnoreCase)).ToList();
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException($"No {serviceName} registered for type '{requestedType}'."),
            _ => throw new InvalidOperationException($"Multiple {serviceName}s registered for type '{requestedType}'.")
        };
    }

    private static string CreateRunId(string jobName) => $"{Safe(jobName)}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

    private static string Safe(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '-');
        }
        return value;
    }

    private static MigrationResult WithWarnings(MigrationResult result, IReadOnlyList<string> warnings)
    {
        if (warnings.Count == 0)
        {
            return result;
        }

        var combinedWarnings = result.Warnings.Count == 0
            ? warnings
            : result.Warnings.Concat(warnings).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return new MigrationResult
        {
            WorkItemId = result.WorkItemId,
            Success = result.Success,
            TargetAssetId = result.TargetAssetId,
            Message = result.Message,
            Warnings = combinedWarnings
        };
    }

    private sealed record RowProcessResult(MigrationResult Result, bool Skipped, bool ValidationFailed);
}
