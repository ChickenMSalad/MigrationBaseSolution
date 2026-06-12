using Migration.Admin.Api.Authentication;

namespace Migration.Admin.Api.Authentication;

/// <summary>
/// Non-enforcing auth state middleware. It only annotates HttpContext.Items so
/// later sets can add enforcement without changing every endpoint at once.
/// </summary>
public sealed class AdminApiAuthenticationStateMiddleware
{
    public const string AuthEnabledItemKey = "Migration.Admin.Api.AuthEnabled";
    public const string AuthRequiredItemKey = "Migration.Admin.Api.AuthRequired";

    private readonly RequestDelegate _next;

    public AdminApiAuthenticationStateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        AdminApiAuthenticationOptions options)
    {
        context.Items[AuthEnabledItemKey] = options.IsEnabled;
        context.Items[AuthRequiredItemKey] = options.Required;

        await _next(context).ConfigureAwait(false);
    }
}

public static class AdminApiAuthenticationApplicationBuilderExtensions
{
    public static IApplicationBuilder UseMigrationAdminApiAuthenticationState(
        this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<AdminApiAuthenticationStateMiddleware>();
    }
}


