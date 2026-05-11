using Migration.Connectors.Targets.Aprimo.Clients;
using Migration.Connectors.Targets.Aprimo.Configuration;
using Migration.Shared.Configuration.Hosts.Aprimo;
using Migration.Shared.Configuration.Infrastructure;
using Microsoft.Extensions.Options;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Clients
{
    public sealed class AprimoAuthClient : IAprimoAuthClient
    {
        private readonly HttpClient _httpClient;
        private readonly AprimoOptions _options;

        public AprimoAuthClient(HttpClient httpClient, AprimoOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        private string? _cachedToken;
        private DateTimeOffset _tokenExpiresAt;
        private DateTimeOffset _tokenNotBeforeAt;

        private static readonly TimeSpan NotBeforeBuffer = TimeSpan.FromSeconds(1);   // safety
        private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromMinutes(1);   // refresh early

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            // Fast path: cached token is usable (checks BOTH nbf and exp)
            var now = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(_cachedToken) &&
                now >= _tokenNotBeforeAt + NotBeforeBuffer &&
                now < _tokenExpiresAt - ExpiryBuffer)
            {
                return _cachedToken!;
            }

            await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Double-check inside lock
                now = DateTimeOffset.UtcNow;
                if (!string.IsNullOrWhiteSpace(_cachedToken) &&
                    now >= _tokenNotBeforeAt + NotBeforeBuffer &&
                    now < _tokenExpiresAt - ExpiryBuffer)
                {
                    return _cachedToken!;
                }

                var tokenUri = new Uri(
                    new Uri(_options.BaseUrl.Replace(".dam", "").TrimEnd('/') + "/"),
                    _options.TokenEndpoint.TrimStart('/'));

                // Use FormUrlEncodedContent to avoid any subtle formatting issues
                var form = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["scope"] = _options.Scope // (or _options.Scopes) make sure this is actually populated
                };

                //var form = new Dictionary<string, string>
                //{
                //    ["grant_type"] = "password",
                //    ["client_id"] = "NZF72BOS-KVGT",
                //    ["client_secret"] = _options.ClientSecret,
                //    ["username"] = "dtaylor@ntara.com",
                //    ["password"] = "6fdb9813d0db4547b8978b9c3bf18d1f",
                //    ["scope"] = _options.Scope // (or _options.Scopes) make sure this is actually populated,

                //};

                using var request = new HttpRequestMessage(HttpMethod.Post, tokenUri)
                {
                    Content = new FormUrlEncodedContent(form)
                };
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var accessToken = root.GetProperty("access_token").GetString()?.Trim()?.Trim('"');
                if (string.IsNullOrWhiteSpace(accessToken))
                    throw new InvalidOperationException("Token response did not include access_token.");

                DumpJwt(accessToken);

                var expiresIn = root.TryGetProperty("expires_in", out var expiresProp) && expiresProp.TryGetInt32(out var exp)
                    ? exp
                    : 3600;

                // Decode JWT to enforce nbf
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                DateTimeOffset nbf = DateTimeOffset.UtcNow; // default if opaque token
                if (handler.CanReadToken(accessToken))
                {
                    var jwt = handler.ReadJwtToken(accessToken);
                    nbf = new DateTimeOffset(jwt.ValidFrom, TimeSpan.Zero);

                    var utcNow = DateTimeOffset.UtcNow;
                    if (nbf > utcNow)
                    {
                        var delay = (nbf - utcNow) + NotBeforeBuffer;
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Only cache AFTER we are past nbf
                _cachedToken = accessToken;
                _tokenNotBeforeAt = nbf;
                _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

                return _cachedToken!;
            }
            finally
            {
                _tokenLock.Release();
            }
        }


        public static void DumpJwt(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                Console.WriteLine("Token is not a readable JWT (might be opaque).");
                return;
            }

            var jwt = handler.ReadJwtToken(token);

            Console.WriteLine($"UTC Now (DateTime): {string.Format("{0:yyyy-MM-ddTHH:mm:ss.FFFZ}", DateTime.UtcNow)}");

            Console.WriteLine($"iss: {jwt.Issuer}");
            Console.WriteLine($"aud: {string.Join(", ", jwt.Audiences)}");
            Console.WriteLine($"exp: {jwt.ValidTo:o} (UTC)");
            Console.WriteLine($"nbf: {jwt.ValidFrom:o} (UTC)");
        }
    }
}



