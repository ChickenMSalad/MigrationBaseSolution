using Migration.Admin.Api.OperationalStore;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.Endpoints;

public static class OperationalMirrorDiagnosticsEndpointExtensions
{
    public static IEndpointRouteBuilder MapOperationalMirrorDiagnosticsEndpoints(
        this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(
                "/api/operational/mirror/status",
                (
                    IOptions<OperationalRunMirrorOptions> options,
                    IAdminOperationalRunMirrorService mirrorService,
                    IValidateOptions<OperationalRunMirrorOptions> validator) =>
                {
                    var response = new OperationalMirrorConfigurationStatusResponse
                    {
                        Enabled = options.Value.Enabled,
                        MirrorServiceRegistered = mirrorService is not null,
                        OptionsValidatorRegistered = validator is not null
                    };

                    return Results.Ok(response);
                })
            .WithName("GetOperationalMirrorStatus")
            .WithTags("Operational Store")
            .WithSummary("Returns operational run mirror registration and feature-toggle status.")
            .Produces<OperationalMirrorConfigurationStatusResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        return app;
    }
}
