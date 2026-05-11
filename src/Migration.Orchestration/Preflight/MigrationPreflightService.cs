using Microsoft.Extensions.Logging;
using Migration.Application.Abstractions;
using Migration.Application.Models;
using Migration.Domain.Enums;
using Migration.Domain.Models;

namespace Migration.Orchestration.Preflight;

public sealed class MigrationPreflightService : IMigrationPreflightService
{
    private static readonly HashSet<string> KnownTransforms = new(StringComparer.OrdinalIgnoreCase)
    {
        "trim", "lower", "lowercase", "to-lower", "tolower",
        "upper", "uppercase", "to-upper", "toupper",
        "split:semicolon", "splitsemicolon", "split-semi-colon", "split-semi",
        "split:comma", "splitcomma", "split:pipe", "splitpipe", "split:newline", "splitnewline", "split:line", "splitline",
        "normalize-date", "normalizedate", "date:yyyy-mm-dd", "normalize-datetime", "normalizedatetime", "date:o",
        "boolean", "bool", "to-bool", "tobool",
        "integer", "int", "to-int", "toint",
        "decimal", "number", "to-decimal", "todecimal",
        "empty-to-null", "emptytonull", "null-if-empty", "nullifempty"
    };

    private readonly IEnumerable<IManifestProvider> _manifestProviders;
    private readonly IEnumerable<IAssetSourceConnector> _sourceConnectors;
    private readonly IMappingProfileLoader _mappingProfileLoader;
    private readonly IMapper _mapper;
    private readonly IEnumerable<ITransformStep> _transformSteps;
    private readonly IEnumerable<IValidationStep> _validationSteps;
    private readonly ILogger<MigrationPreflightService> _logger;

    public MigrationPreflightService(
        IEnumerable<IManifestProvider> manifestProviders,
        IEnumerable<IAssetSourceConnector> sourceConnectors,
        IMappingProfileLoader mappingProfileLoader,
        IMapper mapper,
        IEnumerable<ITransformStep> transformSteps,
        IEnumerable<IValidationStep> validationSteps,
        ILogger<MigrationPreflightService> logger)
    {
        _manifestProviders = manifestProviders;
        _sourceConnectors = sourceConnectors;
        _mappingProfileLoader = mappingProfileLoader;
        _mapper = mapper;
        _transformSteps = transformSteps;
        _validationSteps = validationSteps;
        _logger = logger;
    }

    public async Task<PreflightResult> RunAsync(PreflightRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Job);

        var started = DateTimeOffset.UtcNow;
        var issues = new List<PreflightIssue>();
        var details = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var preflightId = $"preflight-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";

        try
        {
            var job = request.Job;
            var manifestProvider = ResolveSingle(_manifestProviders, x => x.Type, job.ManifestType, "manifest provider");
            var profile = await _mappingProfileLoader.LoadAsync(job.MappingProfilePath, cancellationToken).ConfigureAwait(false);
            var rows = await manifestProvider.ReadAsync(job, cancellationToken).ConfigureAwait(false);

            details["manifestType"] = job.ManifestType;
            details["manifestPath"] = job.ManifestPath;
            details["mappingProfilePath"] = job.MappingProfilePath;
            details["sourceType"] = job.SourceType;
            details["targetType"] = job.TargetType;
            details["mappingProfileName"] = profile.ProfileName;

            ValidateProfileCompatibility(job, profile, issues);
            ValidateManifestShape(rows, issues);
            ValidateMapping(profile, rows, issues);
            await ValidateRowsAsync(job, profile, rows, request, issues, cancellationToken).ConfigureAwait(false);

            var checkedRows = request.MaxRows <= 0 ? rows.Count : Math.Min(rows.Count, request.MaxRows);
            var summary = BuildSummary(rows.Count, checkedRows, issues);

            return new PreflightResult
            {
                PreflightId = preflightId,
                ProjectId = request.ProjectId,
                JobName = job.JobName,
                Status = summary.ErrorCount > 0 ? PreflightStatuses.Failed : summary.WarningCount > 0 ? PreflightStatuses.Warning : PreflightStatuses.Passed,
                StartedUtc = started,
                CompletedUtc = DateTimeOffset.UtcNow,
                Summary = summary,
                Issues = issues,
                Details = details
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preflight {PreflightId} failed before validation completed.", preflightId);
            issues.Add(new PreflightIssue
            {
                Severity = PreflightSeverities.Error,
                Code = "preflight.exception",
                Message = ex.Message
            });

            return new PreflightResult
            {
                PreflightId = preflightId,
                ProjectId = request.ProjectId,
                JobName = request.Job.JobName,
                Status = PreflightStatuses.Failed,
                StartedUtc = started,
                CompletedUtc = DateTimeOffset.UtcNow,
                Summary = BuildSummary(0, 0, issues),
                Issues = issues,
                Details = details
            };
        }
    }

