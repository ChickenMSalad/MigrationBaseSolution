using Migration.Admin.Api.Contracts;

namespace Migration.Admin.Api.Endpoints;

public static class QueueProviderPlanEndpointExtensions
{
    public static RouteGroupBuilder MapQueueProviderPlanEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet("/cloud/queue-provider-plan", (
                IConfiguration configuration,
                IWebHostEnvironment environment,
                HttpContext httpContext) =>
            {
                var workspaceId = FirstNonEmpty(
                    httpContext.Request.Headers["X-Workspace-Id"].FirstOrDefault(),
                    configuration["Workspace:WorkspaceId"],
                    "default");

                var descriptor = BuildDescriptor(configuration, environment, workspaceId);
                return Results.Ok(descriptor);
            })
            .WithName("GetQueueProviderPlan")
            .WithTags("Cloud")
            .WithSummary("Gets the safe queue provider plan for cloud-readiness diagnostics.")
            .Produces<QueueProviderPlanDescriptor>(StatusCodes.Status200OK);

        return api;
    }

    private static QueueProviderPlanDescriptor BuildDescriptor(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        string workspaceId)
    {
        var normalizedWorkspaceId = NormalizeSegment(workspaceId);

        var provider = Read(
            configuration,
            "MigrationRunQueue:Provider",
            environment.IsDevelopment() ? "InMemory" : "unknown");

        var logicalQueueName = Read(
            configuration,
            "MigrationRunQueue:QueueName",
            "migration-runs");

        var credentialMode = Read(
            configuration,
            "Cloud:CredentialMode",
            environment.IsDevelopment() ? "userSecrets" : "unknown");

        var serviceBusNamespace = FirstNonEmptyOrNull(
            configuration["MigrationRunQueue:ServiceBusNamespace"],
            configuration["ServiceBus:Namespace"],
            configuration["Cloud:ServiceBusNamespace"]);

        var storageAccountName = FirstNonEmptyOrNull(
            configuration["MigrationRunQueue:StorageAccountName"],
            configuration["AzureQueue:StorageAccountName"],
            configuration["AzureBlob:AccountName"],
            configuration["Cloud:QueueStorageAccountName"]);

        var providerKind = InferProviderKind(provider, serviceBusNamespace, storageAccountName);

        var workspaceQueueName = BuildWorkspaceQueueName(logicalQueueName, normalizedWorkspaceId);

        var warnings = BuildWarnings(
            environment,
            provider,
            providerKind,
            logicalQueueName,
            serviceBusNamespace,
            storageAccountName,
            normalizedWorkspaceId);

        return new QueueProviderPlanDescriptor(
            EnvironmentName: environment.EnvironmentName,
            WorkspaceId: normalizedWorkspaceId,
            QueueProvider: provider,
            ProviderKind: providerKind,
            LogicalQueueName: logicalQueueName,
            WorkspaceQueueName: workspaceQueueName,
            ServiceBusNamespace: serviceBusNamespace,
            StorageAccountName: storageAccountName,
            UsesInMemory: string.Equals(providerKind, QueueProviderKinds.InMemory, StringComparison.OrdinalIgnoreCase),
            UsesAzureStorageQueue: string.Equals(providerKind, QueueProviderKinds.AzureStorageQueue, StringComparison.OrdinalIgnoreCase),
            UsesServiceBus: string.Equals(providerKind, QueueProviderKinds.AzureServiceBusQueue, StringComparison.OrdinalIgnoreCase),
            RequiresManagedIdentity: string.Equals(credentialMode, "managedIdentity", StringComparison.OrdinalIgnoreCase),
            SupportsDeadLettering: string.Equals(providerKind, QueueProviderKinds.AzureServiceBusQueue, StringComparison.OrdinalIgnoreCase),
            SupportsSessions: string.Equals(providerKind, QueueProviderKinds.AzureServiceBusQueue, StringComparison.OrdinalIgnoreCase),
            SupportsScheduledMessages: string.Equals(providerKind, QueueProviderKinds.AzureServiceBusQueue, StringComparison.OrdinalIgnoreCase),
            RecommendedMessageProperties:
            [
                "runId",
                "projectId",
                "workspaceId",
                "tenantId",
                "idempotencyKey",
                "leaseResource",
                "attempt",
                "createdUtc"
            ],
            Warnings: warnings);
    }

    private static string InferProviderKind(
        string provider,
        string? serviceBusNamespace,
        string? storageAccountName)
    {
        if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Memory", StringComparison.OrdinalIgnoreCase))
        {
            return QueueProviderKinds.InMemory;
        }

        if (string.Equals(provider, "AzureQueue", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "AzureStorageQueue", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrWhiteSpace(storageAccountName))
        {
            return QueueProviderKinds.AzureStorageQueue;
        }

        if (string.Equals(provider, "ServiceBus", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "AzureServiceBus", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrWhiteSpace(serviceBusNamespace))
        {
            return QueueProviderKinds.AzureServiceBusQueue;
        }

        return QueueProviderKinds.Unknown;
    }

    private static List<string> BuildWarnings(
        IWebHostEnvironment environment,
        string provider,
        string providerKind,
        string logicalQueueName,
        string? serviceBusNamespace,
        string? storageAccountName,
        string workspaceId)
    {
        var warnings = new List<string>();

        if (!environment.IsDevelopment() &&
            string.Equals(providerKind, QueueProviderKinds.InMemory, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development environments should not use in-memory queues.");
        }

        if (string.Equals(providerKind, QueueProviderKinds.AzureStorageQueue, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(storageAccountName))
        {
            warnings.Add("Azure Storage Queue provider is selected but no storage account name is configured.");
        }

        if (string.Equals(providerKind, QueueProviderKinds.AzureServiceBusQueue, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(serviceBusNamespace))
        {
            warnings.Add("Azure Service Bus provider is selected but no namespace is configured.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(workspaceId, "default", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Non-development queue names should include an explicit workspace id.");
        }

        if (string.IsNullOrWhiteSpace(logicalQueueName))
        {
            warnings.Add("MigrationRunQueue:QueueName is not configured.");
        }

        if (string.Equals(providerKind, QueueProviderKinds.Unknown, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Queue provider '{provider}' is not recognized.");
        }

        return warnings;
    }

    private static string BuildWorkspaceQueueName(
        string logicalQueueName,
        string workspaceId)
    {
        var normalizedLogicalName = NormalizeSegment(logicalQueueName);
        var normalizedWorkspaceId = NormalizeSegment(workspaceId);

        if (string.Equals(normalizedWorkspaceId, "default", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedLogicalName;
        }

        return $"{normalizedLogicalName}-{normalizedWorkspaceId}";
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


