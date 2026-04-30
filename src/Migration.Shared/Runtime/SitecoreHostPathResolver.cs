using Microsoft.Extensions.Options;
using Migration.Shared.Configuration.Hosts.Sitecore;

namespace Migration.Shared.Runtime;

public sealed class SitecoreHostPathResolver
{
    private readonly SitecoreHostOptions _options;

    public SitecoreHostPathResolver(IOptions<SitecoreHostOptions> options)
    {
        _options = options.Value;
    }

    public string GetSourceFile(string fileName) =>
        Path.Combine(_options.Paths.SourceDirectory ?? string.Empty, fileName);

    public string GetOutputFile(string fileName) =>
        Path.Combine(_options.Paths.OutputDirectory ?? string.Empty, fileName);

    public string GetReportFile(string fileName) =>
        Path.Combine(_options.Paths.ReportDirectory ?? string.Empty, fileName);
}
