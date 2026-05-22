using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Migration.Admin.Api.Endpoints.Operational.Credentials;

public static class OperationalConnectorCredentialVaultEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalConnectorCredentialVaultEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/operational/connectors/credentials")
            .WithTags("Operational Connector Credentials");

        group.MapGet("/summary", () => Results.Ok(ConnectorCredentialVaultSummaryResponse.Default()));
        group.MapGet("/catalog", () => Results.Ok(ConnectorCredentialCatalogItem.DefaultCatalog()));
        group.MapPost("/validate", (ConnectorCredentialValidationRequest request) =>
        {
            var findings = new List<string>();

            if (string.IsNullOrWhiteSpace(request.ConnectorKey))
            {
                findings.Add("Connector key is required.");
            }

            if (string.IsNullOrWhiteSpace(request.SecretReferenceName))
            {
                findings.Add("Secret reference name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.SecretProvider))
            {
                findings.Add("Secret provider is required.");
            }

            return Results.Ok(new ConnectorCredentialValidationResponse(
                findings.Count == 0,
                findings.Count == 0 ? "Credential reference is structurally valid." : "Credential reference requires attention.",
                findings));
        });

        return endpoints;
    }

    private sealed record ConnectorCredentialVaultSummaryResponse(
        int RegisteredCredentialReferences,
        int MissingSecretReferences,
        int ConnectorsRequiringCredentials,
        IReadOnlyList<string> SupportedSecretProviders)
    {
        public static ConnectorCredentialVaultSummaryResponse Default() => new(
            RegisteredCredentialReferences: 0,
            MissingSecretReferences: 0,
            ConnectorsRequiringCredentials: 0,
            SupportedSecretProviders: new[] { "AzureKeyVault", "Environment", "LocalDevelopment" });
    }

    private sealed record ConnectorCredentialCatalogItem(
        string ConnectorKey,
        string DisplayName,
        string Direction,
        IReadOnlyList<string> RequiredSecretNames)
    {
        public static IReadOnlyList<ConnectorCredentialCatalogItem> DefaultCatalog() => new[]
        {
            new ConnectorCredentialCatalogItem("source.azureblob", "Azure Blob Source", "Source", new[] { "ConnectionString" }),
            new ConnectorCredentialCatalogItem("target.azureblob", "Azure Blob Target", "Target", new[] { "ConnectionString" }),
            new ConnectorCredentialCatalogItem("target.bynder", "Bynder Target", "Target", new[] { "ClientId", "ClientSecret", "BaseUrl" }),
            new ConnectorCredentialCatalogItem("source.sharepoint", "SharePoint Source", "Source", new[] { "TenantId", "ClientId", "ClientSecret" })
        };
    }

    private sealed record ConnectorCredentialValidationRequest(
        string? ConnectorKey,
        string? SecretProvider,
        string? SecretReferenceName);

    private sealed record ConnectorCredentialValidationResponse(
        bool IsValid,
        string Message,
        IReadOnlyList<string> Findings);
}
