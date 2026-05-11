using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoFieldUpsert
    {
        public string FieldName { get; set; } = default!;

        public string FieldLabel { get; set; } = default!;
        public string FieldId { get; set; } = default!;
        public string LanguageId { get; set; } = default!;

        // If set -> send { value = ... }
        public string? Value { get; set; }

        // If set -> send { values = [...] }
        public List<string>? Values { get; set; }

        public string RawValue { get; set; }
        public bool IsClassification { get; set; } = false;
        public bool IsRecordLink { get; set; } = false;
    }


}
