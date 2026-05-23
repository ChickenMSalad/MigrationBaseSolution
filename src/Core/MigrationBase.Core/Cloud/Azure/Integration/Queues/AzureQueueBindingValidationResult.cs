namespace MigrationBase.Core.Cloud.Azure.Integration.Queues;

public sealed class AzureQueueBindingValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IList<string> Errors { get; } = new List<string>();
    public IList<string> Warnings { get; } = new List<string>();

    public void AddError(string message)
    {
        if (!string.IsNullOrWhiteSpace(message)) Errors.Add(message);
    }

    public void AddWarning(string message)
    {
        if (!string.IsNullOrWhiteSpace(message)) Warnings.Add(message);
    }
}
