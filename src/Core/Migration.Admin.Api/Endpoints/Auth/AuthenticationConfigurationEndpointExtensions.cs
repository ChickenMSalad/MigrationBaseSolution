using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class AuthenticationConfigurationEndpointExtensions
{
    public static RouteGroupBuilder MapAuthenticationConfigurationEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/auth/configuration", (
                IConfiguration configuration,
                IWebHostEnvironment environment) =>
            {
                var descriptor = BuildDescriptor(configuration, environment);
                return Results.Ok(descriptor);
            })
            .WithName("GetAuthenticationConfiguration")
            .WithTags("Cloud")
            .WithSummary("Gets the safe authentication configuration plan for future auth enforcement.")
            .Produces<AuthenticationConfigurationDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static AuthenticationConfigurationDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var authMode = Read(
            configuration,
            "Cloud:AuthMode",
            environment.IsDevelopment() ? AuthenticationModes.Disabled : AuthenticationModes.EntraId);

        var authRequired = ReadBool(
            configuration,
            "Cloud:RequiresAuth",
            !environment.IsDevelopment());

        var authority = FirstNonEmptyOrNull(configuration["Auth:Authority"]);
        var audience = FirstNonEmptyOrNull(configuration["Auth:Audience"]);
        var clientId = FirstNonEmptyOrNull(configuration["Auth:ClientId"], configuration["VITE_AUTH_CLIENT_ID"]);
        var tenantId = FirstNonEmptyOrNull(configuration["Auth:TenantId"], configuration["Workspace:TenantId"]);

        var isConfigured = !authRequired ||
                           (!string.IsNullOrWhiteSpace(authority) &&
                            !string.IsNullOrWhiteSpace(audience));

        var warnings = BuildWarnings(
            environment,
            authMode,
            authRequired,
            authority,
            audience,
            clientId,
            tenantId);

        return new AuthenticationConfigurationDescriptor(
            EnvironmentName: environment.EnvironmentName,
            AuthMode: authMode,
            AuthRequired: authRequired,
            IsConfigured: isConfigured,
            AuthorityConfigured: IsConfiguredFlag(authority),
            AudienceConfigured: IsConfiguredFlag(audience),
            ClientIdConfigured: IsConfiguredFlag(clientId),
            TenantIdConfigured: IsConfiguredFlag(tenantId),
            RequiredFrontendSettings:
            [
                "VITE_AUTH_ENABLED",
                "VITE_AUTH_AUTHORITY",
                "VITE_AUTH_CLIENT_ID",
                "VITE_AUTH_AUDIENCE"
            ],
            RequiredApiSettings:
            [
                "Cloud:AuthMode",
                "Cloud:RequiresAuth",
                "Auth:Authority",
                "Auth:Audience"
            ],
            RecommendedTokenClaims:
            [
                "sub",
                "oid",
                "tid",
                "name",
                "preferred_username",
                "roles",
                "scp",
                "aud",
                "iss",
                "exp"
            ],
            Warnings: warnings);
    }

    private static List<string> BuildWarnings(
        IWebHostEnvironment environment,
        string authMode,
        bool authRequired,
        string? authority,
        string? audience,
        string? clientId,
        string? tenantId)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment() &&
            string.Equals(authMode, AuthenticationModes.Disabled, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Auth mode should not be disabled outside development.");
        }

        if (authRequired && string.IsNullOrWhiteSpace(authority))
        {
            warnings.Add("Auth is required but Auth:Authority is not configured.");
        }

        if (authRequired && string.IsNullOrWhiteSpace(audience))
        {
            warnings.Add("Auth is required but Auth:Audience is not configured.");
        }

        if (authRequired && string.IsNullOrWhiteSpace(clientId))
        {
            warnings.Add("Auth is required but frontend Auth:ClientId/VITE_AUTH_CLIENT_ID is not configured.");
        }

        if (!environment.IsDevelopment() && string.IsNullOrWhiteSpace(tenantId))
        {
            warnings.Add("Non-development auth should have an explicit tenant id.");
        }

        if (string.Equals(authMode, AuthenticationModes.Unknown, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Cloud:AuthMode is unknown.");
        }

        return warnings;
    }

    private static string Read(
        IConfiguration configuration,
        string key,
        string fallback)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool ReadBool(
        IConfiguration configuration,
        string key,
        bool fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var parsed)
            ? fallback
            : parsed;
    }

    private static string? FirstNonEmptyOrNull(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? IsConfiguredFlag(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : "<configured>";
}


