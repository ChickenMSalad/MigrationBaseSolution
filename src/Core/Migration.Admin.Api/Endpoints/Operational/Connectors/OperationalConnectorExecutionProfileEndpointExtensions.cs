using Microsoft.AspNetCore.Http.HttpResults;

namespace Migration.Admin.Api.Endpoints.Operational.Connectors;

public static class OperationalConnectorExecutionProfileEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalConnectorExecutionProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/operational/connectors/execution-profiles")
            .WithTags("Operational Connector Execution Profiles");

        group.MapGet("/summary", GetSummary)
            .WithName("GetOperationalConnectorExecutionProfileSummary");

        group.MapGet("/catalog", GetCatalog)
            .WithName("GetOperationalConnectorExecutionProfileCatalog");

        group.MapPost("/validate", Validate)
            .WithName("ValidateOperationalConnectorExecutionProfile");

        return app;
    }

    private static Ok<ConnectorExecutionProfileSummaryResponse> GetSummary()
    {
        var response = new ConnectorExecutionProfileSummaryResponse(
            TotalProfiles: 4,
            SourceProfiles: 2,
            TargetProfiles: 2,
            PolicyFamilies: new[]
            {
                "Concurrency",
                "Retry",
                "Backoff",
                "Throttle",
                "CircuitBreaker"
            },
            DefaultProfileId: "balanced-standard");

        return TypedResults.Ok(response);
    }

    private static Ok<IReadOnlyList<ConnectorExecutionProfileCatalogItem>> GetCatalog()
    {
        IReadOnlyList<ConnectorExecutionProfileCatalogItem> profiles = new[]
        {
            new ConnectorExecutionProfileCatalogItem(
                ProfileId: "balanced-standard",
                DisplayName: "Balanced Standard",
                ConnectorScope: "Any",
                MaxConcurrency: 8,
                MaxAttempts: 3,
                RetryDelaySeconds: 30,
                ThrottlePerMinute: 300,
                IsDefault: true),
            new ConnectorExecutionProfileCatalogItem(
                ProfileId: "source-discovery-safe",
                DisplayName: "Source Discovery Safe",
                ConnectorScope: "Source",
                MaxConcurrency: 4,
                MaxAttempts: 4,
                RetryDelaySeconds: 45,
                ThrottlePerMinute: 180,
                IsDefault: false),
            new ConnectorExecutionProfileCatalogItem(
                ProfileId: "target-write-conservative",
                DisplayName: "Target Write Conservative",
                ConnectorScope: "Target",
                MaxConcurrency: 3,
                MaxAttempts: 5,
                RetryDelaySeconds: 60,
                ThrottlePerMinute: 120,
                IsDefault: false),
            new ConnectorExecutionProfileCatalogItem(
                ProfileId: "bulk-transfer-aggressive",
                DisplayName: "Bulk Transfer Aggressive",
                ConnectorScope: "Any",
                MaxConcurrency: 16,
                MaxAttempts: 2,
                RetryDelaySeconds: 15,
                ThrottlePerMinute: 900,
                IsDefault: false)
        };

        return TypedResults.Ok(profiles);
    }

    private static Ok<ConnectorExecutionProfileValidationResponse> Validate(ConnectorExecutionProfileValidationRequest request)
    {
        var findings = new List<string>();

        if (request.MaxConcurrency < 1)
        {
            findings.Add("MaxConcurrency must be at least 1.");
        }

        if (request.MaxConcurrency > 32)
        {
            findings.Add("MaxConcurrency above 32 should be reviewed before production use.");
        }

        if (request.MaxAttempts < 1)
        {
            findings.Add("MaxAttempts must be at least 1.");
        }

        if (request.RetryDelaySeconds < 1)
        {
            findings.Add("RetryDelaySeconds must be at least 1.");
        }

        if (request.ThrottlePerMinute < 1)
        {
            findings.Add("ThrottlePerMinute must be at least 1.");
        }

        var isValid = findings.Count == 0;
        if (isValid)
        {
            findings.Add("Execution profile is structurally valid.");
        }

        return TypedResults.Ok(new ConnectorExecutionProfileValidationResponse(isValid, findings));
    }

    public sealed record ConnectorExecutionProfileSummaryResponse(
        int TotalProfiles,
        int SourceProfiles,
        int TargetProfiles,
        IReadOnlyList<string> PolicyFamilies,
        string DefaultProfileId);

    public sealed record ConnectorExecutionProfileCatalogItem(
        string ProfileId,
        string DisplayName,
        string ConnectorScope,
        int MaxConcurrency,
        int MaxAttempts,
        int RetryDelaySeconds,
        int ThrottlePerMinute,
        bool IsDefault);

    public sealed record ConnectorExecutionProfileValidationRequest(
        string ProfileId,
        string ConnectorScope,
        int MaxConcurrency,
        int MaxAttempts,
        int RetryDelaySeconds,
        int ThrottlePerMinute);

    public sealed record ConnectorExecutionProfileValidationResponse(
        bool IsValid,
        IReadOnlyList<string> Findings);
}