    private void ValidateProfileCompatibility(MigrationJobDefinition job, MappingProfile profile, List<PreflightIssue> issues)
    {
        if (!profile.SourceType.Equals(job.SourceType, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("mapping.sourceType.mismatch", $"Mapping sourceType '{profile.SourceType}' does not match job sourceType '{job.SourceType}'.", "sourceType"));
        }

        if (!profile.TargetType.Equals(job.TargetType, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error("mapping.targetType.mismatch", $"Mapping targetType '{profile.TargetType}' does not match job targetType '{job.TargetType}'.", "targetType"));
        }

        if (profile.FieldMappings.Count == 0)
        {
            issues.Add(Error("mapping.empty", "Mapping profile contains no field mappings.", "fieldMappings"));
        }
    }

    private void ValidateManifestShape(IReadOnlyList<ManifestRow> rows, List<PreflightIssue> issues)
    {
        if (rows.Count == 0)
        {
            issues.Add(Error("manifest.empty", "Manifest returned zero rows."));
            return;
        }

        var firstNonEmptyColumns = rows.FirstOrDefault(x => x.Columns.Count > 0)?.Columns.Keys.ToList() ?? new List<string>();
        if (firstNonEmptyColumns.Count == 0)
        {
            issues.Add(Warning("manifest.noColumns", "Manifest rows contain no column metadata. Mapping column validation may be limited."));
        }
    }

