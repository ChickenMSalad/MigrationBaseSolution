namespace Migration.Shared.Configuration.Hosts.Common;

public sealed class HostPathOptions
{
    public string? SourceDirectory { get; set; }
    public string? ImportsSourceDirectory { get; set; }
    public string? OutputDirectory { get; set; }
    public string? ReportDirectory { get; set; }
    public string? TempDirectory { get; set; }
    public string? DumpDirectory { get; set; }
}
