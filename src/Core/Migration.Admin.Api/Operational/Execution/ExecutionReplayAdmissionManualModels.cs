namespace Migration.Admin.Api.Operational.Execution;

public sealed record ReplayAdmissionManualDecisionRequest(
    Guid ExecutionSessionId,
    string Operator,
    string Reason);

public sealed record ReplayAdmissionManualDecisionResult(
    Guid ExecutionSessionId,
    string Decision,
    string Reason,
    DateTimeOffset CreatedUtc);


