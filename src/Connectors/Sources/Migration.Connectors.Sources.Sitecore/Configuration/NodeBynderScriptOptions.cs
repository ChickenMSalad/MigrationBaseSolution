using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Sitecore.Configuration
{
    public sealed class NodeBynderScriptOptions
    {
        public string NodeExecutablePath { get; set; } = "node";
        public string WorkingDirectory { get; set; } = string.Empty;
        public string ScriptPath { get; set; } = string.Empty;
    }
}
