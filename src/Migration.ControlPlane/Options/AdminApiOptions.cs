namespace Migration.ControlPlane.Options;

public sealed class AdminApiOptions
{
    public const string SectionName = "ControlPlane";

    /// <summary>
    /// Local control-plane persistence root for projects and run control records.
    /// Use shared storage if the Admin API and worker run on separate machines.
    /// </summary>
    public string StorageRoot { get; init; } = "Runtime/admin-api";
}
