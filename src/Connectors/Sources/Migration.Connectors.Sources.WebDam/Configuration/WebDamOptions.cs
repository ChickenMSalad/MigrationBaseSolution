using System;

namespace Migration.Connectors.Sources.WebDam.Configuration;

public sealed class WebDamOptions
{
    public const string SectionName = "WebDam";

    /// <summary>
    /// Defaults to the documented Webdam API v2 base URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://apiv2.webdamdb.com/";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Optional if you use authorization_code flow.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Optional if you use authorization_code flow.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Optional persisted refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Optional persisted access token.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Optional persisted access token expiration UTC.
    /// </summary>
    public DateTimeOffset? AccessTokenExpiresAtUtc { get; set; }

    /// <summary>
    /// Renew slightly early to avoid edge-of-expiration failures.
    /// </summary>
    public TimeSpan TokenRefreshSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Page size for folder asset listings. Webdam docs show a maximum of 100.
    /// </summary>
    public int PageSize { get; set; } = 100;
}
