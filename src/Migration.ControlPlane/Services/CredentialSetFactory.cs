using Migration.ControlPlane.Models;

namespace Migration.ControlPlane.Services;

public sealed class CredentialSetFactory
{
    public CredentialSetRecord Create(CreateCredentialSetRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        return new CredentialSetRecord
        {
            CredentialSetId = $"cred-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}",
            DisplayName = request.DisplayName.Trim(),
            ConnectorType = request.ConnectorType.Trim(),
            ConnectorRole = request.ConnectorRole.Trim(),
            CreatedUtc = now,
            UpdatedUtc = now,
            Values = NormalizeValues(request.Values),
            SecretKeys = NormalizeSecretKeys(request.SecretKeys, request.Values)
        };
    }

    public CredentialSetRecord Update(CredentialSetRecord existing, UpdateCredentialSetRequest request)
    {
        return existing with
        {
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? existing.DisplayName : request.DisplayName.Trim(),
            ConnectorType = string.IsNullOrWhiteSpace(request.ConnectorType) ? existing.ConnectorType : request.ConnectorType.Trim(),
            ConnectorRole = string.IsNullOrWhiteSpace(request.ConnectorRole) ? existing.ConnectorRole : request.ConnectorRole.Trim(),
            UpdatedUtc = DateTimeOffset.UtcNow,
            Values = request.Values is null ? existing.Values : NormalizeValues(request.Values),
            SecretKeys = request.SecretKeys is null
                ? existing.SecretKeys
                : NormalizeSecretKeys(request.SecretKeys, request.Values ?? existing.Values)
        };
    }

    public static CredentialSetSummary Sanitize(CredentialSetRecord record)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in record.Values)
        {
            values[pair.Key] = record.SecretKeys.Contains(pair.Key) && !string.IsNullOrEmpty(pair.Value)
                ? "********"
                : pair.Value;
        }

        return new CredentialSetSummary(
            record.CredentialSetId,
            record.DisplayName,
            record.ConnectorType,
            record.ConnectorRole,
            record.CreatedUtc,
            record.UpdatedUtc,
            values,
            record.SecretKeys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static Dictionary<string, string?> NormalizeValues(Dictionary<string, string?> values)
    {
        return values
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .ToDictionary(x => x.Key.Trim(), x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> NormalizeSecretKeys(IReadOnlyCollection<string>? secretKeys, Dictionary<string, string?> values)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (secretKeys is not null)
        {
            foreach (var key in secretKeys.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                result.Add(key.Trim());
            }
        }

        foreach (var key in values.Keys)
        {
            if (LooksSecret(key)) result.Add(key.Trim());
        }

        return result;
    }

    private static bool LooksSecret(string key)
    {
        return key.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || key.Contains("password", StringComparison.OrdinalIgnoreCase)
            || key.Contains("token", StringComparison.OrdinalIgnoreCase)
            || key.Contains("key", StringComparison.OrdinalIgnoreCase)
            || key.Contains("connectionstring", StringComparison.OrdinalIgnoreCase);
    }
}
