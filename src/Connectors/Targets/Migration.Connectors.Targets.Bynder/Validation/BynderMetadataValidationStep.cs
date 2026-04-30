using System.Collections;
using System.Reflection;
using Bynder.Sdk.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Migration.Application.Abstractions;
using Migration.Connectors.Targets.Bynder.Clients;
using Migration.Domain.Models;

namespace Migration.Connectors.Targets.Bynder.Validation;

public sealed class BynderMetadataValidationStep : IValidationStep
{
    private readonly IBynderClient _bynderClient;
    private readonly MetapropertyOptionBuilderFactory _builderFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BynderMetadataValidationStep> _logger;

    private static readonly HashSet<string> ReservedFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "mediaId", "assetId", "bynderId", "name", "filename", "fileName", "originalFileName",
        "description", "tags", "keywords", "sourceUri", "downloadUrl", "url", "filePath", "path",
        "metapropertyOptions", "metadata"
    };

    public BynderMetadataValidationStep(
        IBynderClient bynderClient,
        MetapropertyOptionBuilderFactory builderFactory,
        IMemoryCache cache,
        ILogger<BynderMetadataValidationStep> logger)
    {
        _bynderClient = bynderClient;
        _builderFactory = builderFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ValidationIssue>> ValidateAsync(AssetWorkItem item, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(item.TargetPayload?.TargetType.ToString(), "Bynder", StringComparison.OrdinalIgnoreCase) &&
            item.TargetPayload is null)
        {
            return Array.Empty<ValidationIssue>();
        }

        var fields = item.TargetPayload?.Fields;
        if (fields is null || fields.Count == 0)
        {
            return Array.Empty<ValidationIssue>();
        }

        var issues = new List<ValidationIssue>();
        var builder = await _builderFactory.CreateBuilder().ConfigureAwait(false);
        var metapropertiesByName = await GetMetapropertiesByNameAsync().ConfigureAwait(false);

        foreach (var field in fields)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = NormalizeMetapropertyTargetName(field.Key);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var values = ToStringList(field.Value);
            if (values.Count == 0)
            {
                continue;
            }

            try
            {
                builder[name] = values;
            }
            catch (BynderException ex)
            {
                issues.Add(new ValidationIssue("bynder.metaproperty_missing", $"Mapped target field '{field.Key}' could not be resolved as a Bynder metaproperty: {ex.Message}"));
                continue;
            }

            if (metapropertiesByName.TryGetValue(name, out var metaproperty))
            {
                var allowed = ExtractAllowedOptionValues(metaproperty);
                if (allowed.Count > 0)
                {
                    foreach (var value in values)
                    {
                        if (!allowed.Contains(value))
                        {
                            issues.Add(new ValidationIssue("bynder.metaproperty_value_invalid", $"Value '{value}' is not valid for Bynder metaproperty '{name}'."));
                        }
                    }
                }
            }
            else
            {
                _logger.LogDebug("Bynder metaproperty '{Name}' resolved through builder but could not be found in reflection cache for option value validation.", name);
            }
        }

        return issues;
    }

    private async Task<Dictionary<string, object>> GetMetapropertiesByNameAsync()
    {
        return (await _cache.GetOrCreateAsync("BynderMetadataValidationStep.Metaproperties", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var service = _bynderClient.GetAssetService();
            var metaproperties = await service.GetMetapropertiesAsync().ConfigureAwait(false);
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var entryObject in metaproperties)
            {
                var value = ReadProperty(entryObject, "Value") ?? entryObject;
                var name = ReadProperty(value, "Name")?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    result[name] = value;
                }
            }

            return result;
        }).ConfigureAwait(false)) ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> ExtractAllowedOptionValues(object metaproperty)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var options = ReadProperty(metaproperty, "Options") ?? ReadProperty(metaproperty, "Values");
        if (options is not IEnumerable enumerable || options is string)
        {
            return allowed;
        }

        foreach (var option in enumerable)
        {
            foreach (var prop in new[] { "Id", "ID", "Name", "Label", "DisplayName", "Value", "ExternalReference", "ExternalId" })
            {
                var value = ReadProperty(option!, prop)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    allowed.Add(value);
                }
            }
        }

        return allowed;
    }

    private static object? ReadProperty(object value, string propertyName)
    {
        return value.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)?.GetValue(value);
    }

    private static string? NormalizeMetapropertyTargetName(string fieldName)
    {
        if (ReservedFieldNames.Contains(fieldName) || fieldName.StartsWith("_", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (fieldName.StartsWith("meta:", StringComparison.OrdinalIgnoreCase))
        {
            return fieldName["meta:".Length..].Trim();
        }

        if (fieldName.StartsWith("metaproperty:", StringComparison.OrdinalIgnoreCase))
        {
            return fieldName["metaproperty:".Length..].Trim();
        }

        return fieldName.Trim();
    }

    private static IList<string> ToStringList(object? value)
    {
        if (value is null)
        {
            return new List<string>();
        }

        if (value is string text)
        {
            return text.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        if (value is IEnumerable<string> strings)
        {
            return strings.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
        }

        if (value is IEnumerable enumerable)
        {
            return enumerable.Cast<object?>().Select(x => x?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).ToList();
        }

        return new List<string> { value.ToString() ?? string.Empty };
    }
}
