using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public class AprimoRecordPagedCollection
    {
        [JsonProperty("items")]
        public AprimoRecord[]? Items { get; set; }

        [JsonProperty("page")]
        public int? Page { get; set; }

        [JsonProperty("pageSize")]
        public int? PageSize { get; set; }

        [JsonProperty("totalCount")]
        public long? TotalCount { get; set; }
    }
}
