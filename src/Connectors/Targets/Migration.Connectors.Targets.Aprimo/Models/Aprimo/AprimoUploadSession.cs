using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoUploadSession
    {
        [JsonProperty("token")]
        public string Token { get; set; } = default!;

        [JsonProperty("sasUrl")]
        public string SasUrl { get; set; } = default!;
    }
}
