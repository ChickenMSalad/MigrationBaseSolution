using System.Text;
using Migration.ControlPlane.Models;
using Migration.ControlPlane.Services;
using Migration.Orchestration.Abstractions;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

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
            var validation = ValidateCreateRequest(request);
            if (validation is not null)
            {
                return validation;
            }

            var prepared = await PrepareCreateRequestAsync(request, configuration, environment, ct).ConfigureAwait(false);
            if (prepared.Error is not null)
            {
                return prepared.Error;
            }

            var credentialSet = factory.Create(prepared.Request!);
            await store.SaveAsync(credentialSet, ct).ConfigureAwait(false);
            return Results.Created($"/api/credentials/{credentialSet.CredentialSetId}", CredentialSetFactory.Sanitize(credentialSet));
        })
        .WithTags("Credentials")
        .WithSummary("Create a credential set. In cloud, raw secret values are written to Key Vault and stored as references.");

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

            var prepared = await PrepareUpdateRequestAsync(existing, request, configuration, environment, ct).ConfigureAwait(false);
            if (prepared.Error is not null)
            {
                return prepared.Error;
            }

            var updated = factory.Update(existing, prepared.Request!);
            await store.SaveAsync(updated, ct).ConfigureAwait(false);
            return Results.Ok(CredentialSetFactory.Sanitize(updated));
        })
        .WithTags("Credentials")
        .WithSummary("Update a credential set. In cloud, raw secret values are written to Key Vault and stored as references.");

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

    private static IResult? ValidateCreateRequest(CreateCredentialSetRequest request)
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

        return null;
    }

    private static async Task<CreatePreparationResult> PrepareCreateRequestAsync(
        CreateCredentialSetRequest request,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var values = CopyValues(request.Values);
        var secretKeys = BuildSecretKeySet(request.SecretKeys, values);

        var prepared = await PrepareSecretValuesAsync(
            request.DisplayName,
            request.ConnectorType,
            values,
            secretKeys,
            configuration,
            environment,
            allowMaskedValues: false,
            existingValues: null,
            cancellationToken).ConfigureAwait(false);

        if (prepared.Error is not null)
        {
            return new CreatePreparationResult(null, prepared.Error);
        }

        return new CreatePreparationResult(
            new CreateCredentialSetRequest(
                request.DisplayName,
                request.ConnectorType,
                request.ConnectorRole,
                prepared.Values,
                secretKeys.ToArray()),
            null);
    }

    private static async Task<UpdatePreparationResult> PrepareUpdateRequestAsync(
        CredentialSetRecord existing,
        UpdateCredentialSetRequest request,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (request.Values is null)
        {
            return new UpdatePreparationResult(request, null);
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? existing.DisplayName
            : request.DisplayName;
        var connectorType = string.IsNullOrWhiteSpace(request.ConnectorType)
            ? existing.ConnectorType
            : request.ConnectorType;

        var values = CopyValues(request.Values);
        var secretKeys = BuildSecretKeySet(request.SecretKeys ?? existing.SecretKeys, values);

        var prepared = await PrepareSecretValuesAsync(
            displayName,
            connectorType,
            values,
            secretKeys,
            configuration,
            environment,
            allowMaskedValues: true,
            existingValues: existing.Values,
            cancellationToken).ConfigureAwait(false);

        if (prepared.Error is not null)
        {
            return new UpdatePreparationResult(null, prepared.Error);
        }

        return new UpdatePreparationResult(
            request with
            {
                Values = prepared.Values,
                SecretKeys = secretKeys.ToArray()
            },
            null);
    }

    private static async Task<SecretPreparationResult> PrepareSecretValuesAsync(
        string displayName,
        string connectorType,
        Dictionary<string, string?> values,
        HashSet<string> secretKeys,
        IConfiguration configuration,
        IHostEnvironment environment,
        bool allowMaskedValues,
        IReadOnlyDictionary<string, string?>? existingValues,
        CancellationToken cancellationToken)
    {
        if (AllowsPlainTextSecrets(configuration, environment))
        {
            return new SecretPreparationResult(values, null);
        }

        var keyVaultUri = GetConfiguredKeyVaultUri(configuration);
        var needsVault = new List<string>();

        foreach (var key in secretKeys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            if (!values.TryGetValue(key, out var value))
            {
                continue;
            }

            if (allowMaskedValues && string.Equals(value, "********", StringComparison.Ordinal))
            {
                if (existingValues is not null && existingValues.TryGetValue(key, out var existingValue))
                {
                    values[key] = existingValue;
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(value) || IsSecretReference(value))
            {
                continue;
            }

            needsVault.Add(key);
        }

        if (needsVault.Count == 0)
        {
            return new SecretPreparationResult(values, null);
        }

        if (keyVaultUri is null)
        {
            return new SecretPreparationResult(values, Results.BadRequest(new
            {
                error = "Secret credential values were entered, but no Key Vault URI is configured for cloud-safe storage.",
                secretKeys = needsVault,
                requiredConfiguration = new[]
                {
                    "CredentialVault:KeyVaultUri",
                    "AzureKeyVault:VaultUri",
                    "KeyVault:VaultUri"
                }
            }));
        }

        foreach (var key in needsVault)
        {
            var secretValue = values[key];
            if (string.IsNullOrWhiteSpace(secretValue))
            {
                continue;
            }

            var secretName = BuildSecretName(displayName, connectorType, key);
            await WriteKeyVaultSecretAsync(keyVaultUri, secretName, secretValue, configuration, cancellationToken)
                .ConfigureAwait(false);
            values[key] = $"kv://{secretName}";
        }

        return new SecretPreparationResult(values, null);
    }

    private static Dictionary<string, string?> CopyValues(Dictionary<string, string?> values)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in values)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                result[pair.Key.Trim()] = pair.Value;
            }
        }

        return result;
    }

    private static HashSet<string> BuildSecretKeySet(
        IReadOnlyCollection<string>? explicitSecretKeys,
        IReadOnlyDictionary<string, string?> values)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (explicitSecretKeys is not null)
        {
            foreach (var key in explicitSecretKeys.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                result.Add(key.Trim());
            }
        }

        foreach (var key in values.Keys.Where(LooksSecret))
        {
            result.Add(key.Trim());
        }

        return result;
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

    private static Uri? GetConfiguredKeyVaultUri(IConfiguration configuration)
    {
        var value = configuration["CredentialVault:KeyVaultUri"]
            ?? configuration["AzureKeyVault:VaultUri"]
            ?? configuration["KeyVault:VaultUri"];

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value.TrimEnd('/'), UriKind.Absolute, out var uri)
            ? uri
            : throw new InvalidOperationException($"Configured Key Vault URI '{value}' is not a valid absolute URI.");
    }

    private static string BuildSecretName(string displayName, string connectorType, string key)
    {
        var raw = $"migration-{connectorType}-{displayName}-{key}";
        var builder = new StringBuilder(raw.Length);
        var previousDash = false;

        foreach (var ch in raw.ToLowerInvariant())
        {
            var next = char.IsLetterOrDigit(ch) ? ch : '-';
            if (next == '-')
            {
                if (previousDash)
                {
                    continue;
                }

                previousDash = true;
            }
            else
            {
                previousDash = false;
            }

            builder.Append(next);
        }

        var result = builder.ToString().Trim('-');
        if (result.Length == 0)
        {
            result = $"migration-credential-{Guid.NewGuid():N}";
        }

        if (result.Length > 100)
        {
            result = result[..100].Trim('-');
        }

        return result;
    }

    private static async Task WriteKeyVaultSecretAsync(
    Uri vaultUri,
    string secretName,
    string secretValue,
    IConfiguration configuration,
    CancellationToken cancellationToken)
    {
        var client = new SecretClient(vaultUri, new DefaultAzureCredential());

        await client.SetSecretAsync(
            new KeyVaultSecret(secretName, secretValue),
            cancellationToken);
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

    private sealed record CreatePreparationResult(CreateCredentialSetRequest? Request, IResult? Error);

    private sealed record UpdatePreparationResult(UpdateCredentialSetRequest? Request, IResult? Error);

    private sealed record SecretPreparationResult(Dictionary<string, string?> Values, IResult? Error);
}


