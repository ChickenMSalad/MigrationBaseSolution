using System.Text;
using Microsoft.Extensions.Options;

namespace Migration.Core.Azure.ResourceNaming;

public sealed class AzureResourceNameBuilder : IAzureResourceNameBuilder
{
    private readonly AzureResourceNamingOptions _options;

    public AzureResourceNameBuilder(IOptions<AzureResourceNamingOptions> options)
    {
        _options = options.Value;
    }

    public AzureResourceNameResult Build(AzureResourceNameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var warnings = new List<string>();
        var separator = string.IsNullOrWhiteSpace(_options.Separator) ? "-" : _options.Separator.Trim();
        var token = ResolveResourceTypeToken(request.ResourceType, warnings);

        var segments = new List<string>
        {
            _options.Organization,
            _options.Application,
            _options.Environment
        };

        if (_options.IncludeRegionInNames)
        {
            segments.Add(_options.RegionCode);
        }

        segments.Add(token);
        segments.Add(request.Workload);

        if (!string.IsNullOrWhiteSpace(request.Instance))
        {
            segments.Add(request.Instance);
        }

        var normalizedSegments = segments
            .Select(segment => NormalizeSegment(segment, request.StorageAccountSafe))
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();

        var value = request.StorageAccountSafe
            ? string.Concat(normalizedSegments)
            : string.Join(separator, normalizedSegments);

        if (request.MaxLength is > 0 && value.Length > request.MaxLength.Value)
        {
            warnings.Add($"Generated name exceeded MaxLength={request.MaxLength.Value} and was trimmed.");
            value = value[..request.MaxLength.Value].Trim(separator.ToCharArray());
        }

        return new AzureResourceNameResult(value, normalizedSegments, warnings);
    }

    private string ResolveResourceTypeToken(string resourceType, ICollection<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            warnings.Add("ResourceType was empty; using generic resource token.");
            return "res";
        }

        if (_options.ResourceTypeTokens.TryGetValue(resourceType.Trim(), out var token) && !string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        warnings.Add($"No configured token found for resource type '{resourceType}'; normalized resource type was used.");
        return resourceType;
    }

    private static string NormalizeSegment(string? value, bool storageAccountSafe)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                continue;
            }

            if (!storageAccountSafe && (character == '-' || character == '_'))
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }
}
