using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Connectors.Sources.WebDam.Clients;
using Migration.Connectors.Sources.WebDam.Configuration;
using Migration.Connectors.Sources.WebDam.Models;
using Migration.Connectors.Sources.WebDam.Services;
using Migration.ControlPlane.Services;

namespace Migration.Connectors.Sources.WebDam.ManifestBuilder;

public sealed class WebDamManifestExportServiceFactory
{
    private readonly ICredentialSetStore _credentialSetStore;
    private readonly IOptions<WebDamOptions> _configuredOptions;
    private readonly ILoggerFactory _loggerFactory;

    public WebDamManifestExportServiceFactory(
        ICredentialSetStore credentialSetStore,
        IOptions<WebDamOptions> configuredOptions,
        ILoggerFactory loggerFactory)
    {
        _credentialSetStore = credentialSetStore ?? throw new ArgumentNullException(nameof(credentialSetStore));
        _configuredOptions = configuredOptions ?? throw new ArgumentNullException(nameof(configuredOptions));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task<WebDamExportService> CreateAsync(
        string? credentialSetId,
        CancellationToken cancellationToken = default)
    {
        var options = CloneConfiguredOptions();

        if (!string.IsNullOrWhiteSpace(credentialSetId))
        {
            var credentialSet = await _credentialSetStore
                .GetAsync(credentialSetId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Credential set '{credentialSetId}' was not found.");

            if (!string.Equals(credentialSet.ConnectorType, "webdam", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Credential set '{credentialSetId}' is for connector '{credentialSet.ConnectorType}', not WebDam.");
            }

            ApplyCredentialValues(options, credentialSet.Values);
        }

        ValidateUsableCredentials(options, credentialSetId);

        var optionsWrapper = Options.Create(options);
        var tokenStore = new WebDamManifestTokenStore();

        var authHttpClient = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute)
        };

        var apiHttpClient = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute)
        };

        var authClient = new WebDamAuthClient(authHttpClient, optionsWrapper, tokenStore);
        var apiClient = new WebDamApiClient(apiHttpClient, authClient, optionsWrapper);

        return new WebDamExportService(
            apiClient,
            _loggerFactory.CreateLogger<WebDamExportService>());
    }

    private WebDamOptions CloneConfiguredOptions()
    {
        var source = _configuredOptions.Value;

        return new WebDamOptions
        {
            BaseUrl = source.BaseUrl,
            ClientId = source.ClientId,
            ClientSecret = source.ClientSecret,
            RedirectUri = source.RedirectUri,
            Username = source.Username,
            Password = source.Password,
            RefreshToken = source.RefreshToken,
            AccessToken = source.AccessToken,
            AccessTokenExpiresAtUtc = source.AccessTokenExpiresAtUtc,
            TokenRefreshSkew = source.TokenRefreshSkew,
            PageSize = source.PageSize
        };
    }

    private static void ApplyCredentialValues(WebDamOptions options, IReadOnlyDictionary<string, string?> values)
    {
        options.BaseUrl = GetString(values, "BaseUrl") ?? options.BaseUrl;
        options.ClientId = GetString(values, "ClientId") ?? options.ClientId;
        options.ClientSecret = GetString(values, "ClientSecret") ?? options.ClientSecret;
        options.RedirectUri = GetString(values, "RedirectUri") ?? options.RedirectUri;
        options.Username = GetString(values, "Username") ?? options.Username;
        options.Password = GetString(values, "Password") ?? options.Password;
        options.RefreshToken = GetString(values, "RefreshToken") ?? options.RefreshToken;
        options.AccessToken = GetString(values, "AccessToken") ?? options.AccessToken;

        var expiresAt = GetDateTimeOffset(values, "AccessTokenExpiresAtUtc");
        if (expiresAt.HasValue)
        {
            options.AccessTokenExpiresAtUtc = expiresAt.Value;
        }

        var pageSize = GetInt(values, "PageSize");
        if (pageSize.HasValue && pageSize.Value > 0)
        {
            options.PageSize = pageSize.Value;
        }
    }

    private static void ValidateUsableCredentials(WebDamOptions options, string? credentialSetId)
    {
        var hasPasswordGrant =
            !string.IsNullOrWhiteSpace(options.ClientId) &&
            !string.IsNullOrWhiteSpace(options.ClientSecret) &&
            !string.IsNullOrWhiteSpace(options.Username) &&
            !string.IsNullOrWhiteSpace(options.Password);

        var hasRefreshGrant =
            !string.IsNullOrWhiteSpace(options.ClientId) &&
            !string.IsNullOrWhiteSpace(options.ClientSecret) &&
            !string.IsNullOrWhiteSpace(options.RefreshToken);

        var hasSeededAccessToken =
            !string.IsNullOrWhiteSpace(options.AccessToken) &&
            options.AccessTokenExpiresAtUtc.HasValue &&
            options.AccessTokenExpiresAtUtc.Value > DateTimeOffset.UtcNow;

        if (hasPasswordGrant || hasRefreshGrant || hasSeededAccessToken)
        {
            return;
        }

        var source = string.IsNullOrWhiteSpace(credentialSetId)
            ? "configured/default WebDam settings"
            : $"credential set '{credentialSetId}'";

        throw new InvalidOperationException(
            $"No usable WebDam credentials were available from {source}. Provide ClientId, ClientSecret, Username, and Password; or ClientId, ClientSecret, and RefreshToken; or AccessToken with AccessTokenExpiresAtUtc.");
    }

    private static string? GetString(IReadOnlyDictionary<string, string?> values, string key)
    {
        if (!TryGetValue(values, key, out var value))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int? GetInt(IReadOnlyDictionary<string, string?> values, string key)
    {
        var text = GetString(values, key);

        return int.TryParse(text, out var value)
            ? value
            : null;
    }

    private static DateTimeOffset? GetDateTimeOffset(IReadOnlyDictionary<string, string?> values, string key)
    {
        var text = GetString(values, key);

        return DateTimeOffset.TryParse(text, out var value)
            ? value
            : null;
    }

    private static bool TryGetValue(IReadOnlyDictionary<string, string?> values, string key, out string? value)
    {
        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }



    private sealed class WebDamManifestTokenStore : IWebDamTokenStore
    {
        private WebDamTokenState? _current;

        public Task<WebDamTokenState?> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_current);

        public Task SaveAsync(WebDamTokenState token, CancellationToken cancellationToken = default)
        {
            _current = token;
            return Task.CompletedTask;
        }
    }
}