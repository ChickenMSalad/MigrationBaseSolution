using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoLinks
    {
        // Capture ANY link key (files, thumbnail, self, definition, etc.)
        // while still supporting strongly-typed access for common ones if you want later.
        [JsonExtensionData]
        public IDictionary<string, object>? AdditionalLinks { get; set; }
    }
}
