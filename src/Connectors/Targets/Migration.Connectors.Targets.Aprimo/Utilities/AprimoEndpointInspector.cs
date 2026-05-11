using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Utilities
{


    public static class AprimoEndpointInspector
    {
        public static async Task<string[]> GetAllowedMethodsAsync(
            HttpClient http,
            Uri baseUri,
            string bearerToken,
            string endpointPath,
            CancellationToken cancellationToken = default)
        {
            if (http is null) throw new ArgumentNullException(nameof(http));
            if (baseUri is null) throw new ArgumentNullException(nameof(baseUri));
            if (string.IsNullOrWhiteSpace(bearerToken)) throw new ArgumentException("Bearer token is required.", nameof(bearerToken));
            if (string.IsNullOrWhiteSpace(endpointPath)) throw new ArgumentException("Endpoint path is required.", nameof(endpointPath));

            http.BaseAddress = baseUri;
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var request = new HttpRequestMessage(HttpMethod.Options, endpointPath);
            using var response = await http.SendAsync(request, cancellationToken);

            if (response.Headers.TryGetValues("Allow", out var values))
            {
                return values
                    .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            return Array.Empty<string>();
        }
    }
}
