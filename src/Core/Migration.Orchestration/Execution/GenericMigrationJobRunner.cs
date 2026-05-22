using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
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

        if (IsSharePointRcloneDirectCopy(job))
        {
            return await RunSharePointRcloneDirectCopyAsync(job, cancellationToken).ConfigureAwait(false);
        }

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
                await ReportAsync(runId, job, itemResult.Result.Success ? MigrationProgressEvents.WorkItemSucceeded : MigrationProgressEvents.WorkItemFailed, itemResult.Result.WorkItemId, done, total, itemResult.Result.Message, token).ConfigureAwait(false);
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

            await ReportAsync(runId, job, MigrationProgressEvents.RunCompleted, null, completed, total, $"Completed in {elapsed:g}. Succeeded={succeeded}; Failed={failed}; ValidationFailed={validationFailed}; Skipped={skipped}.", CancellationToken.None).ConfigureAwait(false);
        }

        var ordered = results.OrderBy(x => x.WorkItemId, StringComparer.OrdinalIgnoreCase).ToList();
        return new MigrationRunSummary(runId, job.JobName, total, succeeded, failed, skipped, DateTimeOffset.UtcNow - started, ordered);
    }

    private async Task<MigrationRunSummary> RunSharePointRcloneDirectCopyAsync(MigrationJobDefinition job, CancellationToken cancellationToken)
    {
        var runId = CreateRunId(job.JobName);
        var stateJobName = JobStateKeyFactory.BuildStateJobName(job);
        var started = DateTimeOffset.UtcNow;
        var options = await SharePointRcloneDirectCopyOptions.FromAsync(job, cancellationToken).ConfigureAwait(false);
        var source = BuildRemotePath(options.RemoteName, options.SourcePath);
        var destination = options.TargetIsLocalStorage
            ? Path.GetFullPath(options.DestinationPath)
            : BuildRemotePath(options.TargetRemoteName, options.DestinationPath);
        var workItemId = "sharepoint-rclone-copy";

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["RunId"] = runId,
            ["JobName"] = job.JobName,
            ["SourceType"] = job.SourceType,
            ["TargetType"] = job.TargetType,
            ["DryRun"] = job.DryRun,
            ["StateJobName"] = stateJobName,
            ["SharePointRcloneSource"] = source,
            ["LocalOutputPath"] = destination
        });

        await ReportAsync(runId, job, MigrationProgressEvents.RunStarted, null, null, 1, $"Starting SharePoint rclone direct copy from '{source}' to '{destination}'.", cancellationToken).ConfigureAwait(false);

        await _stateStore.StartRunAsync(new MigrationRunRecord
        {
            RunId = runId,
            JobName = stateJobName,
            SourceType = job.SourceType,
            TargetType = job.TargetType,
            DryRun = job.DryRun,
            StartedUtc = started,
            Status = "Running",
            TotalWorkItems = 1
        }, cancellationToken).ConfigureAwait(false);

        await _stateStore.SaveWorkItemAsync(new MigrationWorkItemState
        {
            RunId = runId,
            JobName = stateJobName,
            WorkItemId = workItemId,
            SourceAssetId = source,
            Status = MigrationWorkItemStatuses.Running,
            DryRun = job.DryRun,
            StartedUtc = DateTimeOffset.UtcNow,
            AttemptCount = 1,
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["OriginalJobName"] = job.JobName,
                ["ExecutionMode"] = job.DryRun ? "DryRun" : "Live",
                ["SharePointRcloneSource"] = source,
                ["LocalOutputPath"] = destination,
                ["ManifestBypassed"] = "true"
            }
        }, cancellationToken).ConfigureAwait(false);

        try
        {
            if (options.TargetIsLocalStorage)
            {
                Directory.CreateDirectory(destination);
            }
            await ReportAsync(runId, job, MigrationProgressEvents.WorkItemStarted, workItemId, 0, 1, "Executing rclone copy. Manifest rows are bypassed for SharePoint rclone direct runs.", cancellationToken).ConfigureAwait(false);

            var arguments = new List<string>
            {
                "copy",
                QuoteArgument(source),
                QuoteArgument(destination),
                "--create-empty-src-dirs"
            };

            if (job.DryRun)
            {
                arguments.Add("--dry-run");
            }

            if (!string.IsNullOrWhiteSpace(options.ConfigPath))
            {
                arguments.Add("--config");
                arguments.Add(QuoteArgument(options.ConfigPath));
            }

            if (options.UseJsonLog)
            {
                arguments.Add("--use-json-log");
            }

            await RunProcessAsync(options.ExecutablePath, string.Join(" ", arguments), cancellationToken).ConfigureAwait(false);

            var message = job.DryRun
                ? $"Dry run completed. rclone copy would copy '{source}' to '{destination}'."
                : $"SharePoint rclone copy completed. Source='{source}'; Destination='{destination}'.";

            var result = new MigrationResult
            {
                WorkItemId = workItemId,
                Success = true,
                TargetAssetId = destination,
                Message = message
            };

            await _stateStore.SaveWorkItemAsync(new MigrationWorkItemState
            {
                RunId = runId,
                JobName = stateJobName,
                WorkItemId = workItemId,
                SourceAssetId = source,
                TargetAssetId = destination,
                Status = job.DryRun ? MigrationWorkItemStatuses.DryRunSucceeded : MigrationWorkItemStatuses.Succeeded,
                DryRun = job.DryRun,
                CompletedUtc = DateTimeOffset.UtcNow,
                AttemptCount = 1,
                Message = message,
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["OriginalJobName"] = job.JobName,
                    ["ExecutionMode"] = job.DryRun ? "DryRun" : "Live",
                    ["SharePointRcloneSource"] = source,
                    ["LocalOutputPath"] = destination,
                    ["ManifestBypassed"] = "true"
                }
            }, cancellationToken).ConfigureAwait(false);

            await _stateStore.CompleteRunAsync(new MigrationRunRecord
            {
                RunId = runId,
                JobName = stateJobName,
                SourceType = job.SourceType,
                TargetType = job.TargetType,
                DryRun = job.DryRun,
                StartedUtc = started,
                CompletedUtc = DateTimeOffset.UtcNow,
                Status = job.DryRun ? "DryRunSucceeded" : "Succeeded",
                TotalWorkItems = 1,
                Succeeded = 1,
                Failed = 0,
                Skipped = 0,
                ValidationFailed = 0
            }, cancellationToken).ConfigureAwait(false);

            await ReportAsync(runId, job, MigrationProgressEvents.WorkItemSucceeded, workItemId, 1, 1, message, cancellationToken).ConfigureAwait(false);
            await ReportAsync(runId, job, MigrationProgressEvents.RunCompleted, null, 1, 1, message, cancellationToken).ConfigureAwait(false);

            return new MigrationRunSummary(runId, job.JobName, 1, 1, 0, 0, DateTimeOffset.UtcNow - started, new[] { result });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "SharePoint rclone direct copy failed.");

            var result = new MigrationResult
            {
                WorkItemId = workItemId,
                Success = false,
                Message = ex.Message
            };

            await _stateStore.SaveWorkItemAsync(new MigrationWorkItemState
            {
                RunId = runId,
                JobName = stateJobName,
                WorkItemId = workItemId,
                SourceAssetId = source,
                Status = MigrationWorkItemStatuses.TargetFailed,
                DryRun = job.DryRun,
                CompletedUtc = DateTimeOffset.UtcNow,
                AttemptCount = 1,
                Message = ex.Message,
                LastError = ex.Message,
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["OriginalJobName"] = job.JobName,
                    ["ExecutionMode"] = job.DryRun ? "DryRun" : "Live",
                    ["SharePointRcloneSource"] = source,
                    ["LocalOutputPath"] = destination,
                    ["ManifestBypassed"] = "true"
                }
            }, CancellationToken.None).ConfigureAwait(false);

            await _stateStore.CompleteRunAsync(new MigrationRunRecord
            {
                RunId = runId,
                JobName = stateJobName,
                SourceType = job.SourceType,
                TargetType = job.TargetType,
                DryRun = job.DryRun,
                StartedUtc = started,
                CompletedUtc = DateTimeOffset.UtcNow,
                Status = "CompletedWithFailures",
                TotalWorkItems = 1,
                Succeeded = 0,
                Failed = 1,
                Skipped = 0,
                ValidationFailed = 0
            }, CancellationToken.None).ConfigureAwait(false);

            await ReportAsync(runId, job, MigrationProgressEvents.WorkItemFailed, workItemId, 1, 1, ex.Message, CancellationToken.None).ConfigureAwait(false);
            await ReportAsync(runId, job, MigrationProgressEvents.RunCompleted, null, 1, 1, $"SharePoint rclone direct copy failed: {ex.Message}", CancellationToken.None).ConfigureAwait(false);

            return new MigrationRunSummary(runId, job.JobName, 1, 0, 1, 0, DateTimeOffset.UtcNow - started, new[] { result });
        }
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
                var skipped = new MigrationResult
                {
                    WorkItemId = workItemId,
                    Success = true,
                    TargetAssetId = existingState.TargetAssetId,
                    Message = "Skipped; work item already succeeded in real-run state store."
                };

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
            await _stateStore.SaveWorkItemAsync(new MigrationWorkItemState
            {
                RunId = runId,
                JobName = stateJobName,
                WorkItemId = workItemId,
                Status = MigrationWorkItemStatuses.Canceled,
                DryRun = job.DryRun,
                AttemptCount = attemptCount,
                CompletedUtc = DateTimeOffset.UtcNow,
                Message = "Canceled.",
                LastError = "Canceled.",
                Properties = CreateStateProperties(job, row, manifestFingerprint, executionMode)
            }, CancellationToken.None).ConfigureAwait(false);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Work item {WorkItemId} failed.", workItemId);

            await _stateStore.SaveWorkItemAsync(new MigrationWorkItemState
            {
                RunId = runId,
                JobName = stateJobName,
                WorkItemId = workItemId,
                Status = MigrationWorkItemStatuses.TargetFailed,
                DryRun = job.DryRun,
                AttemptCount = attemptCount,
                CompletedUtc = DateTimeOffset.UtcNow,
                Message = ex.Message,
                LastError = ex.Message,
                Properties = CreateStateProperties(job, row, manifestFingerprint, executionMode)
            }, cancellationToken).ConfigureAwait(false);

            return new RowProcessResult(new MigrationResult
            {
                WorkItemId = workItemId,
                Success = false,
                Message = ex.Message
            }, false, false);
        }
    }

    private static bool IsSharePointRcloneDirectCopy(MigrationJobDefinition job)
    {
        if (!job.SourceType.Equals("SharePoint", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var mode = GetSetting(job.Settings, "SharePointMode", "SourceBinaryMode", "sourceService", "service") ?? "Rclone";
        if (!mode.Equals("Rclone", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return job.TargetType.Equals("LocalStorage", StringComparison.OrdinalIgnoreCase)
            || job.TargetType.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase)
            || job.TargetType.Equals("Azure", StringComparison.OrdinalIgnoreCase)
            || job.TargetType.Equals("S3", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Unable to start process '{fileName}'.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"rclone failed with exit code {process.ExitCode}. {stderr}{Environment.NewLine}{stdout}".Trim());
        }
    }

    private static string BuildRemotePath(string remoteName, string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            throw new InvalidOperationException("SharePoint rclone direct run requires a remote name. Configure it on the selected SharePoint source credentials as remoteName or rcloneRemoteName.");
        }

        var remote = remoteName.Trim().TrimEnd(':');
        var path = NormalizeRemotePath(sourcePath);

        return string.IsNullOrWhiteSpace(path) ? $"{remote}:" : $"{remote}:{path}";
    }

    private static string NormalizeRemotePath(string? path) => (path ?? string.Empty).Replace('\\', '/').Trim('/');

    private static string QuoteArgument(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

    private static string? GetSetting(IReadOnlyDictionary<string, string> settings, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string GetStateWorkItemId(ManifestRow row) => !string.IsNullOrWhiteSpace(row.SourceAssetId) ? row.SourceAssetId : row.RowId;

    private static Dictionary<string, string> CreateStateProperties(MigrationJobDefinition job, ManifestRow row, string manifestFingerprint, string executionMode) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["OriginalJobName"] = job.JobName,
        ["ManifestFingerprint"] = manifestFingerprint,
        ["ExecutionMode"] = executionMode,
        ["ManifestPath"] = job.ManifestPath ?? string.Empty,
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

    private sealed record SharePointRcloneDirectCopyOptions(
        string ExecutablePath,
        string? ConfigPath,
        string RemoteName,
        string? SourcePath,
        string DestinationPath,
        string? TargetRemoteName,
        bool TargetIsLocalStorage,
        bool UseJsonLog)
    {
        public static async Task<SharePointRcloneDirectCopyOptions> FromAsync(MigrationJobDefinition job, CancellationToken cancellationToken)
        {
            var mappingValues = await ReadMappingValuesAsync(job.MappingProfilePath, cancellationToken).ConfigureAwait(false);

            var executablePath = GetFirst(job.Settings, mappingValues,
                    "rcloneExecutablePath", "RcloneExecutablePath", "rclonePath", "RclonePath", "executablePath", "ExecutablePath",
                    "SourceCredential_RcloneExecutablePath", "SourceRcloneExecutablePath", "SharePointRcloneExecutablePath",
                    "TargetCredential_RcloneExecutablePath", "TargetRcloneExecutablePath")
                ?? "rclone";

            var configPath = GetFirst(job.Settings, mappingValues,
                "rcloneConfigPath", "RcloneConfigPath", "configPath", "ConfigPath", "configurationPath", "ConfigurationPath",
                "SourceCredential_RcloneConfigPath", "SourceRcloneConfigPath", "SharePointRcloneConfigPath",
                "TargetCredential_RcloneConfigPath", "TargetRcloneConfigPath", "AzureBlobRcloneConfigPath", "S3RcloneConfigPath");

            var remoteName = GetFirst(job.Settings, mappingValues,
                    "remoteName", "RemoteName", "rcloneRemoteName", "RcloneRemoteName", "remote", "Remote",
                    "SourceCredential_RcloneRemoteName", "SourceRcloneRemoteName", "SharePointRcloneRemoteName",
                    "SourceCredential_RemoteName", "SourceRemoteName", "SharePointRemoteName")
                ?? string.Empty;

            var sourcePath = GetFirst(job.Settings, mappingValues,
                "sourcePath", "SourcePath", "SharePointRootPath", "sharePointRootPath", "rcloneRootPath", "RcloneRootPath", "rootPath", "RootPath", "sourceFolderPath", "SourceFolderPath",
                "SourceCredential_SourcePath", "SourceRootPath", "SharePointSourcePath");

            var destinationPath = GetFirst(job.Settings, mappingValues,
                "outputPath", "OutputPath",
                "folderPath", "FolderPath",
                "destinationPath", "DestinationPath",
                "targetPath", "TargetPath",
                "targetFolderPath", "TargetFolderPath",
                "intermediateStorage.outputPath", "IntermediateStorage.OutputPath", "intermediateStorage:outputPath", "IntermediateStorage:OutputPath",
                "intermediateStorage.folderPath", "IntermediateStorage.FolderPath", "intermediateStorage:folderPath", "IntermediateStorage:FolderPath",
                "localStorage.outputPath", "LocalStorage.OutputPath", "localStorage:outputPath", "LocalStorage:OutputPath",
                "localOutputPath", "LocalOutputPath",
                "localStorageOutputPath", "LocalStorageOutputPath",
                "localStorageRootPath", "LocalStorageRootPath",
                "localStoragePath", "LocalStoragePath",
                "azureBlobFolderPath", "AzureBlobFolderPath", "azureFolderPath", "AzureFolderPath", "blobFolderPath", "BlobFolderPath",
                "s3FolderPath", "S3FolderPath", "s3Prefix", "S3Prefix", "objectKeyPrefix", "ObjectKeyPrefix",
                "targetRootPath", "TargetRootPath",
                "basePath", "BasePath");

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                var knownKeys = mappingValues.Count == 0
                    ? "no scalar values were read from the mapping profile"
                    : string.Join(", ", mappingValues.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Take(60));

                throw new InvalidOperationException($"SharePoint rclone direct run requires a destination folder/path. Add outputPath or folderPath to the mapping profile intermediateStorage object or project/run settings. MappingProfilePath='{job.MappingProfilePath}'. Read mapping keys: {knownKeys}.");
            }

            var targetIsLocalStorage = job.TargetType.Equals("LocalStorage", StringComparison.OrdinalIgnoreCase);
            var targetRemoteName = targetIsLocalStorage
                ? null
                : GetFirst(job.Settings, mappingValues,
                    "targetRemoteName", "TargetRemoteName", "targetRcloneRemoteName", "TargetRcloneRemoteName",
                    "TargetCredential_RcloneRemoteName", "TargetRcloneRemoteName",
                    "intermediateStorage.targetRcloneRemoteName", "IntermediateStorage.TargetRcloneRemoteName",
                    "intermediateStorage:targetRcloneRemoteName", "IntermediateStorage:TargetRcloneRemoteName",
                    "AzureBlobRcloneRemoteName", "AzureRcloneRemoteName", "S3RcloneRemoteName",
                    "TargetCredential_RemoteName", "TargetRemote", "TargetRemote");

            if (!targetIsLocalStorage && string.IsNullOrWhiteSpace(targetRemoteName))
            {
                throw new InvalidOperationException($"SharePoint rclone direct run requires a target rclone remote name for target '{job.TargetType}'. Add targetRcloneRemoteName to the mapping profile intermediateStorage object.");
            }

            var useJsonLog = bool.TryParse(GetFirst(job.Settings, mappingValues, "useJsonLog", "UseJsonLog", "rcloneUseJsonLog", "RcloneUseJsonLog"), out var parsedUseJsonLog)
                && parsedUseJsonLog;

            return new SharePointRcloneDirectCopyOptions(
                executablePath.Trim(),
                string.IsNullOrWhiteSpace(configPath) ? null : configPath.Trim(),
                remoteName.Trim(),
                string.IsNullOrWhiteSpace(sourcePath) ? null : sourcePath.Trim(),
                destinationPath.Trim(),
                string.IsNullOrWhiteSpace(targetRemoteName) ? null : targetRemoteName.Trim(),
                targetIsLocalStorage,
                useJsonLog);
        }

        private static string? GetFirst(IReadOnlyDictionary<string, string> settings, IReadOnlyDictionary<string, string> mappingValues, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (settings.TryGetValue(key, out var settingValue) && !string.IsNullOrWhiteSpace(settingValue))
                {
                    return settingValue.Trim();
                }

                if (mappingValues.TryGetValue(key, out var mappingValue) && !string.IsNullOrWhiteSpace(mappingValue))
                {
                    return mappingValue.Trim();
                }
            }

            return null;
        }

        private static async Task<IReadOnlyDictionary<string, string>> ReadMappingValuesAsync(string mappingProfilePath, CancellationToken cancellationToken)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(mappingProfilePath) || !File.Exists(mappingProfilePath))
            {
                return values;
            }

            var json = await File.ReadAllTextAsync(mappingProfilePath, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
            {
                return values;
            }

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Capture specific LocalStorage/direct-run values first so similarly named source fields
            // elsewhere in the profile cannot accidentally win.
            CollectPreferredIntermediateStorageValues(root, values);

            // Then capture scalar values from the full profile as broad fallbacks. This preserves
            // compatibility with existing profile/project/run setting shapes without requiring a
            // separate DTO just for this direct-transfer path.
            CollectScalarValues(root, values);

            return values;
        }

        private static void CollectPreferredIntermediateStorageValues(JsonElement root, IDictionary<string, string> values)
        {
            if (!TryFindObject(root, "intermediateStorage", out var intermediateStorage)
                && !TryFindObject(root, "intermediateStorageBehavior", out intermediateStorage)
                && !TryFindObject(root, "storageBehavior", out intermediateStorage))
            {
                return;
            }

            CopyNamedValue(intermediateStorage, values, "provider");
            CopyNamedValue(intermediateStorage, values, "outputPath");
            CopyNamedValue(intermediateStorage, values, "folderPath");
            CopyNamedValue(intermediateStorage, values, "destinationPath");
            CopyNamedValue(intermediateStorage, values, "targetFolderPath");
            CopyNamedValue(intermediateStorage, values, "localOutputPath");
            CopyNamedValue(intermediateStorage, values, "localStorageOutputPath");
            CopyNamedValue(intermediateStorage, values, "localStorageRootPath");
            CopyNamedValue(intermediateStorage, values, "rootPath");
            CopyNamedValue(intermediateStorage, values, "basePath");
            CopyNamedValue(intermediateStorage, values, "destinationPath");
            CopyNamedValue(intermediateStorage, values, "targetPath");
            CopyNamedValue(intermediateStorage, values, "targetRcloneRemoteName");
            CopyNamedValue(intermediateStorage, values, "targetRemoteName");

            if (TryFindObject(intermediateStorage, "localStorage", out var localStorage)
                || TryFindObject(intermediateStorage, "localstorage", out localStorage)
                || TryFindObject(intermediateStorage, "local", out localStorage))
            {
                CopyNamedValue(localStorage, values, "outputPath");
                CopyNamedValue(localStorage, values, "folderPath");
                CopyNamedValue(localStorage, values, "destinationPath");
                CopyNamedValue(localStorage, values, "localOutputPath");
                CopyNamedValue(localStorage, values, "rootPath");
                CopyNamedValue(localStorage, values, "basePath");
                CopyNamedValue(localStorage, values, "destinationPath");
                CopyNamedValue(localStorage, values, "targetPath");
            }

            if (TryFindObject(intermediateStorage, "azureBlob", out var azureBlob)
                || TryFindObject(intermediateStorage, "azure", out azureBlob)
                || TryFindObject(intermediateStorage, "s3", out azureBlob))
            {
                CopyNamedValue(azureBlob, values, "outputPath");
                CopyNamedValue(azureBlob, values, "folderPath");
                CopyNamedValue(azureBlob, values, "destinationPath");
                CopyNamedValue(azureBlob, values, "targetPath");
                CopyNamedValue(azureBlob, values, "basePath");
            }
        }

        private static bool TryFindObject(JsonElement element, string propertyName, out JsonElement found)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)
                        && property.Value.ValueKind == JsonValueKind.Object)
                    {
                        found = property.Value;
                        return true;
                    }

                    if (TryFindObject(property.Value, propertyName, out found))
                    {
                        return true;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (TryFindObject(item, propertyName, out found))
                    {
                        return true;
                    }
                }
            }

            found = default;
            return false;
        }

        private static void CopyNamedValue(JsonElement obj, IDictionary<string, string> values, string propertyName)
        {
            if (obj.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (var property in obj.EnumerateObject())
            {
                if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = ToScalarString(property.Value);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values[propertyName] = value.Trim();
                    values[property.Name] = value.Trim();
                }

                return;
            }
        }

        private static void CollectScalarValues(JsonElement element, IDictionary<string, string> values)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                CollectKeyValueShape(element, values);

                foreach (var property in element.EnumerateObject())
                {
                    var value = ToScalarString(property.Value);
                    if (!string.IsNullOrWhiteSpace(value) && !values.ContainsKey(property.Name))
                    {
                        values[property.Name] = value.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(value) && LooksLikeJson(value))
                    {
                        CollectJsonStringValues(value, values);
                    }

                    CollectScalarValues(property.Value, values);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    CollectScalarValues(item, values);
                }
            }
        }


        private static void CollectKeyValueShape(JsonElement obj, IDictionary<string, string> values)
        {
            if (obj.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            var key = GetFirstPropertyValue(obj, "key", "name", "property", "setting", "option", "field");
            var value = GetFirstPropertyValue(obj, "value", "text", "settingValue", "optionValue");

            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value) && !values.ContainsKey(key))
            {
                values[key.Trim()] = value.Trim();
            }
        }

        private static string? GetFirstPropertyValue(JsonElement obj, params string[] names)
        {
            foreach (var property in obj.EnumerateObject())
            {
                if (names.Any(name => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    var value = ToScalarString(property.Value);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value.Trim();
                    }
                }
            }

            return null;
        }

        private static bool LooksLikeJson(string value)
        {
            var trimmed = value.TrimStart();
            return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
        }

        private static void CollectJsonStringValues(string value, IDictionary<string, string> values)
        {
            try
            {
                using var nested = JsonDocument.Parse(value);
                CollectPreferredIntermediateStorageValues(nested.RootElement, values);
                CollectScalarValues(nested.RootElement, values);
            }
            catch (JsonException)
            {
                // Not actually JSON despite looking like JSON. Ignore and continue.
            }
        }

        private static string? ToScalarString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                JsonValueKind.Number when element.TryGetDouble(out var doubleValue) => doubleValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => null
            };
        }
    }
}
