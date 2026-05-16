namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Standard JSON error response used by Admin API endpoints.
/// Serialized shape intentionally remains:
/// { "error": "..." }
/// </summary>
public sealed record AdminApiErrorResponse(string Error);

/// <summary>
/// Standard JSON response for run-start requests blocked by the preflight gate.
/// Serialized shape intentionally remains:
/// {
///   "error": "...",
///   "projectId": "...",
///   "requiredAction": "..."
/// }
/// </summary>
public sealed record RunPreflightGateBlockedResponse(
    string Error,
    string ProjectId,
    string RequiredAction);

/// <summary>
/// Standard JSON response for terminal run cancellation conflicts.
/// Serialized shape intentionally remains:
/// { "error": "..." }
/// </summary>
public sealed record RunStateConflictResponse(string Error);
