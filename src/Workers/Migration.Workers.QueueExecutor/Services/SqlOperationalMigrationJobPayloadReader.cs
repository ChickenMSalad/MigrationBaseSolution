using System.Text.Json;
using Migration.Domain.Models;

namespace Migration.Workers.QueueExecutor.Services;

internal static class SqlOperationalMigrationJobPayloadReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] JobPropertyNames =
    [
        "job",
        "migrationJob",
        "jobDefinition",
        "migrationJobDefinition"
    ];

    public static bool TryReadJob(
        string? payloadJson,
        out MigrationJobDefinition? job,
        out string? error)
    {
        job = null;
        error = null;

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            error = "Work item payload is empty. Expected a MigrationJobDefinition JSON object or an envelope containing a job object.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "Work item payload must be a JSON object.";
                return false;
            }

            foreach (var propertyName in JobPropertyNames)
            {
                if (root.TryGetProperty(propertyName, out var jobElement) && jobElement.ValueKind == JsonValueKind.Object)
                {
                    job = jobElement.Deserialize<MigrationJobDefinition>(JsonOptions);
                    return ValidateJob(job, propertyName, out error);
                }
            }

            job = root.Deserialize<MigrationJobDefinition>(JsonOptions);
            return ValidateJob(job, "root", out error);
        }
        catch (JsonException ex)
        {
            error = $"Work item payload is not valid JSON: {ex.Message}";
            return false;
        }
        catch (NotSupportedException ex)
        {
            error = $"Work item payload could not be deserialized as a MigrationJobDefinition: {ex.Message}";
            return false;
        }
    }

    private static bool ValidateJob(
        MigrationJobDefinition? job,
        string source,
        out string? error)
    {
        if (job is null)
        {
            error = $"No MigrationJobDefinition could be read from payload source '{source}'.";
            return false;
        }

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(job.JobName))
        {
            missing.Add(nameof(job.JobName));
        }

        if (string.IsNullOrWhiteSpace(job.SourceType))
        {
            missing.Add(nameof(job.SourceType));
        }

        if (string.IsNullOrWhiteSpace(job.TargetType))
        {
            missing.Add(nameof(job.TargetType));
        }

        if (string.IsNullOrWhiteSpace(job.ManifestType))
        {
            missing.Add(nameof(job.ManifestType));
        }

        if (missing.Count > 0)
        {
            error = $"MigrationJobDefinition from payload source '{source}' is missing required field(s): {string.Join(", ", missing)}.";
            return false;
        }

        error = null;
        return true;
    }
}
