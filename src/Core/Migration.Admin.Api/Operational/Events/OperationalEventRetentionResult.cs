namespace Migration.Admin.Api.Operational.Events;

public sealed record OperationalEventRetentionResult(
    int RetentionDays,
    int DeletedEvents,
    DateTimeOffset CutoffUtc);
