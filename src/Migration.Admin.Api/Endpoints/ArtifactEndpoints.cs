using Migration.ControlPlane.Artifacts;

namespace Migration.Admin.Api.Endpoints;

public static class ArtifactEndpoints
{
    public static IEndpointRouteBuilder MapArtifactEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/artifacts")
            .WithTags("Artifacts");

        group.MapGet("/", async (
                IArtifactStore artifacts,
                string? kind,
                string? projectId,
                CancellationToken cancellationToken) =>
            {
                var parsedKind = TryParseKind(kind);
                var results = await artifacts.ListAsync(parsedKind, projectId, cancellationToken).ConfigureAwait(false);
                return Results.Ok(results);
            })
            .WithSummary("List uploaded control-plane artifacts.");

        group.MapPost("/", async (
                HttpRequest request,
                IArtifactStore artifacts,
                CancellationToken cancellationToken) =>
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new { error = "Expected multipart/form-data." });
                }

                var form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
                var file = form.Files.GetFile("file");
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest(new { error = "Upload a non-empty file using form field 'file'." });
                }

                var kind = TryParseKind(form["kind"].FirstOrDefault()) ?? InferKind(file.FileName);
                var projectId = form["projectId"].FirstOrDefault();
                var description = form["description"].FirstOrDefault();

                await using var stream = file.OpenReadStream();
                var record = await artifacts.SaveAsync(
                    stream,
                    file.FileName,
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    kind,
                    projectId,
                    description,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return Results.Created($"/api/artifacts/{record.ArtifactId}", record);
            })
            .DisableAntiforgery()
            .WithSummary("Upload a manifest, mapping, report, or other control-plane artifact.");

        group.MapPost("/manifests", async (
                HttpRequest request,
                IArtifactStore artifacts,
                CancellationToken cancellationToken) =>
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new { error = "Expected multipart/form-data." });
                }

                var form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
                var file = form.Files.GetFile("file");
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest(new { error = "Upload a non-empty manifest using form field 'file'." });
                }

                await using var stream = file.OpenReadStream();
                var record = await artifacts.SaveAsync(
                    stream,
                    file.FileName,
                    string.IsNullOrWhiteSpace(file.ContentType) ? "text/csv" : file.ContentType,
                    ArtifactKind.Manifest,
                    form["projectId"].FirstOrDefault(),
                    form["description"].FirstOrDefault(),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return Results.Created($"/api/artifacts/{record.ArtifactId}", record);
            })
            .DisableAntiforgery()
            .WithSummary("Upload a manifest artifact.");

        group.MapPost("/mappings", async (
                HttpRequest request,
                IArtifactStore artifacts,
                CancellationToken cancellationToken) =>
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new { error = "Expected multipart/form-data." });
                }

                var form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
                var file = form.Files.GetFile("file");
                if (file is null || file.Length == 0)
                {
                    return Results.BadRequest(new { error = "Upload a non-empty mapping using form field 'file'." });
                }

                await using var stream = file.OpenReadStream();
                var record = await artifacts.SaveAsync(
                    stream,
                    file.FileName,
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/json" : file.ContentType,
                    ArtifactKind.Mapping,
                    form["projectId"].FirstOrDefault(),
                    form["description"].FirstOrDefault(),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return Results.Created($"/api/artifacts/{record.ArtifactId}", record);
            })
            .DisableAntiforgery()
            .WithSummary("Upload a mapping profile artifact.");

        group.MapGet("/{artifactId}", async (
                string artifactId,
                IArtifactStore artifacts,
                CancellationToken cancellationToken) =>
            {
                var record = await artifacts.GetAsync(artifactId, cancellationToken).ConfigureAwait(false);
                return record is null ? Results.NotFound() : Results.Ok(record);
            })
            .WithSummary("Get artifact metadata.");

        group.MapGet("/{artifactId}/download", async (
                string artifactId,
                IArtifactStore artifacts,
                CancellationToken cancellationToken) =>
            {
                var record = await artifacts.GetAsync(artifactId, cancellationToken).ConfigureAwait(false);
                if (record is null)
                {
                    return Results.NotFound();
                }

                var stream = await artifacts.OpenReadAsync(artifactId, cancellationToken).ConfigureAwait(false);
                return Results.File(stream, record.ContentType, record.FileName);
            })
            .WithSummary("Download an artifact file.");

        group.MapGet("/{artifactId}/manifest-preview", async (
                string artifactId,
                int? take,
                IArtifactStore artifacts,
                CancellationToken cancellationToken) =>
            {
                var preview = await artifacts.PreviewManifestAsync(artifactId, take ?? 10, cancellationToken).ConfigureAwait(false);
                return Results.Ok(preview);
            })
            .WithSummary("Preview CSV manifest columns and sample rows.");

        group.MapDelete("/{artifactId}", async (
                string artifactId,
                IArtifactStore artifacts,
                CancellationToken cancellationToken) =>
            {
                var deleted = await artifacts.DeleteAsync(artifactId, cancellationToken).ConfigureAwait(false);
                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .WithSummary("Delete an artifact.");

        return app;
    }

    private static ArtifactKind? TryParseKind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<ArtifactKind>(value, ignoreCase: true, out var kind)
            ? kind
            : null;
    }

    private static ArtifactKind InferKind(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) || extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return ArtifactKind.Manifest;
        }

        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            return ArtifactKind.Mapping;
        }

        return ArtifactKind.Other;
    }
}
