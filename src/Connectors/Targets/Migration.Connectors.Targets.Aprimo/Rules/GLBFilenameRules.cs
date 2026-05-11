using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Rules
{
    public sealed class FilenameCategoryRules
    {
        [JsonProperty("rules")]
        public List<FilenameCategoryRule> Rules { get; set; } = new();
    }

    public sealed class FilenameCategoryRule
    {
        /// <summary>
        /// Regular expression used to match against the filename.
        /// </summary>
        [JsonProperty("pattern")]
        public string Pattern { get; set; } = default!;

        /// <summary>
        /// Resulting category when the regex matches.
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; } = default!;

        /// <summary>
        /// Optional: original table value / human-readable source.
        /// </summary>
        [JsonProperty("source")]
        public string? Source { get; set; }
    }

}
