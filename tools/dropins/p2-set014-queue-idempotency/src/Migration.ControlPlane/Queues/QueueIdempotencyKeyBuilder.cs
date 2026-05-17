using System.Security.Cryptography;
using System.Text;

namespace Migration.ControlPlane.Queues;

public static class QueueIdempotencyKeyBuilder
{
    public static string Build(
        string workspaceId,
        string projectId,
        string runId,
        string messageType)
    {
        var raw = string.Join(":",
            Normalize(workspaceId),
            Normalize(projectId),
            Normalize(runId),
            Normalize(messageType));

        return raw;
    }

    public static string BuildHashed(
        string workspaceId,
        string projectId,
        string runId,
        string messageType)
    {
        var raw = Build(workspaceId, projectId, runId, messageType);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string LeaseResourceForRun(
        string workspaceId,
        string projectId,
        string runId) =>
        $"workspace/{Normalize(workspaceId)}/project/{Normalize(projectId)}/run/{Normalize(runId)}";

    private static string Normalize(string value)
    {
        var sanitized = new string((value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' ? ch : '-')
            .ToArray());

        while (sanitized.Contains("--", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(sanitized)
            ? "unknown"
            : sanitized.Trim('-');
    }
}
