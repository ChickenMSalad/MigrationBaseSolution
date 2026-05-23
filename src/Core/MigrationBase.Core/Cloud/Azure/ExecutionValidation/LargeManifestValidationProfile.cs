using System;

namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public sealed record LargeManifestValidationProfile
{
    public string ProfileName { get; init; } = string.Empty;

    public string EnvironmentName { get; init; } = string.Empty;

    public long ExpectedManifestRowCount { get; init; }

    public long MinimumAcceptedManifestRowCount { get; init; }

    public long MaximumAcceptedManifestRowCount { get; init; }

    public int ExpectedShardCount { get; init; }

    public int MaximumRowsPerShard { get; init; }

    public TimeSpan MaximumManifestLoadDuration { get; init; } = TimeSpan.Zero;

    public TimeSpan MaximumQueueProjectionDuration { get; init; } = TimeSpan.Zero;

    public TimeSpan MaximumOperationalQueryDuration { get; init; } = TimeSpan.Zero;

    public bool RequireSqlOperationalStore { get; init; } = true;

    public bool RequireResumeMarkers { get; init; } = true;

    public bool RequireReplayEligibilitySnapshot { get; init; } = true;

    public bool RequireSourceTargetIdentifierMapping { get; init; } = true;
}
