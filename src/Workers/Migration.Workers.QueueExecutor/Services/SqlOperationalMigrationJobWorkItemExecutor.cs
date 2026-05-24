using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Operational.WorkItems;
using Migration.Domain.Models;
using Migration.Orchestration.Abstractions;
using Migration.Workers.QueueExecutor.Options;

namespace Migration.Workers.QueueExecutor.Services;

public sealed class SqlOperationalMigrationJobWorkItemExecutor : ISqlOperationalWorkItemExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IMigrationJobRunner _runner;
    private readonly ProjectCredentialJobSettingsHydrator _credentialHydrator;
    private readonly IOptions<SqlOperationalMigrationJobExecutorOptions> _options;
    private readonly ILogger<SqlOperationalMigrationJobWorkItemExecutor> _logger;

    public SqlOperationalMigrationJobWorkItemExecutor(
        IMigrationJobRunner runner,
        ProjectCredentialJobSettingsHydrator credentialHydrator,
        IOptions<SqlOperationalMigrationJobExecutorOptions> options,
        ILogger<SqlOperationalMigrationJobWorkItemExecutor> logger)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _credentialHydrator = credentialHydrator ?? throw new ArgumentNullException(nameof(credentialHydrator));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SqlOperationalWorkItemExecutionResult> ExecuteAsync(
        OperationalWorkItemRecord workItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        var options = _options.Value;
        if (!options.Enabled)
        {
            return SqlOperationalWorkItemExecutionResult.TerminalFailure(
                "SQL_OPERATIONAL_JOB_EXECUTOR_DISABLED",
                "SqlOperationalMigrationJobExecutor is registered but disabled. Set SqlOperationalMigrationJobExecutor:Enabled=true to execute MigrationJobDefinition payloads.");
        }

        if (!SqlOperationalMigrationJobPayloadReader.TryReadJob(workItem.PayloadJson, out var job, out var payloadError) || job is null)
        {
            return SqlOperationalWorkItemExecutionResult.TerminalFailure(
                "SQL_OPERATIONAL_INVALID_JOB_PAYLOAD",
                payloadError ?? "SQL operational work item payload did not contain a valid MigrationJobDefinition.");
        }

        try
        {
            var contextualJob = options.AddOperationalContextSettings
                ? AddOperationalContextSettings(job, workItem)
                : job;

            var hydratedJob = await _credentialHydrator
                .HydrateAsync(contextualJob, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Executing SQL operational work item {WorkItemId} as migration job {JobName}. RunId={RunId}; ManifestRowId={ManifestRowId}; DryRun={DryRun}",
                workItem.WorkItemId,
                hydratedJob.JobName,
                workItem.RunId,
                workItem.ManifestRowId,
                hydratedJob.DryRun);

            var summary = await _runner.RunAsync(hydratedJob, cancellationToken).ConfigureAwait(false);
            var resultJson = SerializeResult(workItem, summary);

            if (summary.Failed == 0)
            {
                return SqlOperationalWorkItemExecutionResult.Success(resultJson);
            }

            var message = $"Migration job completed with failed work items. Total={summary.TotalWorkItems}; Succeeded={summary.Succeeded}; Failed={summary.Failed}; Skipped={summary.Skipped}.";
            if (options.TreatJobFailuresAsRetryable)
            {
                return SqlOperationalWorkItemExecutionResult.RetryableFailure(
                    "SQL_OPERATIONAL_JOB_COMPLETED_WITH_FAILURES",
                    message,
                    DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, options.RetryDelaySeconds)));
            }

            return SqlOperationalWorkItemExecutionResult.TerminalFailure(
                "SQL_OPERATIONAL_JOB_COMPLETED_WITH_FAILURES",
                message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SQL operational migration job work item {WorkItemId} failed before completion.",
                workItem.WorkItemId);

            if (options.TreatUnhandledExceptionsAsRetryable)
            {
                return SqlOperationalWorkItemExecutionResult.RetryableFailure(
                    ex.GetType().Name,
                    ex.Message,
                    DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, options.RetryDelaySeconds)));
            }

            return SqlOperationalWorkItemExecutionResult.TerminalFailure(
                ex.GetType().Name,
                ex.Message);
        }
    }

    private static MigrationJobDefinition AddOperationalContextSettings(
        MigrationJobDefinition job,
        OperationalWorkItemRecord workItem)
    {
        var settings = job.Settings is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(job.Settings, StringComparer.OrdinalIgnoreCase);

        settings["SqlOperationalRunId"] = workItem.RunId.ToString("D");
        settings["SqlOperationalWorkItemId"] = workItem.WorkItemId.ToString("D");
        settings["SqlOperationalWorkItemType"] = workItem.WorkItemType;
        settings["SqlOperationalAttemptCount"] = workItem.AttemptCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (workItem.ManifestRowId is not null)
        {
            settings["SqlOperationalManifestRowId"] = workItem.ManifestRowId.Value.ToString("D");
        }

        if (!string.IsNullOrWhiteSpace(workItem.PartitionKey))
        {
            settings["SqlOperationalPartitionKey"] = workItem.PartitionKey;
        }

        return new MigrationJobDefinition
        {
            JobName = job.JobName,
            SourceType = job.SourceType,
            TargetType = job.TargetType,
            ManifestType = job.ManifestType,
            MappingProfilePath = job.MappingProfilePath,
            ManifestPath = job.ManifestPath,
            ConnectionString = job.ConnectionString,
            QueryText = job.QueryText,
            Settings = settings,
            DryRun = job.DryRun,
            Parallelism = job.Parallelism
        };
    }

    private static string SerializeResult(
        OperationalWorkItemRecord workItem,
        MigrationRunSummary summary)
    {
        return JsonSerializer.Serialize(new
        {
            workItem.WorkItemId,
            workItem.RunId,
            workItem.ManifestRowId,
            summary.JobName,
            OrchestrationRunId = summary.RunId,
            summary.TotalWorkItems,
            summary.Succeeded,
            summary.Failed,
            summary.Skipped,
            Elapsed = summary.Elapsed.ToString("c"),
            CompletedUtc = DateTimeOffset.UtcNow
        }, JsonOptions);
    }
}
