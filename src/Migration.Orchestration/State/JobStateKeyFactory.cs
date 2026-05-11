using System.Security.Cryptography;
using Migration.Domain.Models;

namespace Migration.Orchestration.State;

/// <summary>
/// Helper for deriving stable state namespaces. The current runner primarily relies on
/// MigrationWorkItemState.DryRun to prevent dry-run successes from skipping real runs,
/// but this helper gives consoles/APIs a common way to display or plan future state keys.
/// </summary>
public static class JobStateKeyFactory
{
    public static string BuildStateJobName(MigrationJobDefinition job)
    {
        ArgumentNullException.ThrowIfNull(job);

        var mode = job.DryRun ? "dryrun" : "realrun";
        var manifestFingerprint = ComputeManifestFingerprint(job.ManifestPath);
        return $"{job.JobName}::{mode}::{manifestFingerprint}";
    }

    public static string ComputeManifestFingerprint(string? manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
        {
            return "nofile";
        }

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(File.ReadAllBytes(manifestPath));
        return Convert.ToHexString(hash)[..12];
    }
}
