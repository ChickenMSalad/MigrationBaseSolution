using System.Globalization;
using Microsoft.Extensions.Logging;
using Migration.Connectors.Targets.Cloudinary.Clients;
using Migration.Connectors.Targets.Cloudinary.Configuration;
using Migration.Connectors.Targets.Cloudinary.Models;

namespace Migration.Connectors.Targets.Cloudinary.Services;

public sealed class CloudinaryStructuredMetadataService(
    ICloudinaryAdminClient adminClient,
    ILogger<CloudinaryStructuredMetadataService> logger)
{
    private IReadOnlyDictionary<string, CloudinaryMetadataFieldSchema>? _schemas;

    public async Task<Dictionary<string, object>> BuildMetadataAsync(
        IDictionary<string, string?> row,
        IReadOnlyList<CloudinaryStructuredMetadataMapping> mappings,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (mappings.Count == 0)
        {
            return result;
        }

        var schemas = await GetSchemasAsync(cancellationToken).ConfigureAwait(false);

        foreach (var mapping in mappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.Column) || string.IsNullOrWhiteSpace(mapping.ExternalId))
            {
                continue;
            }

            if (!row.TryGetValue(mapping.Column, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (!schemas.TryGetValue(mapping.ExternalId, out var schema))
            {
                throw new InvalidOperationException($"Structured metadata field '{mapping.ExternalId}' was not found in Cloudinary.");
            }

            result[mapping.ExternalId] = ResolveValue(schema, raw!, mapping);
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<string, CloudinaryMetadataFieldSchema>> GetSchemasAsync(CancellationToken cancellationToken = default)
    {
        if (_schemas is not null)
        {
            return _schemas;
        }

        var fields = await adminClient.GetMetadataFieldsAsync(cancellationToken).ConfigureAwait(false);
        _schemas = fields.ToDictionary(x => x.ExternalId, StringComparer.OrdinalIgnoreCase);
        logger.LogInformation("Loaded {Count} Cloudinary structured metadata field definitions.", _schemas.Count);
        return _schemas;
    }

    private static object ResolveValue(CloudinaryMetadataFieldSchema schema, string raw, CloudinaryStructuredMetadataMapping mapping)
    {
        if (schema.Type.Equals("date", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(mapping.DateInputFormat) &&
                DateTime.TryParseExact(raw.Trim(), mapping.DateInputFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate))
            {
                return exactDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
            {
                return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException($"Could not parse date value '{raw}' for structured metadata field '{schema.ExternalId}'.");
        }

        if (schema.Type.Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            var separator = string.IsNullOrWhiteSpace(mapping.Separator) ? "," : mapping.Separator!;
            var labels = raw.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return labels.Select(label => ResolveSelectValue(schema, label, mapping)).ToArray();
        }

        if (schema.Type.Equals("enum", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveSelectValue(schema, raw.Trim(), mapping);
        }

        if (schema.Type.Equals("integer", StringComparison.OrdinalIgnoreCase) && long.TryParse(raw, out var integer))
        {
            return integer;
        }

        return raw.Trim();
    }

    private static string ResolveSelectValue(CloudinaryMetadataFieldSchema schema, string raw, CloudinaryStructuredMetadataMapping mapping)
    {
        if (mapping.StaticOptionMappings.TryGetValue(raw, out var mapped))
        {
            return mapped;
        }

        if (!string.Equals(mapping.ValueMode, CloudinaryMetadataValueMode.Label, StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        var option = schema.DatasourceValues.FirstOrDefault(x => string.Equals(x.Value, raw, StringComparison.OrdinalIgnoreCase));
        if (option is null)
        {
            throw new InvalidOperationException($"Could not resolve datasource label '{raw}' for Cloudinary field '{schema.ExternalId}'.");
        }

        return option.ExternalId;
    }
}
