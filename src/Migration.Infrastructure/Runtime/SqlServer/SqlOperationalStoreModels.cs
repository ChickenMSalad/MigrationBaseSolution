using System;

namespace Migration.Infrastructure.Runtime.SqlServer;

public sealed record SqlOperationalWorkItem(
    long WorkItemId,
    Guid RunId,
    long? ManifestRowId,
    string WorkType,
    string Status,
    int AttemptCount,
    int MaxAttempts,
    string? PayloadJson,
    DateTime? LeaseExpiresAtUtc);

public sealed record SqlClaimWorkItemsRequest(
    string WorkerId,
    int BatchSize,
    int LeaseSeconds,
    Guid? RunId = null);

public sealed record SqlFailWorkItemRequest(
    long WorkItemId,
    string WorkerId,
    string? ErrorCode,
    string? ErrorMessage,
    string? ExceptionType,
    bool IsRetryable,
    int RetryDelaySeconds,
    string? FailureJson);
