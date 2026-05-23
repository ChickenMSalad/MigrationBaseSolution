namespace MigrationBase.Core.Cloud.Azure.Runtime.Workers.Closeout;

/// <summary>
/// Provides a lightweight readiness view for the worker runtime foundation before queue dispatcher wiring begins.
/// </summary>
public sealed class AzureWorkerRuntimeFoundationReadiness
{
    private readonly List<string> _notes = new();

    public IReadOnlyList<string> Notes => _notes;

    public bool IsReadyForQueueDispatcher { get; private set; }

    public static AzureWorkerRuntimeFoundationReadiness Ready(params string[] notes)
    {
        var readiness = new AzureWorkerRuntimeFoundationReadiness
        {
            IsReadyForQueueDispatcher = true
        };

        foreach (var note in notes.Where(static value => !string.IsNullOrWhiteSpace(value)))
        {
            readiness._notes.Add(note);
        }

        return readiness;
    }
}
