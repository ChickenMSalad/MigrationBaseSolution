using Migration.Application.Taxonomy;

namespace Migration.Admin.Api.Endpoints;

public static class TaxonomyEndpoints
{
    public static IEndpointRouteBuilder MapTaxonomyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/taxonomy")
            .WithTags("Taxonomy");

        group.MapGet("/targets", () =>
        {
            var targets = new[]
            {
                new TaxonomyTargetDto("Bynder", "Bynder"),
                new TaxonomyTargetDto("Cloudinary", "Cloudinary"),
                new TaxonomyTargetDto("Aprimo", "Aprimo")
            };

            return Results.Ok(targets);
        });

        group.MapPost("/export", async (
            TaxonomyExportRequest request,
            ITaxonomyExportService taxonomyExportService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.TargetType))
            {
                return Results.BadRequest("TargetType is required.");
            }

            var result = await taxonomyExportService.ExportAsync(request, cancellationToken);

            return Results.File(
                result.Content,
                result.ContentType,
                result.FileName);
        });

        return app;
    }

    private sealed record TaxonomyTargetDto(string Value, string Label);
}
