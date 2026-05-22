using Migration.ControlPlane.Services;

namespace Migration.Admin.Api.Endpoints;

public static class DeleteEndpointExtensions
{
    public static IEndpointRouteBuilder MapControlPlaneDeleteEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api")
            .WithTags("Delete");

        //api.MapDelete("/projects/{projectId}", async (
        //        string projectId,
        //        bool includeRuns,
        //        ControlPlaneDeleteService deleteService,
        //        CancellationToken cancellationToken) =>
        //    {
        //        var result = await deleteService.DeleteProjectAsync(projectId, includeRuns, cancellationToken);
        //        return Results.Ok(result);
        //    })
        //    .WithName("DeleteProject")
        //    .WithSummary("Deletes a file-backed project record. Pass includeRuns=true to also delete related run/progress/state records.");

        api.MapDelete("/runs/{runId}", async (
                string runId,
                ControlPlaneDeleteService deleteService,
                CancellationToken cancellationToken) =>
            {
                var result = await deleteService.DeleteRunAsync(runId, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("DeleteRun")
            .WithSummary("Deletes a file-backed run record and associated local monitoring/state files.");

        //api.MapDelete("/credentials/{credentialId}", async (
        //        string credentialId,
        //        ControlPlaneDeleteService deleteService,
        //        CancellationToken cancellationToken) =>
        //    {
        //        var result = await deleteService.DeleteCredentialAsync(credentialId, cancellationToken);
        //        return Results.Ok(result);
        //    })
        //    .WithName("DeleteCredential")
        //    .WithSummary("Deletes a file-backed credential record from the local control-plane store.");

        //api.MapDelete("/artifacts/{artifactId}", async (
        //        string artifactId,
        //        ControlPlaneDeleteService deleteService,
        //        CancellationToken cancellationToken) =>
        //    {
        //        var result = await deleteService.DeleteArtifactAsync(artifactId, cancellationToken);
        //        return Results.Ok(result);
        //    })
        //    .WithName("DeleteArtifact")
        //    .WithSummary("Deletes an uploaded artifact record/file from the local control-plane store.");

        api.MapDelete("/connectors/{connectorType}", (string connectorType) =>
            Results.Problem(
                title: "Connectors are code-registered and cannot be deleted.",
                detail: $"Connector '{connectorType}' is part of the compiled connector catalog. Disable connector availability through GenericMigrationRuntime filtering or remove it from the catalog/registration code.",
                statusCode: StatusCodes.Status405MethodNotAllowed))
            .WithName("DeleteConnector")
            .WithSummary("Returns 405 because connectors are currently compiled catalog entries, not user-created records.");

        return app;
    }
}
