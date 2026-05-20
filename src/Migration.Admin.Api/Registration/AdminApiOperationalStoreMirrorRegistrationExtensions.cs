using Migration.Admin.Api.OperationalStore;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.Registration;

public static class AdminApiOperationalStoreMirrorRegistrationExtensions
{
    public static IServiceCollection AddMigrationAdminApiOperationalRunMirror(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<OperationalRunMirrorOptions>(
            configuration.GetSection(OperationalRunMirrorOptions.SectionName));

        services.Configure<OperationalLeaseExpirationOptions>(
            configuration.GetSection(OperationalLeaseExpirationOptions.SectionName));

        services.Configure<OperationalRunAutoFinalizationOptions>(
            configuration.GetSection(OperationalRunAutoFinalizationOptions.SectionName));

        services.Configure<OperationalDispatcherOptions>(
            configuration.GetSection(OperationalDispatcherOptions.SectionName));

        services.Configure<DispatcherExecutionHistoryRetentionOptions>(
            configuration.GetSection(DispatcherExecutionHistoryRetentionOptions.SectionName));

        services.Configure<OperationalRetentionOptions>(
            configuration.GetSection(OperationalRetentionOptions.SectionName));

        services.AddSingleton<IValidateOptions<OperationalRunMirrorOptions>, OperationalRunMirrorOptionsValidator>();
        services.AddSingleton<OperationalMirrorInvocationState>();

        services.AddScoped<IAdminOperationalRunMirrorService, AdminOperationalRunMirrorService>();
        services.AddScoped<IOperationalMirrorReadinessEvaluator, OperationalMirrorReadinessEvaluator>();
        services.AddScoped<IOperationalSqlSchemaSmokeTestService, OperationalSqlSchemaSmokeTestService>();
        services.AddScoped<IOperationalMirrorEnablementGuard, OperationalMirrorEnablementGuard>();
        services.AddScoped<IOperationalMirrorWriteVerificationService, OperationalMirrorWriteVerificationService>();
        services.AddScoped<IOperationalMirrorReadService, OperationalMirrorReadService>();
        services.AddScoped<IOperationalRunStatusProjectionService, OperationalRunStatusProjectionService>();
        services.AddScoped<IOperationalWorkItemLeaseService, OperationalWorkItemLeaseService>();
        services.AddScoped<IOperationalWorkItemRecoveryService, OperationalWorkItemRecoveryService>();
        services.AddScoped<IOperationalLeaseExpirationService, OperationalLeaseExpirationService>();
        services.AddScoped<IOperationalMetricsService, OperationalMetricsService>();
        services.AddScoped<IOperationalRunControlService, OperationalRunControlService>();
        services.AddScoped<IOperationalRunStatusReconciliationService, OperationalRunStatusReconciliationService>();
        services.AddScoped<IOperationalRunCompletionFinalizationService, OperationalRunCompletionFinalizationService>();
        services.AddScoped<IOperationalRunFailureFinalizationService, OperationalRunFailureFinalizationService>();
        services.AddScoped<IOperationalRunDashboardSummaryService, OperationalRunDashboardSummaryService>();
        services.AddScoped<IOperationalRunTimelineService, OperationalRunTimelineService>();
        services.AddScoped<IOperationalRunTimelineQueryService, OperationalRunTimelineQueryService>();
        services.AddScoped<IOperationalRunTimelineMetricsService, OperationalRunTimelineMetricsService>();
        services.AddScoped<IOperationalRunTimelineDashboardService, OperationalRunTimelineDashboardService>();
        services.AddScoped<IOperationalRunTimelineSearchService, OperationalRunTimelineSearchService>();
        services.AddScoped<IOperationalRunTimelineCatalogService, OperationalRunTimelineCatalogService>();
        services.AddScoped<IOperationalRunTimelineGlobalCatalogService, OperationalRunTimelineGlobalCatalogService>();
        services.AddScoped<IOperationalGlobalActivityFeedService, OperationalGlobalActivityFeedService>();
        services.AddScoped<IOperationalGlobalActivityQueryService, OperationalGlobalActivityQueryService>();
        services.AddScoped<IOperationalGlobalActivityMetricsService, OperationalGlobalActivityMetricsService>();
        services.AddScoped<IOperationalGlobalActivityDashboardService, OperationalGlobalActivityDashboardService>();
        services.AddScoped<IOperationalGlobalFailureService, OperationalGlobalFailureService>();
        services.AddScoped<IOperationalGlobalFailureMetricsService, OperationalGlobalFailureMetricsService>();
        services.AddScoped<IOperationalGlobalFailureDashboardService, OperationalGlobalFailureDashboardService>();
        services.AddScoped<IOperationalGlobalFailureQueryService, OperationalGlobalFailureQueryService>();
        services.AddScoped<IOperationalGlobalFailureCatalogService, OperationalGlobalFailureCatalogService>();
        services.AddScoped<IOperationalGlobalFailureSystemPairMetricsService, OperationalGlobalFailureSystemPairMetricsService>();
        services.AddScoped<IOperationalGlobalFailureRunStatusMetricsService, OperationalGlobalFailureRunStatusMetricsService>();
        services.AddScoped<IOperationalGlobalFailureAnalyticsDashboardService, OperationalGlobalFailureAnalyticsDashboardService>();
        services.AddScoped<IOperationalGlobalFailureFilteredAnalyticsService, OperationalGlobalFailureFilteredAnalyticsService>();
        services.AddScoped<IOperationalGlobalFailureAnalyticsPresetService, OperationalGlobalFailureAnalyticsPresetService>();
        services.AddScoped<IOperationalGlobalFailureAnalyticsPresetDashboardService, OperationalGlobalFailureAnalyticsPresetDashboardService>();
        services.AddScoped<IOperationalGlobalFailureAnalyticsPresetSearchService, OperationalGlobalFailureAnalyticsPresetSearchService>();
        services.AddScoped<IOperationalGlobalFailureAnalyticsPresetFavoriteService, OperationalGlobalFailureAnalyticsPresetFavoriteService>();
        services.AddScoped<IOperationalGlobalRunHealthSummaryService, OperationalGlobalRunHealthSummaryService>();
        services.AddScoped<IOperationalGlobalRunHealthDashboardService, OperationalGlobalRunHealthDashboardService>();
        services.AddScoped<IOperationalGlobalRunHealthSnapshotService, OperationalGlobalRunHealthSnapshotService>();
        services.AddScoped<IOperationalGlobalRunHealthTrendSummaryService, OperationalGlobalRunHealthTrendSummaryService>();
        services.AddScoped<IOperationalRunAutoFinalizationService, OperationalRunAutoFinalizationService>();
        services.AddScoped<IOperationalDispatcherService, OperationalDispatcherService>();
        services.AddScoped<IOperationalDispatcherDiagnosticsService, OperationalDispatcherDiagnosticsService>();
        services.AddScoped<IDispatcherExecutionHistoryService, DispatcherExecutionHistoryService>();
        services.AddScoped<IDispatcherExecutionHistoryReadinessService, DispatcherExecutionHistoryReadinessService>();
        services.AddScoped<IDispatcherExecutionHistoryMetricsService, DispatcherExecutionHistoryMetricsService>();
        services.AddScoped<IDispatcherExecutionHistoryQueryService, DispatcherExecutionHistoryQueryService>();
        services.AddScoped<IDispatcherExecutionHistoryRetentionService, DispatcherExecutionHistoryRetentionService>();
        services.AddScoped<IOperationalDispatcherDashboardSummaryService, OperationalDispatcherDashboardSummaryService>();
        services.AddScoped<IOperationalRetentionService, OperationalRetentionService>();

        services.AddHostedService<OperationalRunAutoFinalizationHostedService>();
        services.AddHostedService<OperationalDispatcherHostedService>();

        return services;
    }
}
