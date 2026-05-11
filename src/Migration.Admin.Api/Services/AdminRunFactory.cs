using Migration.Admin.Api.Models;
using Migration.Domain.Models;

namespace Migration.Admin.Api.Services;

public sealed class AdminRunFactory
{
    public MigrationProjectRecord CreateProject(CreateMigrationProjectRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        return new MigrationProjectRecord
        {
            ProjectId = CreateId("project"),
            DisplayName = request.DisplayName.Trim(),
            SourceType = request.SourceType.Trim(),
            TargetType = request.TargetType.Trim(),
            ManifestType = request.ManifestType.Trim(),
            CreatedUtc = now,
            UpdatedUtc = now,
            Settings = new Dictionary<string, string?>(request.Settings ?? new(), StringComparer.OrdinalIgnoreCase)
        };
    }

    public MigrationProjectRecord UpdateProject(MigrationProjectRecord existing, UpdateMigrationProjectRequest request)
    {
        return existing with
        {
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? existing.DisplayName : request.DisplayName.Trim(),
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? existing.SourceType : request.SourceType.Trim(),
            TargetType = string.IsNullOrWhiteSpace(request.TargetType) ? existing.TargetType : request.TargetType.Trim(),
            ManifestType = string.IsNullOrWhiteSpace(request.ManifestType) ? existing.ManifestType : request.ManifestType.Trim(),
            UpdatedUtc = DateTimeOffset.UtcNow,
            Settings = request.Settings is null ? existing.Settings : new Dictionary<string, string?>(request.Settings, StringComparer.OrdinalIgnoreCase)
        };
    }

    public MigrationRunControlRecord CreateRun(MigrationProjectRecord project, CreateRunRequest request)
    {
        var runId = CreateId("run");
        var jobName = NormalizeJobName(request.JobName, project, runId);
        var job = new MigrationJobDefinition
        {
            JobName = jobName,
            SourceType = project.SourceType,
            TargetType = project.TargetType,
            ManifestType = project.ManifestType,
            ManifestPath = request.ManifestPath,
            MappingProfilePath = request.MappingProfilePath,
            DryRun = request.DryRun,
            Parallelism = Math.Max(1, request.Parallelism),
            Settings = MergeSettings(project.Settings, request.Settings)
        };

        return new MigrationRunControlRecord
        {
            RunId = runId,
            ProjectId = project.ProjectId,
            JobName = jobName,
            DryRun = request.DryRun,
            Status = AdminRunStatuses.Queued,
            Message = "Run accepted by Admin API. Execution should be handled by the worker runtime.",
            Job = job
        };
    }

    public MigrationRunControlRecord CreatePreflight(MigrationProjectRecord project, CreatePreflightRequest request)
    {
        var runId = CreateId("preflight");
        var jobName = NormalizeJobName(request.JobName, project, runId);
        var settings = MergeSettings(project.Settings, request.Settings);
        settings["PreflightOnly"] = "true";

        var job = new MigrationJobDefinition
        {
            JobName = jobName,
            SourceType = project.SourceType,
            TargetType = project.TargetType,
            ManifestType = project.ManifestType,
            ManifestPath = request.ManifestPath,
            MappingProfilePath = request.MappingProfilePath,
            DryRun = true,
            Parallelism = 1,
            Settings = settings
        };

        return new MigrationRunControlRecord
        {
            RunId = runId,
            ProjectId = project.ProjectId,
            JobName = jobName,
            DryRun = true,
            Status = AdminRunStatuses.PreflightQueued,
            Message = "Preflight accepted by Admin API. Execution should be handled by the worker runtime.",
            Job = job
        };
    }

    private static Dictionary<string, string?> MergeSettings(Dictionary<string, string?> projectSettings, Dictionary<string, string?>? runSettings)
    {
        var merged = new Dictionary<string, string?>(projectSettings, StringComparer.OrdinalIgnoreCase);
        if (runSettings is not null)
            foreach (var pair in runSettings) merged[pair.Key] = pair.Value;
        return merged;
    }

    private static string NormalizeJobName(string? requestedJobName, MigrationProjectRecord project, string runId)
        => string.IsNullOrWhiteSpace(requestedJobName) ? $"{project.ProjectId}-{runId}" : requestedJobName.Trim();

    private static string CreateId(string prefix)
        => $"{prefix}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
}
