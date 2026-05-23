namespace MigrationBase.Core.Cloud.Azure.Workers;

public sealed record AzureWorkerLifecycleReportResult
{
    public bool Accepted { get; init; }

    public string? Message { get; init; }

    public DateTimeOffset ReportedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public static AzureWorkerLifecycleReportResult Success(string? message = null) =>
        new() { Accepted = true, Message = message };

    public static AzureWorkerLifecycleReportResult Rejected(string message) =>
        new() { Accepted = false, Message = message };
}
