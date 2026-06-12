using Migration.Admin.Api.Authentication;

namespace Migration.Admin.Api.Registration;

public static class AdminApiAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationAdminApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var options = BuildOptions(configuration, environment);
        services.AddSingleton(options);

        // Placeholder only.
        // A later set can add Microsoft.Identity.Web/JWT Bearer registration here
        // once package governance and auth enforcement timing are agreed.
        return services;
    }

    private static AdminApiAuthenticationOptions BuildOptions(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var mode = Read(
            configuration,
            "Cloud:AuthMode",
            environment.IsDevelopment() ? "disabled" : "entraId");

        var required = ReadBool(
            configuration,
            "Cloud:RequiresAuth",
            !environment.IsDevelopment());

        return new AdminApiAuthenticationOptions
        {
            Mode = mode,
            Required = required,
            Authority = EmptyToNull(configuration["Auth:Authority"]),
            Audience = EmptyToNull(configuration["Auth:Audience"]),
            TenantId = EmptyToNull(configuration["Auth:TenantId"] ?? configuration["Workspace:TenantId"])
        };
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

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}


