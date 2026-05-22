using System.Security.Claims;

namespace Migration.Admin.Api.Endpoints;

public static class AdminSecurityStatusEndpointExtensions
{
    public static IEndpointRouteBuilder MapAdminSecurityStatusEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/security")
            .WithTags("Admin Security");

        group.MapGet("/status", (ClaimsPrincipal user, IConfiguration configuration, IWebHostEnvironment environment) =>
        {
            var authSection = configuration.GetSection("AdminApi:Authentication");
            var entraSection = configuration.GetSection("AdminApi:Authentication:EntraId");
            var requiredRoles = configuration.GetSection("AdminApi:Authorization:RequiredRoles").Get<string[]>() ?? Array.Empty<string>();

            var claims = user.Claims
                .Select(claim => new AdminSecurityClaimDto(claim.Type, claim.Value))
                .OrderBy(claim => claim.Type, StringComparer.OrdinalIgnoreCase)
                .ThenBy(claim => claim.Value, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Results.Ok(new AdminSecurityStatusDto(
                EnvironmentName: environment.EnvironmentName,
                AuthenticationConfigured: authSection.Exists(),
                EntraIdConfigured: entraSection.Exists(),
                AuthorityConfigured: !string.IsNullOrWhiteSpace(entraSection["Authority"]),
                AudienceConfigured: !string.IsNullOrWhiteSpace(entraSection["Audience"]),
                RequiredRoles: requiredRoles,
                IsAuthenticated: user.Identity?.IsAuthenticated == true,
                AuthenticationType: user.Identity?.AuthenticationType,
                UserName: user.Identity?.Name,
                Claims: claims));
        })
        .WithName("GetAdminSecurityStatus")
        .WithSummary("Returns Admin API authentication and authorization configuration state.");

        return endpoints;
    }

    private sealed record AdminSecurityStatusDto(
        string EnvironmentName,
        bool AuthenticationConfigured,
        bool EntraIdConfigured,
        bool AuthorityConfigured,
        bool AudienceConfigured,
        string[] RequiredRoles,
        bool IsAuthenticated,
        string? AuthenticationType,
        string? UserName,
        AdminSecurityClaimDto[] Claims);

    private sealed record AdminSecurityClaimDto(string Type, string Value);
}
