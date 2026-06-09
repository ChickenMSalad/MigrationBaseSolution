using Microsoft.AspNetCore.Http.HttpResults;

namespace Migration.Admin.Api.Endpoints.Operational.Connectors;

public static class OperationalConnectorConfigurationEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalConnectorConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/operational/connectors")
            .WithTags("Operational Connectors");

        group.MapGet("/configuration/summary", GetSummary)
            .WithName("GetOperationalConnectorConfigurationSummary")
            .WithSummary("Returns operator-facing connector configuration readiness.");

        group.MapGet("/configuration/catalog", GetCatalog)
            .WithName("GetOperationalConnectorConfigurationCatalog")
            .WithSummary("Returns the connector configuration catalog used by the operator UI.");

        group.MapPost("/configuration/validate", Validate)
            .WithName("ValidateOperationalConnectorConfiguration")
            .WithSummary("Validates connector configuration input before future persistence.");

        return app;
    }

    private static Ok<ConnectorConfigurationSummaryResponse> GetSummary()
    {
        var response = new ConnectorConfigurationSummaryResponse(
            RegisteredConnectors: 0,
            ReadyConnectors: 0,
            SourceConnectors: 0,
            TargetConnectors: 0,
            AttentionRequired: 0,
            LastUpdatedUtc: DateTimeOffset.UtcNow,
            Notes:
            [
                "P4.18 introduces the operator-facing connector configuration workspace.",
                "Persistence is intentionally deferred to the SQL credential/connector registry hardening set.",
                "No connector secrets are returned by these endpoints."
            ]);

        return TypedResults.Ok(response);
    }

    private static Ok<IReadOnlyList<ConnectorConfigurationCatalogItem>> GetCatalog()
    {
        ConnectorConfigurationCatalogItem[] catalog =
        [
            new("azure-blob", "Azure Blob Storage", "SourceTarget", true, ["ConnectionString", "ContainerName"]),
            new("s3", "Amazon S3", "Source", true, ["BucketName", "Region", "CredentialReference"]),
            new("sharepoint", "SharePoint", "Source", true, ["SiteUrl", "DriveId", "CredentialReference"]),
            new("aem", "Adobe Experience Manager", "Source", false, ["BaseUrl", "CredentialReference"]),
            new("sitecore-content-hub", "Sitecore Content Hub", "Source", false, ["BaseUrl", "CredentialReference"]),
            new("bynder", "Bynder", "Target", true, ["BaseUrl", "CredentialReference"]),
            new("aprimo", "Aprimo", "Target", false, ["BaseUrl", "CredentialReference"]),
            new("cloudinary", "Cloudinary", "Target", true, ["CloudName", "CredentialReference"])
        ];

        return TypedResults.Ok<IReadOnlyList<ConnectorConfigurationCatalogItem>>(catalog);
    }

    private static Results<Ok<ConnectorConfigurationValidationResponse>, BadRequest<ConnectorConfigurationValidationResponse>> Validate(
        ConnectorConfigurationValidationRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ConnectorKey))
        {
            errors.Add("ConnectorKey is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            errors.Add("DisplayName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Direction))
        {
            errors.Add("Direction is required.");
        }

        if (request.Settings is null || request.Settings.Count == 0)
        {
            errors.Add("At least one setting value is required.");
        }

        var response = new ConnectorConfigurationValidationResponse(
            IsValid: errors.Count == 0,
            Errors: errors,
            ValidatedAtUtc: DateTimeOffset.UtcNow);

        return errors.Count == 0
            ? TypedResults.Ok(response)
            : TypedResults.BadRequest(response);
    }

    private sealed record ConnectorConfigurationSummaryResponse(
        int RegisteredConnectors,
        int ReadyConnectors,
        int SourceConnectors,
        int TargetConnectors,
        int AttentionRequired,
        DateTimeOffset LastUpdatedUtc,
        IReadOnlyList<string> Notes);

    private sealed record ConnectorConfigurationCatalogItem(
        string ConnectorKey,
        string DisplayName,
        string Direction,
        bool RecommendedForFirstProductionLane,
        IReadOnlyList<string> RequiredSettings);

    private sealed record ConnectorConfigurationValidationRequest(
        string ConnectorKey,
        string DisplayName,
        string Direction,
        IReadOnlyDictionary<string, string?> Settings);

    private sealed record ConnectorConfigurationValidationResponse(
        bool IsValid,
        IReadOnlyList<string> Errors,
        DateTimeOffset ValidatedAtUtc);
}


