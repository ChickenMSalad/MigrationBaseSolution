using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Migration.Admin.Api.Endpoints.Operational.SqlBackbone;

public static class SqlOperationalBackboneEndpointExtensions
{
    public static IEndpointRouteBuilder MapSqlOperationalBackboneEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/operational/sql-backbone")
            .WithTags("Operational SQL Backbone");

        group.MapGet("/readiness", () => Results.Ok(new SqlOperationalBackboneReadinessResponse(
            Status: "Configured",
            Store: "SqlServer",
            Area: "OperationalBackbone",
            Notes: "SQL operational backbone endpoint facade is registered. Runtime connection validation is intentionally deferred to the SQL infrastructure layer.")))
            .WithName("GetSqlOperationalBackboneReadiness");

        group.MapGet("/capabilities", () => Results.Ok(new SqlOperationalBackboneCapabilitiesResponse(
            SupportsProjects: true,
            SupportsRuns: true,
            SupportsManifestRows: true,
            SupportsWorkItems: true,
            SupportsFailures: true,
            SupportsCheckpoints: true,
            SupportsIdentifierMappings: true)))
            .WithName("GetSqlOperationalBackboneCapabilities");

        return endpoints;
    }
}

internal sealed record SqlOperationalBackboneReadinessResponse(
    string Status,
    string Store,
    string Area,
    string Notes);

internal sealed record SqlOperationalBackboneCapabilitiesResponse(
    bool SupportsProjects,
    bool SupportsRuns,
    bool SupportsManifestRows,
    bool SupportsWorkItems,
    bool SupportsFailures,
    bool SupportsCheckpoints,
    bool SupportsIdentifierMappings);


