using Migration.Domain.Models;
using Migration.Orchestration.Abstractions;

namespace Migration.Hosts.GenericMigration.Console.Infrastructure;

public sealed class GenericMigrationMenu
{
    private readonly IMigrationJobRunner _runner;
    private readonly IConnectorCatalog _catalog;
    private readonly JobDefinitionLoader _loader;
    private readonly IMigrationExecutionStateMaintenance? _stateMaintenance;

    public GenericMigrationMenu(
        IMigrationJobRunner runner,
        IConnectorCatalog catalog,
        JobDefinitionLoader loader,
        IEnumerable<IMigrationExecutionStateMaintenance> stateMaintenance)
    {
        _runner = runner;
        _catalog = catalog;
        _loader = loader;
        _stateMaintenance = stateMaintenance.FirstOrDefault();
    }

    public async Task RunAsync(string[] args)
    {
        if (args.Length > 0)
        {
            await RunJobAsync(args[0]).ConfigureAwait(false);
            return;
        }

        using var cts = new CancellationTokenSource();
        System.Console.CancelKeyPress += (_, e) =>
        {
            cts.Cancel();
            e.Cancel = true;
        };

        while (true)
        {
            System.Console.Clear();
            WriteHeader();
            System.Console.WriteLine("  1. Run configured/default migration job");
            System.Console.WriteLine("  2. Run a migration job file by path");
            System.Console.WriteLine("  3. Preflight/dry-run a migration job file");
            System.Console.WriteLine("  4. List registered source/target/manifest descriptors");
            System.Console.WriteLine("  5. Inspect job state");
            System.Console.WriteLine("  6. Reset job state");
            System.Console.WriteLine("  x. Exit");
            System.Console.WriteLine();
            System.Console.Write("  Select an option: ");

            var input = System.Console.ReadLine()?.Trim();
            if (string.Equals(input, "x", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                switch (input)
                {
                    case "1":
                        await RunJobAsync(_loader.GetDefaultJobFile(), cts.Token).ConfigureAwait(false);
                        break;

                    case "2":
                        await PromptAndRunJobAsync(preflight: false, cts.Token).ConfigureAwait(false);
                        break;

                    case "3":
                        await PromptAndRunJobAsync(preflight: true, cts.Token).ConfigureAwait(false);
                        break;

                    case "4":
                        WriteDescriptors();
                        break;

                    case "5":
                        await InspectStateAsync(cts.Token).ConfigureAwait(false);
                        break;

                    case "6":
                        await ResetStateAsync(cts.Token).ConfigureAwait(false);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                System.Console.WriteLine("Operation canceled.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"ERROR: {ex.Message}");
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Press any key to return to menu...");
            System.Console.ReadKey(true);
        }
    }

    private async Task PromptAndRunJobAsync(bool preflight, CancellationToken cancellationToken)
    {
        System.Console.Write("  Job file path: ");
        var path = System.Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (preflight)
        {
            await PreflightJobAsync(path, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await RunJobAsync(path, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunJobAsync(string jobFile, CancellationToken cancellationToken = default)
    {
        var job = await _loader.LoadAsync(jobFile, cancellationToken).ConfigureAwait(false);
        await RunLoadedJobAsync(job, jobFile, cancellationToken).ConfigureAwait(false);
    }

    private async Task PreflightJobAsync(string jobFile, CancellationToken cancellationToken = default)
    {
        var job = await _loader.LoadAsync(jobFile, cancellationToken).ConfigureAwait(false);
        var preflightJob = ToPreflightJob(job);

        System.Console.WriteLine();
        System.Console.WriteLine("Preflight mode forces DryRun=true and validates source, mapping, transforms, binary acquisition, and target metadata rules without writing to the target.");
        var summary = await RunLoadedJobAsync(preflightJob, jobFile, cancellationToken).ConfigureAwait(false);
        var reportPath = await PreflightReportWriter.WriteAsync(summary, cancellationToken: cancellationToken).ConfigureAwait(false);

        System.Console.WriteLine();
        System.Console.WriteLine($"Preflight report: {reportPath}");
    }

    private async Task<MigrationRunSummary> RunLoadedJobAsync(MigrationJobDefinition job, string jobFile, CancellationToken cancellationToken)
    {
        System.Console.WriteLine($"Running '{job.JobName}' from {jobFile}");
        System.Console.WriteLine($"Source={job.SourceType}; Target={job.TargetType}; Manifest={job.ManifestType}; DryRun={job.DryRun}; Parallelism={job.Parallelism}");

        var summary = await _runner.RunAsync(job, cancellationToken).ConfigureAwait(false);

        System.Console.WriteLine();
        WriteRunSummary(summary);
        return summary;
    }

    private static void WriteRunSummary(MigrationRunSummary summary)
    {
        var errored = summary.Results.Count(x => !x.Success);
        var warningRows = summary.Results.Count(x => x.Success && x.Warnings.Count > 0);
        var passed = summary.Results.Count(x => x.Success && x.Warnings.Count == 0);

        System.Console.WriteLine($"RunId: {summary.RunId}");
        System.Console.WriteLine($"Total={summary.TotalWorkItems}; Passed={passed}; Warnings={warningRows}; Failed={errored}; Skipped={summary.Skipped}; Elapsed={summary.Elapsed:g}");

        if (errored > 0)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Failed rows:");
            foreach (var result in summary.Results.Where(x => !x.Success).Take(25))
            {
                System.Console.WriteLine($"  ERROR {result.WorkItemId}: {result.Message}");
            }

            if (errored > 25)
            {
                System.Console.WriteLine($"  ... {errored - 25} more failed row(s)");
            }
        }

        if (warningRows > 0)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Warning rows:");
            foreach (var result in summary.Results.Where(x => x.Success && x.Warnings.Count > 0).Take(25))
            {
                System.Console.WriteLine($"  WARNING {result.WorkItemId}: {string.Join("; ", result.Warnings)}");
            }

            if (warningRows > 25)
            {
                System.Console.WriteLine($"  ... {warningRows - 25} more warning row(s)");
            }
        }
    }

    private async Task InspectStateAsync(CancellationToken cancellationToken)
    {
        if (_stateMaintenance is null)
        {
            System.Console.WriteLine("The configured state store does not support inspection.");
            return;
        }

        var jobName = PromptJobName();
        if (string.IsNullOrWhiteSpace(jobName))
        {
            return;
        }

        var states = await _stateMaintenance.ListWorkItemsAsync(jobName, cancellationToken).ConfigureAwait(false);
        System.Console.WriteLine($"State for '{jobName}': {states.Count} item(s)");

        foreach (var group in states.GroupBy(x => x.Status).OrderBy(x => x.Key))
        {
            System.Console.WriteLine($"  {group.Key}: {group.Count()}");
        }

        foreach (var state in states.Take(25))
        {
            System.Console.WriteLine($"  {state.WorkItemId}: {state.Status}; Target={state.TargetAssetId}; Message={state.Message}");
        }

        if (states.Count > 25)
        {
            System.Console.WriteLine($"  ... {states.Count - 25} more item(s)");
        }
    }

    private async Task ResetStateAsync(CancellationToken cancellationToken)
    {
        if (_stateMaintenance is null)
        {
            System.Console.WriteLine("The configured state store does not support reset.");
            return;
        }

        var jobName = PromptJobName();
        if (string.IsNullOrWhiteSpace(jobName))
        {
            return;
        }

        System.Console.Write($"Type RESET to clear state for '{jobName}': ");
        if (!string.Equals(System.Console.ReadLine(), "RESET", StringComparison.Ordinal))
        {
            System.Console.WriteLine("Reset canceled.");
            return;
        }

        await _stateMaintenance.ResetJobAsync(jobName, cancellationToken).ConfigureAwait(false);
        System.Console.WriteLine("State reset complete.");
    }

    private static MigrationJobDefinition ToPreflightJob(MigrationJobDefinition job)
    {
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
            Settings = new Dictionary<string, string?>(job.Settings, StringComparer.OrdinalIgnoreCase),
            DryRun = true,
            Parallelism = job.Parallelism
        };
    }

    private static string? PromptJobName()
    {
        System.Console.Write("  Job name: ");
        return System.Console.ReadLine()?.Trim();
    }

    private void WriteDescriptors()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Sources:");
        foreach (var source in _catalog.GetSources())
        {
            WriteConnector(source);
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Targets:");
        foreach (var target in _catalog.GetTargets())
        {
            WriteConnector(target);
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Manifest providers:");
        foreach (var manifest in _catalog.GetManifestProviders())
        {
            System.Console.WriteLine($"  - {manifest.Type}: {manifest.DisplayName}");
            foreach (var option in manifest.Options)
            {
                System.Console.WriteLine($"      option: {option.Name}; required={option.Required}; default={option.DefaultValue}");
            }
        }
    }

    private static void WriteConnector(Migration.Orchestration.Descriptors.ConnectorDescriptor descriptor)
    {
        System.Console.WriteLine($"  - {descriptor.Type}: {descriptor.DisplayName}");
        if (!string.IsNullOrWhiteSpace(descriptor.Description))
        {
            System.Console.WriteLine($"      {descriptor.Description}");
        }

        foreach (var credential in descriptor.Credentials)
        {
            System.Console.WriteLine($"      credential: {credential.Name}; required={credential.Required}; key={credential.ConfigurationKey}");
        }

        foreach (var option in descriptor.Options)
        {
            var allowed = option.AllowedValues.Count > 0 ? $"; allowed={string.Join("|", option.AllowedValues)}" : string.Empty;
            System.Console.WriteLine($"      option: {option.Name}; required={option.Required}; default={option.DefaultValue}{allowed}");
        }

        foreach (var metadata in descriptor.Metadata)
        {
            System.Console.WriteLine($"      {metadata.Key}: {metadata.Value}");
        }
    }

    private static void WriteHeader()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("  =====================================================");
        System.Console.WriteLine("  |        Generic Migration Orchestrator Console      |");
        System.Console.WriteLine("  =====================================================");
        System.Console.WriteLine();
    }
}
