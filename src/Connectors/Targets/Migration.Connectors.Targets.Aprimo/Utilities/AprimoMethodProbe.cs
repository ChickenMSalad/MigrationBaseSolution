using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Utilities
{


    public static class AprimoMethodProbe
    {
        public static async Task ProbeGetAsync(
            HttpClient http,
            Uri baseUri,
            string bearerToken,
            string endpointPath,
            CancellationToken cancellationToken = default)
        {

            http.BaseAddress = baseUri;
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var response = await http.GetAsync(endpointPath, cancellationToken);

            Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");

            if (response.Headers.TryGetValues("Allow", out var allowValues))
            {
                Console.WriteLine("Allow: " + string.Join(", ", allowValues));
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                Console.WriteLine("Body:");
                Console.WriteLine(body);
            }
        }
    }
}
