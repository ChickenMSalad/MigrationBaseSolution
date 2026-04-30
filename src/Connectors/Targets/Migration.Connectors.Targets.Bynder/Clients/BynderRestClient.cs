using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth2;
using Migration.Connectors.Targets.Bynder.Clients.Models;
using Bynder.Sdk.Settings;

namespace Migration.Connectors.Targets.Bynder.Clients
{
    public class BynderRestClient
    {
        private readonly string _baseUrl;
        private readonly string _tokenUrl = "v6/authentication/oauth2/token";
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _scopes;
        private OAuthToken? _token;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public BynderRestClient(global::Bynder.Sdk.Settings.Configuration configuration)
        {
            _baseUrl = configuration.BaseUrl.ToString();
            _clientId = configuration.ClientId.ToString();
            _clientSecret = configuration.ClientSecret.ToString();
            _scopes = configuration.Scopes.ToString();
        }

        public BynderRestClient(string baseUrl, string clientId, string clientSecret, string scopes)
        {
            _baseUrl = baseUrl;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _scopes = scopes;
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            if (_token == null || _token.IsExpired)
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_token == null)
                    {
                        await AuthenticateAsync();
                    }
                    else if (_token.IsExpired && !string.IsNullOrEmpty(_token.RefreshToken))
                    {
                        await RefreshAccessTokenAsync();
                    }
                    else if (_token.IsExpired)
                    {
                        await AuthenticateAsync();
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return _token?.AccessToken;
        }

        private async Task AuthenticateAsync()
        {
            var client = new RestClient(_baseUrl + _tokenUrl);
            var request = new RestRequest
            {
                Method = Method.Post
            };
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("client_id", _clientId);
            request.AddParameter("client_secret", _clientSecret);

            if (!string.IsNullOrEmpty(_scopes))
            {
                request.AddParameter("scope", _scopes);
            }

            var response = await client.ExecuteAsync<OAuthToken>(request);

            if (!response.IsSuccessful || response.Data == null)
            {
                throw new Exception($"OAuth authentication failed: {response.StatusCode} - {response.Content}");
            }

            _token = response.Data;
            _token.ExpirationTime = DateTime.UtcNow.AddSeconds(_token.ExpiresIn - 30); // 30s buffer
        }

        private async Task RefreshAccessTokenAsync()
        {
            var client = new RestClient(_baseUrl + _tokenUrl);
            var request = new RestRequest
            {
                Method = Method.Post
            };
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", _token!.RefreshToken);
            request.AddParameter("client_id", _clientId);
            request.AddParameter("client_secret", _clientSecret);

            if (!string.IsNullOrEmpty(_scopes))
            {
                request.AddParameter("scope", _scopes);
            }

            var response = await client.ExecuteAsync<OAuthToken>(request);

            if (!response.IsSuccessful || response.Data == null)
            {
                await AuthenticateAsync();
                return;
            }

            var newToken = response.Data;
            newToken.ExpirationTime = DateTime.UtcNow.AddSeconds(newToken.ExpiresIn - 30);
            _token = newToken;
        }

        public async Task<RestClient> GetAuthenticatedClientAsync()
        {
            string? accessToken = await GetAccessTokenAsync();

            var authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(accessToken!, "Bearer");
            var options = new RestClientOptions(_baseUrl)
            {
                Authenticator = authenticator
            };
            var client = new RestClient(options);
            return client;
        }

    }

}