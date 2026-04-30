using Migration.Application.Models;
using Migration.Domain.Models;

namespace Migration.Orchestration.Validation;

public static class TargetPayloadGuard
{
    public static IReadOnlyList<ValidationIssue> Validate(MigrationJobDefinition job, MappingProfile profile, AssetWorkItem item)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(item);

        var issues = new List<ValidationIssue>();
        var payload = item.TargetPayload;

        if (payload is null)
        {
            issues.Add(new ValidationIssue("target.payload.missing", "Target payload is missing."));
            return issues;
        }

        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            issues.Add(new ValidationIssue("target.name.missing", "Target payload Name is missing or empty."));
        }

        foreach (var requiredField in profile.RequiredTargetFields.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (requiredField.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(payload.Name))
                {
                    issues.Add(new ValidationIssue("target.required.name.missing", "Required target field 'Name' is missing or empty."));
                }

                continue;
            }

            if (!payload.Fields.TryGetValue(requiredField, out var value) || IsEmpty(value))
            {
                issues.Add(new ValidationIssue("target.required.field.missing", $"Required target field '{requiredField}' is missing or empty."));
            }
        }

        if (!job.DryRun && job.TargetType.Equals("Bynder", StringComparison.OrdinalIgnoreCase))
        {
            ValidateBynderRealRun(job, item, issues);
        }

        return issues;
    }

    private static void ValidateBynderRealRun(MigrationJobDefinition job, AssetWorkItem item, List<ValidationIssue> issues)
    {
        var payload = item.TargetPayload!;
        var originIdField = GetSetting(job, "BynderOriginIdField", "OriginId");

        if (!payload.Fields.TryGetValue(originIdField, out var originId) || IsEmpty(originId))
        {
            issues.Add(new ValidationIssue(
                "bynder.originid.missing",
                $"Bynder real runs require a stable unique target identity. Map a source value to '{originIdField}' or set Settings:BynderOriginIdField to the mapped field name."));
        }

        var binary = payload.Binary ?? item.SourceAsset?.Binary;
        var sourcePath = item.Manifest.SourcePath;
        var sourceAssetId = item.Manifest.SourceAssetId ?? item.SourceAsset?.SourceAssetId;

        if (binary is null && string.IsNullOrWhiteSpace(sourcePath) && string.IsNullOrWhiteSpace(sourceAssetId))
        {
            issues.Add(new ValidationIssue(
                "bynder.binary.source.missing",
                "Bynder real run has no binary metadata, SourcePath, or SourceAssetId. The target connector cannot safely upload a real asset."));
            return;
        }

        if (binary is not null)
        {
            if (string.IsNullOrWhiteSpace(binary.FileName) && string.IsNullOrWhiteSpace(payload.Name))
            {
                issues.Add(new ValidationIssue(
                    "bynder.binary.filename.missing",
                    "Binary metadata has no FileName and target payload has no Name."));
            }

            if (binary.Length.HasValue && binary.Length.Value <= 0 && string.IsNullOrWhiteSpace(binary.SourceUri) && string.IsNullOrWhiteSpace(sourcePath))
            {
                issues.Add(new ValidationIssue(
                    "bynder.binary.empty",
                    "Binary metadata length is zero/negative and no SourceUri or SourcePath is available."));
            }
        }
    }

    private static string GetSetting(MigrationJobDefinition job, string key, string fallback)
    {
        return job.Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value!
            : fallback;
    }

    private static bool IsEmpty(object? value)
    {
        return value switch
        {
            null => true,
            string text => string.IsNullOrWhiteSpace(text),
            IEnumerable<string> strings => !strings.Any(x => !string.IsNullOrWhiteSpace(x)),
            System.Collections.IEnumerable enumerable when value is not string => !enumerable.Cast<object?>().Any(x => !IsEmpty(x)),
            _ => false
        };
    }
}
