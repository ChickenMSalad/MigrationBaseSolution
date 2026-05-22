using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace Migration.Connectors.Targets.Bynder.Clients.Models
{
    public class OAuthToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("scope")]
        public string Scope { get; set; }
        [JsonPropertyName("bearer")]
        public string Bearer { get; set; }

        // Not from server; calculated locally
        public DateTime ExpirationTime { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpirationTime;
    }
}
