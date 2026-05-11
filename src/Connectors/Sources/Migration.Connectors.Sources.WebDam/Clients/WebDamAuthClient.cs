using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Migration.Connectors.Sources.WebDam.Configuration;
using Migration.Connectors.Sources.WebDam.Models;

namespace Migration.Connectors.Sources.WebDam.Clients;

public sealed class WebDamAuthClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly WebDamOptions _options;
    private readonly IWebDamTokenStore _tokenStore;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public WebDamAuthClient(
        HttpClient httpClient,
        IOptions<WebDamOptions> options,
        IWebDamTokenStore tokenStore)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);
        }
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = await EnsureTokenAsync(cancellationToken).ConfigureAwait(false);
        return token.AccessToken;
    }

    public async Task<WebDamTokenState> AuthenticateWithPasswordAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException("Username and Password must be configured for password grant.");
        }

        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["username"] = _options.Username!,
                ["password"] = _options.Password!
            },
            cancellationToken).ConfigureAwait(false);

        await _tokenStore.SaveAsync(token, cancellationToken).ConfigureAwait(false);
        return token;
    }

    public async Task<WebDamTokenState> AuthenticateWithAuthorizationCodeAsync(
        string authorizationCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode))
        {
            throw new ArgumentException("Authorization code is required.", nameof(authorizationCode));
        }

        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = authorizationCode,
                ["redirect_uri"] = _options.RedirectUri,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret
            },
            cancellationToken).ConfigureAwait(false);

        await _tokenStore.SaveAsync(token, cancellationToken).ConfigureAwait(false);
        return token;
    }

    public async Task<WebDamTokenState> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
        }

        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = _options.RedirectUri
            },
            cancellationToken).ConfigureAwait(false);

        await _tokenStore.SaveAsync(token, cancellationToken).ConfigureAwait(false);
        return token;
    }

    private async Task<WebDamTokenState> EnsureTokenAsync(CancellationToken cancellationToken)
    {
        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var current = await _tokenStore.GetAsync(cancellationToken).ConfigureAwait(false);
            if (current is not null && !current.IsExpired(_options.TokenRefreshSkew))
            {
                return current;
            }

            if (current is not null && !string.IsNullOrWhiteSpace(current.RefreshToken))
            {
                return await RefreshAsync(current.RefreshToken, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(_options.RefreshToken))
            {
                return await RefreshAsync(_options.RefreshToken!, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
            {
                return await AuthenticateWithPasswordAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(_options.AccessToken) && _options.AccessTokenExpiresAtUtc.HasValue)
            {
                var seeded = new WebDamTokenState
                {
                    AccessToken = _options.AccessToken!,
                    RefreshToken = _options.RefreshToken ?? string.Empty,
                    ExpiresAtUtc = _options.AccessTokenExpiresAtUtc.Value,
                    ExpiresInSeconds = (int)Math.Max(
                        0,
                        (_options.AccessTokenExpiresAtUtc.Value - DateTimeOffset.UtcNow).TotalSeconds)
                };

                if (!seeded.IsExpired(_options.TokenRefreshSkew))
                {
                    await _tokenStore.SaveAsync(seeded, cancellationToken).ConfigureAwait(false);
                    return seeded;
                }
            }

            throw new InvalidOperationException(
                "No usable WebDam credentials were available. Configure password grant, a refresh token, or call AuthenticateWithAuthorizationCodeAsync first.");
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<WebDamTokenState> RequestTokenAsync(
        IReadOnlyDictionary<string, string> formValues,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "oauth2/token")
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new WebDamException("WebDam token request failed.")
            {
                StatusCode = response.StatusCode,
                ResponseBody = body
            };
        }

        var dto = JsonSerializer.Deserialize<WebDamOAuthTokenResponse>(body, JsonOptions)
                  ?? throw new WebDamException("Could not deserialize WebDam token response.");

        return new WebDamTokenState
        {
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            TokenType = dto.TokenType,
            ExpiresInSeconds = dto.ExpiresIn,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(dto.ExpiresIn)
        };
    }
}
