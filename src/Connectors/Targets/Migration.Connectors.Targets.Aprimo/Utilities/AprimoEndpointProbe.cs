using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Utilities
{

    public static class AprimoEndpointProbe
    {
        public static async Task ProbeAsync(
            HttpClient http,
            Uri baseUri,
            string bearerToken,
            HttpMethod method,
            string endpointPath,
            string? jsonBody = null,
            CancellationToken cancellationToken = default)
        {
            if (http is null) throw new ArgumentNullException(nameof(http));
            if (baseUri is null) throw new ArgumentNullException(nameof(baseUri));
            if (string.IsNullOrWhiteSpace(bearerToken)) throw new ArgumentException("Bearer token is required.", nameof(bearerToken));
            if (string.IsNullOrWhiteSpace(endpointPath)) throw new ArgumentException("Endpoint path is required.", nameof(endpointPath));

            var requestUri = new Uri(baseUri, endpointPath);

            using var request = new HttpRequestMessage(method, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (jsonBody != null)
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            using var response = await http.SendAsync(request, cancellationToken);

            Console.WriteLine($"=== {method} {requestUri} ===");
            Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");

            foreach (var header in response.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            foreach (var header in response.Content.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                Console.WriteLine("Body:");
                Console.WriteLine(body);
            }

            Console.WriteLine();
        }
    }
}
