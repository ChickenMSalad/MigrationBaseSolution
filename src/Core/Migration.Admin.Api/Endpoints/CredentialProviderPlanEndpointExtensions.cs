using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class CredentialProviderPlanEndpointExtensions
{
    public static RouteGroupBuilder MapCredentialProviderPlanEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/credential-provider-plan", (
                IConfiguration configuration,
                IWebHostEnvironment environment,
                HttpContext httpContext) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var tenantId = FirstNonEmptyOrNull(
                    httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault(),
                    configuration["Workspace:TenantId"]);

                var descriptor = BuildDescriptor(configuration, environment, workspaceId, tenantId);
                return Results.Ok(descriptor);
            })
            .WithName("GetCredentialProviderPlan")
            .WithTags("Cloud")
            .WithSummary("Gets the safe credential provider plan for cloud-readiness diagnostics.")
            .Produces<CredentialProviderPlanDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static CredentialProviderPlanDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string workspaceId,
        string? tenantId)
    {
        var credentialMode = Read(
            configuration,
            "Cloud:CredentialMode",
            environment.IsDevelopment() ? "userSecrets" : "unknown");

        var keyVaultName = FirstNonEmptyOrNull(
            configuration["Cloud:KeyVaultName"],
            configuration["KeyVault:Name"]);

        var keyVaultUri = FirstNonEmptyOrNull(
            configuration["Cloud:KeyVaultUri"],
            configuration["KeyVault:Uri"]);

        var providerKind = InferProviderKind(environment, credentialMode, keyVaultName, keyVaultUri);

        var secretNamePrefix = BuildSecretNamePrefix(workspaceId, tenantId);

        var warnings = BuildWarnings(
            environment,
            credentialMode,
            providerKind,
            keyVaultName,
            keyVaultUri,
            workspaceId,
            tenantId);

        return new CredentialProviderPlanDescriptor(
            EnvironmentName: environment.EnvironmentName,
            CredentialMode: credentialMode,
            WorkspaceId: NormalizeSegment(workspaceId),
            TenantId: tenantId,
            ProviderKind: providerKind,
            UsesLocalSecrets: string.Equals(providerKind, CredentialProviderKinds.LocalDevelopment, StringComparison.OrdinalIgnoreCase),
            UsesUserSecrets: string.Equals(providerKind, CredentialProviderKinds.UserSecrets, StringComparison.OrdinalIgnoreCase),
            UsesKeyVault: string.Equals(providerKind, CredentialProviderKinds.KeyVault, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(providerKind, CredentialProviderKinds.ManagedIdentityKeyVault, StringComparison.OrdinalIgnoreCase),
            UsesManagedIdentity: string.Equals(providerKind, CredentialProviderKinds.ManagedIdentityKeyVault, StringComparison.OrdinalIgnoreCase),
            KeyVaultName: keyVaultName,
            KeyVaultUri: keyVaultUri,
            SecretNamePrefix: secretNamePrefix,
            SupportedSecretKinds:
            [
                "username",
                "password",
                "bearerToken",
                "apiKey",
                "apiSecret",
                "oauthClientId",
                "oauthClientSecret",
                "connectionString",
                "accessKeyId",
                "secretAccessKey"
            ],
            Warnings: warnings);
    }

    private static string InferProviderKind(
        IWebHostEnvironment environment,
        string credentialMode,
        string? keyVaultName,
        string? keyVaultUri)
    {
        if (string.Equals(credentialMode, "managedIdentity", StringComparison.OrdinalIgnoreCase))
        {
            return CredentialProviderKinds.ManagedIdentityKeyVault;
        }

        if (string.Equals(credentialMode, "keyVault", StringComparison.OrdinalIgnoreCase))
        {
            return CredentialProviderKinds.KeyVault;
        }

        if (string.Equals(credentialMode, "userSecrets", StringComparison.OrdinalIgnoreCase))
        {
            return CredentialProviderKinds.UserSecrets;
        }

        if (environment.IsDevelopment())
        {
            return CredentialProviderKinds.LocalDevelopment;
        }

        if (!string.IsNullOrWhiteSpace(keyVaultName) || !string.IsNullOrWhiteSpace(keyVaultUri))
        {
            return CredentialProviderKinds.KeyVault;
        }

        return CredentialProviderKinds.Unknown;
    }

    private static List<string> BuildWarnings(
        IWebHostEnvironment environment,
        string credentialMode,
        string providerKind,
        string? keyVaultName,
        string? keyVaultUri,
        string workspaceId,
        string? tenantId)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment() &&
            string.Equals(providerKind, CredentialProviderKinds.UserSecrets, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should not use user-secrets credential resolution.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(providerKind, CredentialProviderKinds.LocalDevelopment, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should not use local-development credential resolution.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(providerKind, CredentialProviderKinds.Unknown, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Credential provider kind could not be inferred for a non-development environment.");
        }

        if ((string.Equals(providerKind, CredentialProviderKinds.KeyVault, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(providerKind, CredentialProviderKinds.ManagedIdentityKeyVault, StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrWhiteSpace(keyVaultName) &&
            string.IsNullOrWhiteSpace(keyVaultUri))
        {
            warnings.Add("Key Vault credential mode is selected but no Key Vault name or URI is configured.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(workspaceId, "default", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development credential secret names should include an explicit workspace id.");
        }

        if (!environment.IsDevelopment() &&
            string.IsNullOrWhiteSpace(tenantId))
        {
            warnings.Add("Non-development credential secret names should include an explicit tenant id once tenancy is enforced.");
        }

        if (string.Equals(credentialMode, "local", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Cloud:CredentialMode=local is for development only.");
        }

        return warnings;
    }

    private static string BuildSecretNamePrefix(string workspaceId, string? tenantId)
    {
        var normalizedWorkspaceId = NormalizeSegment(workspaceId);

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return $"migration--workspace-{normalizedWorkspaceId}";
        }

        return $"migration--tenant-{NormalizeSegment(tenantId)}--workspace-{normalizedWorkspaceId}";
    }

    private static string Read(
        IConfiguration configuration,
        string key,
        string fallback)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? FirstNonEmptyOrNull(params string?[] values)
    {
        var value = FirstNonEmpty(values);
        return string.IsNullOrWhiteSpace(value) ? null : value;
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

    private static string NormalizeSegment(string value)
    {
        var sanitized = new string(value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-')
            .ToArray());

        while (sanitized.Contains("--", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(sanitized)
            ? "default"
            : sanitized.Trim('-');
    }
}


