using System.Reflection;
using Migration.Connectors.Targets.AzureBlob.Models;

namespace Migration.Connectors.Targets.AzureBlob.Services;

/// <summary>
/// Small reflection adapter used by AzureBlobTargetConnector so the connector can consume the
/// generic AssetWorkItem/TargetAssetPayload without creating a second mapping pipeline.
/// </summary>
public static class AzureBlobIntermediateStorageRequestFactory
{
    public static IReadOnlyDictionary<string, string?> BuildRowValues(object? workItemOrPayload)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        AddObjectProperties(result, workItemOrPayload);

        var payload = GetPropertyValue(workItemOrPayload, "Payload")
            ?? GetPropertyValue(workItemOrPayload, "TargetPayload")
            ?? GetPropertyValue(workItemOrPayload, "TargetAssetPayload")
            ?? GetPropertyValue(workItemOrPayload, "Asset");

        AddObjectProperties(result, payload);

        var source = GetPropertyValue(workItemOrPayload, "Source")
            ?? GetPropertyValue(workItemOrPayload, "SourceAsset")
            ?? GetPropertyValue(workItemOrPayload, "Envelope");

        AddObjectProperties(result, source);

        var manifestRow = GetPropertyValue(workItemOrPayload, "Row")
            ?? GetPropertyValue(workItemOrPayload, "ManifestRow");

        AddObjectProperties(result, manifestRow);

        foreach (var dictionaryCandidate in new[]
        {
            GetPropertyValue(workItemOrPayload, "Values"),
            GetPropertyValue(workItemOrPayload, "Fields"),
            GetPropertyValue(workItemOrPayload, "Metadata"),
            GetPropertyValue(payload, "Values"),
            GetPropertyValue(payload, "Fields"),
            GetPropertyValue(payload, "Metadata"),
            GetPropertyValue(source, "Metadata"),
            GetPropertyValue(manifestRow, "Values")
        })
        {
            AddDictionary(result, dictionaryCandidate);
        }

        return result;
    }

    public static Stream? TryGetBinaryStream(object? workItemOrPayload)
    {
        var candidates = new[]
        {
            workItemOrPayload,
            GetPropertyValue(workItemOrPayload, "Payload"),
            GetPropertyValue(workItemOrPayload, "TargetPayload"),
            GetPropertyValue(workItemOrPayload, "TargetAssetPayload"),
            GetPropertyValue(workItemOrPayload, "Source"),
            GetPropertyValue(workItemOrPayload, "SourceAsset"),
            GetPropertyValue(workItemOrPayload, "Envelope"),
            GetPropertyValue(GetPropertyValue(workItemOrPayload, "SourceAsset"), "Binary"),
            GetPropertyValue(GetPropertyValue(workItemOrPayload, "Source"), "Binary")
        };

        foreach (var candidate in candidates)
        {
            if (candidate is Stream stream)
            {
                return stream;
            }

            foreach (var propertyName in new[] { "Stream", "Content", "Binary", "Data", "ContentStream", "BinaryStream" })
            {
                if (GetPropertyValue(candidate, propertyName) is Stream streamProperty)
                {
                    return streamProperty;
                }
            }
        }

        return null;
    }

    public static string? TryGetContentType(object? workItemOrPayload)
    {
        var row = BuildRowValues(workItemOrPayload);
        foreach (var key in new[] { "ContentType", "contentType", "MimeType", "mimeType", "MIME Type" })
        {
            if (row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    public static async Task<IntermediateStorageOptions?> TryReadIntermediateStorageOptionsAsync(
        string? mappingProfilePath,
        CancellationToken cancellationToken = default)
    {
        var profile = await IntermediateStorageMappingProfileReader
            .ReadAsync(mappingProfilePath, cancellationToken)
            .ConfigureAwait(false);

        return IntermediateStorageMappingProfileReader.GetIntermediateStorageOrNull(profile);
    }

    private static object? GetPropertyValue(object? value, string propertyName)
    {
        if (value is null)
        {
            return null;
        }

        return value.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
            ?.GetValue(value);
    }

    private static void AddObjectProperties(IDictionary<string, string?> target, object? value)
    {
        if (value is null || value is string || value.GetType().IsPrimitive)
        {
            return;
        }

        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            object? raw;
            try
            {
                raw = property.GetValue(value);
            }
            catch
            {
                continue;
            }

            if (raw is null)
            {
                continue;
            }

            if (raw is string text)
            {
                target[property.Name] = text;
                continue;
            }

            if (raw.GetType().IsPrimitive || raw is decimal || raw is DateTime || raw is DateTimeOffset || raw is Guid)
            {
                target[property.Name] = Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    private static void AddDictionary(IDictionary<string, string?> target, object? value)
    {
        if (value is null)
        {
            return;
        }

        if (value is IEnumerable<KeyValuePair<string, string?>> nullableStringPairs)
        {
            foreach (var pair in nullableStringPairs)
            {
                target[pair.Key] = pair.Value;
            }

            return;
        }

        if (value is IEnumerable<KeyValuePair<string, string>> stringPairs)
        {
            foreach (var pair in stringPairs)
            {
                target[pair.Key] = pair.Value;
            }

            return;
        }

        if (value is System.Collections.IDictionary dictionary)
        {
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                if (entry.Key is null)
                {
                    continue;
                }

                target[entry.Key.ToString() ?? string.Empty] = entry.Value?.ToString();
            }
        }
    }
}
