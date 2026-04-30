using Migration.ControlPlane.Models;
using Migration.ControlPlane.Services;
using Migration.Orchestration.Abstractions;

namespace Migration.Admin.Api.Endpoints;

public static class CredentialEndpointExtensions
{
    public static RouteGroupBuilder MapCredentialEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/credentials", async (ICredentialSetStore store, CancellationToken ct) =>
        {
            var credentials = await store.ListAsync(ct).ConfigureAwait(false);
            return Results.Ok(credentials.Select(CredentialSetFactory.Sanitize));
        })
        .WithTags("Credentials")
        .WithSummary("List saved credential sets with secret values masked.");

        api.MapGet("/credentials/{credentialSetId}", async (string credentialSetId, ICredentialSetStore store, CancellationToken ct) =>
        {
            var credentialSet = await store.GetAsync(credentialSetId, ct).ConfigureAwait(false);
            return credentialSet is null ? Results.NotFound() : Results.Ok(CredentialSetFactory.Sanitize(credentialSet));
        })
        .WithTags("Credentials")
        .WithSummary("Get a saved credential set with secret values masked.");

        api.MapPost("/credentials", async (CreateCredentialSetRequest request, CredentialSetFactory factory, ICredentialSetStore store, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName) ||
                string.IsNullOrWhiteSpace(request.ConnectorType) ||
                string.IsNullOrWhiteSpace(request.ConnectorRole))
            {
                return Results.BadRequest(new { error = "DisplayName, ConnectorType, and ConnectorRole are required." });
            }

            if (request.Values is null || request.Values.Count == 0)
            {
                return Results.BadRequest(new { error = "At least one credential value is required." });
            }

            var credentialSet = factory.Create(request);
            await store.SaveAsync(credentialSet, ct).ConfigureAwait(false);
            return Results.Created($"/api/credentials/{credentialSet.CredentialSetId}", CredentialSetFactory.Sanitize(credentialSet));
        })
        .WithTags("Credentials")
        .WithSummary("Create a local-development credential set.");

        api.MapPut("/credentials/{credentialSetId}", async (string credentialSetId, UpdateCredentialSetRequest request, CredentialSetFactory factory, ICredentialSetStore store, CancellationToken ct) =>
        {
            var existing = await store.GetAsync(credentialSetId, ct).ConfigureAwait(false);
            if (existing is null) return Results.NotFound();

            var updated = factory.Update(existing, request);
            await store.SaveAsync(updated, ct).ConfigureAwait(false);
            return Results.Ok(CredentialSetFactory.Sanitize(updated));
        })
        .WithTags("Credentials")
        .WithSummary("Update a credential set.");

        api.MapDelete("/credentials/{credentialSetId}", async (string credentialSetId, ICredentialSetStore store, CancellationToken ct) =>
            await store.DeleteAsync(credentialSetId, ct).ConfigureAwait(false) ? Results.NoContent() : Results.NotFound())
        .WithTags("Credentials")
        .WithSummary("Delete a credential set.");

        api.MapPost("/credentials/{credentialSetId}/test", async (string credentialSetId, ICredentialSetStore store, CredentialTestService tester, CancellationToken ct) =>
        {
            var credentialSet = await store.GetAsync(credentialSetId, ct).ConfigureAwait(false);
            if (credentialSet is null) return Results.NotFound();

            var result = await tester.TestAsync(credentialSet, ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithTags("Credentials")
        .WithSummary("Run local validation for a credential set.");

        api.MapGet("/connectors/{connectorType}/schema", (string connectorType, string? role, IConnectorCatalog catalog) =>
        {
            var matches = catalog.GetSources().Cast<object>()
                .Concat(catalog.GetTargets().Cast<object>())
                .Concat(catalog.GetManifestProviders().Cast<object>())
                .Where(x => MatchesConnector(x, connectorType))
                .ToArray();

            if (!string.IsNullOrWhiteSpace(role))
            {
                matches = matches.Where(x => MatchesRole(x, role)).ToArray();
            }

            return matches.Length == 0 ? Results.NotFound(new { error = $"Connector '{connectorType}' was not found." }) : Results.Ok(matches[0]);
        })
        .WithTags("Connectors")
        .WithSummary("Get connector descriptor/schema metadata for dynamic setup forms.");

        return api;
    }

    private static bool MatchesConnector(object descriptor, string requested)
    {
        return string.Equals(GetStringProperty(descriptor, "Type"), requested, StringComparison.OrdinalIgnoreCase)
            || string.Equals(GetStringProperty(descriptor, "Name"), requested, StringComparison.OrdinalIgnoreCase)
            || string.Equals(GetStringProperty(descriptor, "DisplayName"), requested, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesRole(object descriptor, string requested)
    {
        return string.Equals(GetStringProperty(descriptor, "Direction"), requested, StringComparison.OrdinalIgnoreCase)
            || string.Equals(GetStringProperty(descriptor, "Role"), requested, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetStringProperty(object value, string name)
    {
        var property = value.GetType().GetProperty(name);
        return property?.GetValue(value)?.ToString();
    }
}
