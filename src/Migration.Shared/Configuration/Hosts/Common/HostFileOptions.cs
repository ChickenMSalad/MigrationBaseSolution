namespace Migration.Shared.Configuration.Hosts.Common;

public sealed class HostFileOptions
{
    public string? PrimaryFile { get; set; }
    public string? SecondaryFile { get; set; }
    public string? ValidationFile { get; set; }
    public string? FailedRowsFile { get; set; }
    public string? LogFilename { get; set; }
}
