namespace Migration.Admin.Api.Contracts;

public sealed record TargetExecutionEvidenceResponse(
    string RunId,
    string JobName,
    int TotalCount,
    int SuccessCount,
    int FailedCount,
    int RetryCount,
    IReadOnlyList<TargetExecutionEvidenceRow> Rows);

public sealed record TargetExecutionEvidenceRow(
    string WorkItemId,
    string Status,
    string? OriginId,
    string? Id,
    string? TargetAssetId,
    string? FileName,
    string? Message,
    string? Error,
    DateTimeOffset? StartedUtc,
    DateTimeOffset? CompletedUtc,
    DateTimeOffset UpdatedUtc,
    IReadOnlyDictionary<string, string?> StampedFields,
    IReadOnlyDictionary<string, string?> TargetPayloadFields,
    IReadOnlyDictionary<string, string?> Properties,
    IReadOnlyList<string> Warnings);
