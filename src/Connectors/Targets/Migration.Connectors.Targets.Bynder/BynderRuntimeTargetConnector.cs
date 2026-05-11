using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Domain.Models;

namespace Migration.Connectors.Targets.Bynder;

/// <summary>
/// Runtime Bynder target connector that hydrates Bynder options from the queued job settings.
/// This lets the worker register the Bynder connector without requiring a global Bynder:Client
/// appsettings section at process startup.
/// </summary>
public sealed class BynderRuntimeTargetConnector : IAssetTargetConnector
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILoggerFactory _loggerFactory;

    public BynderRuntimeTargetConnector(
        IMemoryCache memoryCache,
        ILoggerFactory loggerFactory)
    {
        _memoryCache = memoryCache;
        _loggerFactory = loggerFactory;
    }

    public string Type => "Bynder";

    public Task<MigrationResult> UpsertAsync(
        MigrationJobDefinition job,
        AssetWorkItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(item);

        var options = ResolveRuntimeOptions(job);

        var inner = new BynderTargetConnector(
            Options.Create(options),
            _memoryCache,
            _loggerFactory.CreateLogger<BynderTargetConnector>());

        return inner.UpsertAsync(job, item, cancellationToken);
    }

    private static BynderOptions ResolveRuntimeOptions(MigrationJobDefinition job)
    {
        var baseUrl = GetSetting(job,
            "BynderBaseUrl",
            "TargetBaseUrl",
            "TargetCredential_BaseUrl");

        var clientId = GetSetting(job,
            "BynderClientId",
            "TargetClientId",
            "TargetCredential_ClientId");

        var clientSecret = GetSetting(job,
            "BynderClientSecret",
            "TargetClientSecret",
            "TargetCredential_ClientSecret");

        var scopes = GetSetting(job,
            "BynderScopes",
            "TargetScopes",
            "TargetCredential_Scopes");

        var brandStoreId = GetSetting(job,
            "BynderBrandStoreId",
            "TargetBrandStoreId",
            "TargetCredential_BrandStoreId");

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(baseUrl)) missing.Add("BaseUrl");
        if (string.IsNullOrWhiteSpace(clientId)) missing.Add("ClientId");
        if (string.IsNullOrWhiteSpace(clientSecret)) missing.Add("ClientSecret");
        if (string.IsNullOrWhiteSpace(scopes)) missing.Add("Scopes");
        if (string.IsNullOrWhiteSpace(brandStoreId)) missing.Add("BrandStoreId");

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Bynder target connector is missing required credential value(s): {string.Join(", ", missing)}. " +
                "Expected job settings BynderBaseUrl/BynderClientId/BynderClientSecret/BynderScopes/BynderBrandStoreId or TargetCredential_* equivalents.");
        }

        return new BynderOptions
        {
            Client = new global::Bynder.Sdk.Settings.Configuration
            {
                BaseUrl = new Uri(baseUrl!, UriKind.Absolute),
                ClientId = clientId!,
                ClientSecret = clientSecret!,
                Scopes = scopes!
            },
            BrandStoreId = brandStoreId!
        };
    }

    private static string? GetSetting(MigrationJobDefinition job, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (job.Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