    private void ValidateMapping(MappingProfile profile, IReadOnlyList<ManifestRow> rows, List<PreflightIssue> issues)
    {
        var columns = rows
            .SelectMany(x => x.Columns.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var map in profile.FieldMappings)
        {
            if (string.IsNullOrWhiteSpace(map.Source))
            {
                issues.Add(Error("mapping.source.empty", $"Mapping to target '{map.Target}' has no source column.", map.Target));
            }
            else if (!IsPseudoSource(map.Source) && columns.Count > 0 && !columns.Contains(map.Source))
            {
                issues.Add(Error("mapping.source.missing", $"Source column '{map.Source}' was not found in the manifest.", map.Source));
            }

            if (string.IsNullOrWhiteSpace(map.Target))
            {
                issues.Add(Error("mapping.target.empty", $"Mapping from source '{map.Source}' has no target field.", map.Source));
            }

            if (!string.IsNullOrWhiteSpace(map.Transform) && !KnownTransforms.Contains(map.Transform.Trim()))
            {
                issues.Add(Warning("mapping.transform.unknown", $"Transform '{map.Transform}' is not in the known transform list. It may still be handled by a custom transformer.", map.Target));
            }
        }

        var duplicateTargets = profile.FieldMappings
            .Where(x => !string.IsNullOrWhiteSpace(x.Target))
            .GroupBy(x => x.Target, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var target in duplicateTargets)
        {
            issues.Add(Warning("mapping.target.duplicate", $"Target field '{target}' is mapped more than once.", target));
        }

        var mappedTargets = profile.FieldMappings
            .Select(x => x.Target)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var required in profile.RequiredTargetFields.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (!mappedTargets.Contains(required))
            {
                issues.Add(Error("mapping.requiredTarget.unmapped", $"Required target field '{required}' is not mapped.", required));
            }
        }
    }

    private async Task ValidateRowsAsync(MigrationJobDefinition job, MappingProfile profile, IReadOnlyList<ManifestRow> rows, PreflightRequest request, List<PreflightIssue> issues, CancellationToken cancellationToken)
    {
        var rowsToCheck = request.MaxRows <= 0 ? rows : rows.Take(request.MaxRows).ToList();
        foreach (var row in rowsToCheck)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(row.RowId))
            {
                issues.Add(Error("row.id.missing", "Manifest row has no RowId.", rowId: row.RowId));
            }

            if (string.IsNullOrWhiteSpace(row.SourceAssetId) && string.IsNullOrWhiteSpace(row.SourcePath))
            {
                issues.Add(Error("row.source.missing", "Manifest row has neither SourceAssetId nor SourcePath.", rowId: row.RowId));
            }

            foreach (var required in profile.RequiredTargetFields.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var mapped = profile.FieldMappings.FirstOrDefault(x => x.Target.Equals(required, StringComparison.OrdinalIgnoreCase));
                if (mapped is null) continue;
                if (!TryGetSourceValue(row, mapped.Source, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    issues.Add(Error("row.requiredTarget.empty", $"Required target field '{required}' maps from '{mapped.Source}', but this row has no value.", required, row.RowId, row.SourceAssetId));
                }
            }
        }

        if (request.ValidateSourceSample && request.SourceSampleSize > 0)
        {
            await ValidateSourceSampleAsync(job, profile, rows.Take(request.SourceSampleSize).ToList(), issues, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ValidateSourceSampleAsync(MigrationJobDefinition job, MappingProfile profile, IReadOnlyList<ManifestRow> sampleRows, List<PreflightIssue> issues, CancellationToken cancellationToken)
    {
        var sourceConnector = ResolveSingle(_sourceConnectors, x => x.Type, job.SourceType, "source connector");
        foreach (var row in sampleRows)
        {
            try
            {
                var source = await sourceConnector.GetAssetAsync(job, row, cancellationToken).ConfigureAwait(false);
                var item = new AssetWorkItem
                {
                    WorkItemId = row.RowId,
                    Manifest = row,
                    SourceAsset = source,
                    TargetPayload = _mapper.Map(source, row, profile)
                };

                foreach (var transform in _transformSteps)
                {
                    await transform.ApplyAsync(item, cancellationToken).ConfigureAwait(false);
                }

                foreach (var validation in _validationSteps)
                {
                    var validationIssues = await validation.ValidateAsync(item, cancellationToken).ConfigureAwait(false);
                    foreach (var issue in validationIssues)
                    {
                        issues.Add(new PreflightIssue
                        {
                            Severity = issue.IsError ? PreflightSeverities.Error : PreflightSeverities.Warning,
                            Code = issue.Code,
                            Message = issue.Message,
                            RowId = row.RowId,
                            SourceAssetId = row.SourceAssetId
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                issues.Add(Error("source.sample.failed", $"Source sample validation failed: {ex.Message}", rowId: row.RowId, sourceAssetId: row.SourceAssetId));
            }
        }
    }

    private static bool TryGetSourceValue(ManifestRow row, string source, out string? value)
    {
        if (source.Equals("RowId", StringComparison.OrdinalIgnoreCase)) { value = row.RowId; return true; }
        if (source.Equals("SourceAssetId", StringComparison.OrdinalIgnoreCase) || source.Equals("sourceAssetId", StringComparison.OrdinalIgnoreCase)) { value = row.SourceAssetId; return true; }
        if (source.Equals("SourcePath", StringComparison.OrdinalIgnoreCase) || source.Equals("sourcePath", StringComparison.OrdinalIgnoreCase)) { value = row.SourcePath; return true; }
        return row.Columns.TryGetValue(source, out value);
    }

    private static bool IsPseudoSource(string source)
        => source.Equals("RowId", StringComparison.OrdinalIgnoreCase)
           || source.Equals("SourceAssetId", StringComparison.OrdinalIgnoreCase)
           || source.Equals("sourceAssetId", StringComparison.OrdinalIgnoreCase)
           || source.Equals("SourcePath", StringComparison.OrdinalIgnoreCase)
           || source.Equals("sourcePath", StringComparison.OrdinalIgnoreCase);

    private static PreflightSummary BuildSummary(int totalRows, int checkedRows, IReadOnlyList<PreflightIssue> issues)
        => new()
        {
            TotalRows = totalRows,
            CheckedRows = checkedRows,
            ErrorCount = issues.Count(x => x.Severity.Equals(PreflightSeverities.Error, StringComparison.OrdinalIgnoreCase)),
            WarningCount = issues.Count(x => x.Severity.Equals(PreflightSeverities.Warning, StringComparison.OrdinalIgnoreCase)),
            InfoCount = issues.Count(x => x.Severity.Equals(PreflightSeverities.Info, StringComparison.OrdinalIgnoreCase))
        };

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

    private static PreflightIssue Error(string code, string message, string? field = null, string? rowId = null, string? sourceAssetId = null)
        => new() { Severity = PreflightSeverities.Error, Code = code, Message = message, Field = field, RowId = rowId, SourceAssetId = sourceAssetId };

    private static PreflightIssue Warning(string code, string message, string? field = null, string? rowId = null, string? sourceAssetId = null)
        => new() { Severity = PreflightSeverities.Warning, Code = code, Message = message, Field = field, RowId = rowId, SourceAssetId = sourceAssetId };
}
