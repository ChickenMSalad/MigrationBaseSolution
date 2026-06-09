using Migration.ControlPlane.Credentials;

namespace Migration.Admin.Api.Endpoints;

public static class CloudCredentialDiagnosticsEndpointExtensions
{
    public static RouteGroupBuilder MapCloudCredentialDiagnosticsEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/credentials/provider", (
                IConfiguration configuration) =>
            {
                var descriptor = CloudCredentialRegistrationExtensions.BuildDescriptor(configuration);
                return Results.Ok(descriptor);
            })
            .WithName("GetCloudCredentialProvider")
            .WithTags("Cloud")
            .WithSummary("Gets safe cloud credential provider diagnostics.");

        api.MapGet("/cloud/credentials/secret-name", (
                ICloudCredentialNameResolver resolver,
                HttpContext httpContext,
                IConfiguration configuration) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var role = FirstNonEmpty(httpContext.Request.Query["role"].FirstOrDefault(), "source");
                var connector = FirstNonEmpty(httpContext.Request.Query["connector"].FirstOrDefault(), "aem");
                var credentialSet = FirstNonEmpty(httpContext.Request.Query["credentialSet"].FirstOrDefault(), "default");
                var secretKind = FirstNonEmpty(httpContext.Request.Query["secretKind"].FirstOrDefault(), CloudCredentialSecretKinds.Password);

                var reference = resolver.Resolve(workspaceId, role, connector, credentialSet, secretKind);
                return Results.Ok(reference);
            })
            .WithName("ResolveCloudCredentialSecretName")
            .WithTags("Cloud")
            .WithSummary("Resolves a deterministic cloud credential secret name without returning secret values.");

        return api;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}


