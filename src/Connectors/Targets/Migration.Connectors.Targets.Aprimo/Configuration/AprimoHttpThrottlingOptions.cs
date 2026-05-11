using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Configuration
{
    public sealed class AprimoHttpThrottlingOptions
    {
        /// <summary>
        /// Max requests per second to Aprimo host across ALL running instances sharing the same SQLite file.
        /// Set to 0 to disable throttling.
        /// </summary>
        public int MaxRequestsPerSecond { get; set; } = 12;

        /// <summary>
        /// SQLite file path used for tracking + rate limiting. MUST be shared across instances to coordinate.
        /// If null/empty, defaults to AppContext.BaseDirectory\aprimo_http.sqlite
        /// </summary>
        public string? SqlitePath { get; set; }
    }
}

