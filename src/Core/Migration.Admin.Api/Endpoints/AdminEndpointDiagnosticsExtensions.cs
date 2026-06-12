namespace Migration.Admin.Api.Endpoints;

public static class AdminEndpointDiagnosticsExtensions
{
    public static IEndpointRouteBuilder MapAdminEndpointDiagnostics(
        this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(
                "/api/system/endpoints",
                (IEnumerable<EndpointDataSource> endpointDataSources) =>
                {
                    var endpoints = endpointDataSources
                        .SelectMany(source => source.Endpoints)
                        .OfType<RouteEndpoint>()
                        .Select(endpoint => new AdminEndpointDiagnosticItem
                        {
                            RoutePattern = endpoint.RoutePattern.RawText ?? string.Empty,
                            DisplayName = endpoint.DisplayName,
                            Methods = endpoint.Metadata
                                .OfType<HttpMethodMetadata>()
                                .SelectMany(metadata => metadata.HttpMethods)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(method => method)
                                .ToArray()
                        })
                        .Where(endpoint => !string.IsNullOrWhiteSpace(endpoint.RoutePattern))
                        .OrderBy(endpoint => endpoint.RoutePattern)
                        .ThenBy(endpoint => endpoint.DisplayName)
                        .ToArray();

                    return Results.Ok(endpoints);
                })
            .WithName("GetAdminEndpointDiagnostics")
            .WithTags("System")
            .WithSummary("Returns the mapped Admin API route endpoints for local diagnostics.")
            .Produces<IReadOnlyList<AdminEndpointDiagnosticItem>>(StatusCodes.Status200OK)
            .WithOpenApi();

        return app;
    }
}


