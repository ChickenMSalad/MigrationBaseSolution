using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Migration.Shared.Configuration.Hosts.Bynder;

namespace Migration.Connectors.Targets.Bynder.Services
{

    public sealed class BynderHostPathResolver
    {
        private readonly BynderHostOptions _options;

        public BynderHostPathResolver(IOptions<BynderHostOptions> options)
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
}
