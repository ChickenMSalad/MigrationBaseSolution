using System.Text.Json;
using System.Text.Json.Serialization;
using Migration.Domain.Models;
using Migration.Orchestration.Abstractions;

namespace Migration.Hosts.GenericMigration.Console.Infrastructure;

public static class PreflightReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<string> WriteAsync(MigrationRunSummary summary, string? outputDirectory = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var directory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Runtime", "PreflightReports")
            : outputDirectory;

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{SafeFileName(summary.JobName)}-{SafeFileName(summary.RunId)}.preflight-results.json");
        var report = CreateReport(summary);

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, report, JsonOptions, cancellationToken).ConfigureAwait(false);
        return path;
    }

    public static PreflightReport CreateReport(MigrationRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var rows = summary.Results
            .OrderBy(x => x.WorkItemId, StringComparer.OrdinalIgnoreCase)
            .Select(CreateRow)
            .ToList();

        return new PreflightReport
        {
            GeneratedUtc = DateTimeOffset.UtcNow,
            RunId = summary.RunId,
            JobName = summary.JobName,
            Total = summary.TotalWorkItems,
            Passed = rows.Count(x => x.Severity == PreflightSeverity.Info),
            Warnings = rows.Count(x => x.Severity == PreflightSeverity.Warning),
            Failed = rows.Count(x => x.Severity == PreflightSeverity.Error),
            Skipped = summary.Skipped,
            Elapsed = summary.Elapsed,
            Rows = rows
        };
    }

    private static PreflightReportRow CreateRow(MigrationResult result)
    {
        var severity = result.Success
            ? result.Warnings.Count > 0 ? PreflightSeverity.Warning : PreflightSeverity.Info
            : PreflightSeverity.Error;

        return new PreflightReportRow
        {
            WorkItemId = result.WorkItemId,
            Success = result.Success,
            Severity = severity,
            TargetAssetId = result.TargetAssetId,
            Message = result.Message,
            Warnings = result.Warnings.ToList()
        };
    }

    private static string SafeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '-');
        }

        return value;
    }
}

public sealed class PreflightReport
{
    public DateTimeOffset GeneratedUtc { get; init; }
    public string RunId { get; init; } = string.Empty;
    public string JobName { get; init; } = string.Empty;
    public int Total { get; init; }
    public int Passed { get; init; }
    public int Warnings { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
    public TimeSpan Elapsed { get; init; }
    public IReadOnlyList<PreflightReportRow> Rows { get; init; } = Array.Empty<PreflightReportRow>();
}

public sealed class PreflightReportRow
{
    public string WorkItemId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public PreflightSeverity Severity { get; init; }
    public string? TargetAssetId { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public enum PreflightSeverity
{
    Info,
    Warning,
    Error
}
