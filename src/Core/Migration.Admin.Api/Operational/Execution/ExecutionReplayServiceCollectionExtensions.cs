using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.Admin.Api.Operational.Execution;

public static class ExecutionReplayServiceCollectionExtensions
{
    public static IServiceCollection AddExecutionReplayServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ExecutionReplayAdmissionOptions>(
            configuration.GetSection(ExecutionReplayAdmissionOptions.SectionName));

        services.Configure<ExecutionReplayAdmissionBackgroundOptions>(
            configuration.GetSection(ExecutionReplayAdmissionBackgroundOptions.SectionName));

        services.AddScoped<IExecutionDiagnosticExportService, SqlExecutionDiagnosticExportService>();
        services.AddScoped<IExecutionReplayAnalysisService, SqlExecutionReplayAnalysisService>();
        services.AddScoped<IExecutionReplayPreparationService, SqlExecutionReplayPreparationService>();
        services.AddScoped<IExecutionReplayMaterializationService, SqlExecutionReplayMaterializationService>();
        services.AddScoped<IExecutionReplayLineageService, SqlExecutionReplayLineageService>();
        services.AddScoped<IExecutionReplayApprovalService, SqlExecutionReplayApprovalService>();
        services.AddScoped<IExecutionReplayPolicyService, SqlExecutionReplayPolicyService>();
        services.AddScoped<IExecutionReplayPolicyOverrideService, SqlExecutionReplayPolicyOverrideService>();
        services.AddScoped<IExecutionReplayAdmissionManualService, SqlExecutionReplayAdmissionManualService>();
        services.AddScoped<IExecutionReplayAdmissionService, SqlExecutionReplayAdmissionService>();

        services.AddHostedService<ExecutionReplayAdmissionBackgroundService>();

        return services;
    }
}

