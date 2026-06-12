using Migration.ControlPlane.Credentials;

namespace Migration.Admin.Api.Endpoints;

public static class CloudCredentialValueProbeEndpointExtensions
{
    public static RouteGroupBuilder MapCloudCredentialValueProbeEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/credentials/secret-exists", async (
                ICloudCredentialNameResolver resolver,
                ICloudCredentialValueProvider valueProvider,
                HttpContext httpContext,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
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
                var exists = await valueProvider.ExistsAsync(reference, cancellationToken).ConfigureAwait(false);

                return Results.Ok(new
                {
                    reference,
                    exists,
                    valueReturned = false
                });
            })
            .WithName("CheckCloudCredentialSecretExists")
            .WithTags("Cloud")
            .WithSummary("Checks whether a cloud credential secret exists without returning its value.");

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


