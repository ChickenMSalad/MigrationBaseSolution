using System;

namespace MigrationBase.Core.Cloud.Azure;

public sealed class RuntimeEnvironmentOptions
{
    public string EnvironmentName { get; set; } = string.Empty;

    public string ApplicationName { get; set; } = "MigrationBaseSolution";

    public string Region { get; set; } = string.Empty;

    public string DeploymentSlot { get; set; } = string.Empty;

    public string TenantName { get; set; } = string.Empty;

    public bool IsProductionLike { get; set; }

    public string NormalizedEnvironmentName => Normalize(EnvironmentName);

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
