using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Configuration
{
    public class AprimoOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string TokenEndpoint { get; set; } = "/api/oauth2/token";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Scope { get; set; } = "api";
        public string BinaryUploadEndpoint { get; set; } = "/api/core/binaries";
        public string RecordsEndpoint { get; set; } = "/api/core/records";
        public string DefinitionsEndpoint { get; set; } = "/api/core/definitions";
        public string ClassificationsEndpoint { get; set; } = "/api/core/classifications";
    }
}
