using System;

namespace Migration.Connectors.Sources.WebDam.Models;

public sealed class WebDamTokenState
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "bearer";
    public int ExpiresInSeconds { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }

    public bool IsExpired(TimeSpan skew) => DateTimeOffset.UtcNow >= ExpiresAtUtc.Subtract(skew);
}
