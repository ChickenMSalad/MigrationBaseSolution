using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoFieldValue
    {
        public string FieldId { get; set; } = default!;
        public Dictionary<string, object> LocalizedValues { get; set; } = new();
    }
}
