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

        app.MapGet(
                "/api/operational/mirror/readiness",
                (IOperationalMirrorReadinessEvaluator evaluator) =>
                {
                    var response = evaluator.Evaluate();

                    return Results.Ok(response);
                })
            .WithName("GetOperationalMirrorReadiness")
            .WithTags("Operational Store")
            .WithSummary("Returns readiness status for operational run mirror activation.")
            .Produces<OperationalMirrorReadinessStatus>(StatusCodes.Status200OK)
            .WithOpenApi();

        app.MapGet(
                "/api/operational/mirror/enablement-guard",
                async (
                    IOperationalMirrorEnablementGuard guard,
                    CancellationToken cancellationToken) =>
                {
                    var response = await guard.EvaluateAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalMirrorEnablementGuard")
            .WithTags("Operational Store")
            .WithSummary("Returns whether operational mirror writes are currently allowed.")
            .Produces<OperationalMirrorEnablementGuardResult>(StatusCodes.Status200OK)
            .WithOpenApi();

        app.MapGet(
                "/api/operational/mirror/write-verification",
                async (
                    IOperationalMirrorWriteVerificationService verificationService,
                    CancellationToken cancellationToken) =>
                {
                    var response = await verificationService.VerifyAsync(
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithName("GetOperationalMirrorWriteVerification")
            .WithTags("Operational Store")
            .WithSummary("Verifies whether operational mirror writes exist in SQL.")
            .Produces<OperationalMirrorWriteVerificationResult>(StatusCodes.Status200OK)
            .WithOpenApi();

        return app;
    }
}
