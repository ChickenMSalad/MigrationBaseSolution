using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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

        api.MapGet("/credentials/{credentialSetId}", async (
            string credentialSetId,
            ICredentialSetStore store,
            CancellationToken ct) =>
        {
            var credentialSet = await store.GetAsync(credentialSetId, ct).ConfigureAwait(false);
            return credentialSet is null
                ? Results.NotFound()
                : Results.Ok(CredentialSetFactory.Sanitize(credentialSet));
        })
        .WithTags("Credentials")
        .WithSummary("Get a saved credential set with secret values masked.");

        api.MapPost("/credentials", async (
            CreateCredentialSetRequest request,
            CredentialSetFactory factory,
            ICredentialSetStore store,
            IConfiguration configuration,
            IHostEnvironment environment,
            CancellationToken ct) =>
        {
            var validation = ValidateCreateRequest(request, configuration, environment);
            if (validation is not null)
            {
                return validation;
            }

            var credentialSet = factory.Create(request);
            await store.SaveAsync(credentialSet, ct).ConfigureAwait(false);
            return Results.Created($"/api/credentials/{credentialSet.CredentialSetId}", CredentialSetFactory.Sanitize(credentialSet));
        })
        .WithTags("Credentials")
        .WithSummary("Create a credential set. In cloud, secret values must be Key Vault or environment references.");

        api.MapPut("/credentials/{credentialSetId}", async (
            string credentialSetId,
            UpdateCredentialSetRequest request,
            CredentialSetFactory factory,
            ICredentialSetStore store,
            IConfiguration configuration,
            IHostEnvironment environment,
            CancellationToken ct) =>
        {
            var existing = await store.GetAsync(credentialSetId, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return Results.NotFound();
            }

            var validation = ValidateUpdateRequest(request, configuration, environment);
            if (validation is not null)
            {
                return validation;
            }

            var updated = factory.Update(existing, request);
            await store.SaveAsync(updated, ct).ConfigureAwait(false);
            return Results.Ok(CredentialSetFactory.Sanitize(updated));
        })
        .WithTags("Credentials")
        .WithSummary("Update a credential set. In cloud, secret values must be Key Vault or environment references.");

        api.MapDelete("/credentials/{credentialSetId}", async (
            string credentialSetId,
            ICredentialSetStore store,
            CancellationToken ct) =>
        {
            return await store.DeleteAsync(credentialSetId, ct).ConfigureAwait(false)
                ? Results.NoContent()
                : Results.NotFound();
        })
        .WithTags("Credentials")
        .WithSummary("Delete a credential set.");

        api.MapPost("/credentials/{credentialSetId}/test", async (
            string credentialSetId,
            ICredentialSetStore store,
            CredentialTestService tester,
            CancellationToken ct) =>
        {
            var credentialSet = await store.GetAsync(credentialSetId, ct).ConfigureAwait(false);
            if (credentialSet is null)
            {
                return Results.NotFound();
            }

            var result = await tester.TestAsync(credentialSet, ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .WithTags("Credentials")
        .WithSummary("Run structural validation for a credential set.");

        api.MapGet("/connectors/{connectorType}/schema", (
            string connectorType,
            string? role,
            IConnectorCatalog catalog) =>
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

            return matches.Length == 0
                ? Results.NotFound(new { error = $"Connector '{connectorType}' was not found." })
                : Results.Ok(matches[0]);
        })
        .WithTags("Connectors")
        .WithSummary("Get connector descriptor/schema metadata for dynamic setup forms.");

        return api;
    }

    private static IResult? ValidateCreateRequest(
        CreateCredentialSetRequest request,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName)
            || string.IsNullOrWhiteSpace(request.ConnectorType)
            || string.IsNullOrWhiteSpace(request.ConnectorRole))
        {
            return Results.BadRequest(new { error = "DisplayName, ConnectorType, and ConnectorRole are required." });
        }

        if (request.Values is null || request.Values.Count == 0)
        {
            return Results.BadRequest(new { error = "At least one credential value is required." });
        }

        return ValidateSecretReferences(
            request.Values,
            request.SecretKeys,
            configuration,
            environment,
            allowMaskedValues: false);
    }

    private static IResult? ValidateUpdateRequest(
        UpdateCredentialSetRequest request,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (request.Values is null)
        {
            return null;
        }

        return ValidateSecretReferences(
            request.Values,
            request.SecretKeys,
            configuration,
            environment,
            allowMaskedValues: true);
    }

    private static IResult? ValidateSecretReferences(
        Dictionary<string, string> values,
        IReadOnlyCollection<string>? explicitSecretKeys,
        IConfiguration configuration,
        IHostEnvironment environment,
        bool allowMaskedValues)
    {
        if (AllowsPlainTextSecrets(configuration, environment))
        {
            return null;
        }

        var secretKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (explicitSecretKeys is not null)
        {
            foreach (var key in explicitSecretKeys.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                secretKeys.Add(key.Trim());
            }
        }

        foreach (var key in values.Keys.Where(LooksSecret))
        {
            secretKeys.Add(key.Trim());
        }

        var invalid = new List<string>();
        foreach (var key in secretKeys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            if (!values.TryGetValue(key, out var value))
            {
                continue;
            }

            if (allowMaskedValues && string.Equals(value, "********", StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsSecretReference(value))
            {
                invalid.Add(key);
            }
        }

        return invalid.Count == 0
            ? null
            : Results.BadRequest(new
            {
                error = "Secret credential values must be references, not raw secret material.",
                invalidSecretKeys = invalid,
                acceptedReferenceFormats = new[]
                {
                    "kv://secret-name",
                    "kv://secret-name/secret-version",
                    "env://VARIABLE_NAME",
                    "https://<vault>.vault.azure.net/secrets/<secret-name>"
                }
            });
    }

    private static bool AllowsPlainTextSecrets(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return true;
        }

        return bool.TryParse(configuration["Credentials:AllowPlainTextSecrets"], out var allow) && allow;
    }

    private static bool IsSecretReference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.StartsWith("kv://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("keyvault://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("env://", StringComparison.OrdinalIgnoreCase)
            || (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && uri.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath.Contains("/secrets/", StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksSecret(string key)
    {
        return key.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || key.Contains("password", StringComparison.OrdinalIgnoreCase)
            || key.Contains("token", StringComparison.OrdinalIgnoreCase)
            || key.Contains("key", StringComparison.OrdinalIgnoreCase)
            || key.Contains("connectionstring", StringComparison.OrdinalIgnoreCase)
            || key.Contains("connection_string", StringComparison.OrdinalIgnoreCase);
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
