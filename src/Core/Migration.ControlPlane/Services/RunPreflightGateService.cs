using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Services;

public sealed class RunPreflightGateService
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly IAdminProjectStore _store;

    public RunPreflightGateService(IAdminProjectStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<RunPreflightGateResult> ValidateRunCanStartAsync(
        MigrationProjectRecord project,
        CreateRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(request);

        var settings = MergeSettings(project.Settings, request.Settings);

        if (!GetBoolean(settings, "RequirePreflightGate", defaultValue: false))
        {
            return RunPreflightGateResult.Allowed("Preflight gate is advisory by default; migration runs are allowed without a completed preflight.");
        }

        if (GetBoolean(settings, "SkipPreflightGate", defaultValue: false))
        {
            return RunPreflightGateResult.Allowed("Preflight gate bypassed by explicit SkipPreflightGate setting.");
        }

        if (request.DryRun && GetBoolean(settings, "AllowDryRunWithoutPreflight", defaultValue: true))
        {
            return RunPreflightGateResult.Allowed("Dry-run execution is allowed without a completed preflight.");
        }

        var runs = await _store.ListRunsAsync(cancellationToken).ConfigureAwait(false);

        var latestMatchingPreflight = runs
            .Where(run => IsMatchingCompletedPreflight(run, project, request))
            .OrderByDescending(run => run.CompletedUtc ?? run.UpdatedUtc)
            .FirstOrDefault();

        if (latestMatchingPreflight is not null)
        {
            return RunPreflightGateResult.Allowed(
                $"Run is allowed because preflight '{latestMatchingPreflight.RunId}' completed successfully.",
                latestMatchingPreflight.RunId);
        }

        return RunPreflightGateResult.Blocked(
            "This project explicitly requires a successful preflight before starting a non-dry-run migration. " +
            "Run project preflight first, wait for it to complete, then start the migration run, " +
            "or remove Settings.RequirePreflightGate=true from the project/run settings.");
    }

    private static bool IsMatchingCompletedPreflight(
        MigrationRunControlRecord run,
        MigrationProjectRecord project,
        CreateRunRequest request)
    {
        if (!Comparer.Equals(run.ProjectId, project.ProjectId))
        {
            return false;
        }

        if (!Comparer.Equals(run.Status, AdminRunStatuses.Completed))
        {
            return false;
        }

        if (!IsPreflightRun(run))
        {
            return false;
        }

        if (!Comparer.Equals(run.Job.SourceType, project.SourceType) ||
            !Comparer.Equals(run.Job.TargetType, project.TargetType) ||
            !Comparer.Equals(run.Job.ManifestType, project.ManifestType))
        {
            return false;
        }

        var requestedManifestArtifactId = Normalize(request.ManifestArtifactId) ?? Normalize(project.ManifestArtifactId);
        var requestedMappingArtifactId = Normalize(request.MappingArtifactId) ?? Normalize(project.MappingArtifactId);

        if (!IdsMatch(requestedManifestArtifactId, run.ManifestArtifactId))
        {
            return false;
        }

        if (!IdsMatch(requestedMappingArtifactId, run.MappingArtifactId))
        {
            return false;
        }

        return true;
    }

    private static bool IsPreflightRun(MigrationRunControlRecord run)
    {
        if (Comparer.Equals(run.Status, AdminRunStatuses.PreflightQueued))
        {
            return true;
        }

        if (run.Job.Settings.TryGetValue("PreflightOnly", out var value) &&
            bool.TryParse(value, out var parsed) && parsed)
        {
            return true;
        }

        return run.RunId.StartsWith("preflight-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IdsMatch(string? requestedId, string? actualId)
    {
        if (requestedId is null)
        {
            return true;
        }

        return Comparer.Equals(requestedId, Normalize(actualId));
    }

    private static Dictionary<string, string?> MergeSettings(
        Dictionary<string, string?> projectSettings,
        Dictionary<string, string?>? requestSettings)
    {
        var merged = new Dictionary<string, string?>(projectSettings, Comparer);

        if (requestSettings is not null)
        {
            foreach (var pair in requestSettings)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        return merged;
    }

    private static bool GetBoolean(
        IReadOnlyDictionary<string, string?> settings,
        string key,
        bool defaultValue)
    {
        if (!settings.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record RunPreflightGateResult(
    bool IsAllowed,
    string Message,
    string? PreflightRunId = null)
{
    public static RunPreflightGateResult Allowed(string message, string? preflightRunId = null) =>
        new(true, message, preflightRunId);

    public static RunPreflightGateResult Blocked(string message) =>
        new(false, message);
}
