using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Migration.Application.Abstractions.OperationalStore;
using Migration.Application.OperationalStore;

namespace Migration.Application.Registration;

public static class OperationalDispatchServiceCollectionExtensions
{
    public static IServiceCollection AddOperationalDispatch(
        this IServiceCollection services)
    {
        services.TryAddScoped<IOperationalExecutionContextFactory, OperationalExecutionContextFactory>();

        services.TryAddScoped<IOperationalManifestRecordBuilder, OperationalManifestRecordBuilder>();
        services.TryAddScoped<IOperationalManifestLifecycleService, OperationalManifestLifecycleService>();
        services.TryAddScoped<IOperationalManifestDispatchService, OperationalManifestDispatchService>();

        services.TryAddScoped<IOperationalQueueMessageFactory, OperationalQueueMessageFactory>();
        services.TryAddScoped<IOperationalQueueMessageSerializer, OperationalQueueMessageSerializer>();

        services.TryAddScoped<IOperationalQueuePublisher, NullOperationalQueuePublisher>();
        services.TryAddScoped<IOperationalWorkItemQueuePublisher, OperationalWorkItemQueuePublisher>();

        services.TryAddScoped<IOperationalRunLifecycleService, OperationalRunLifecycleService>();
        services.TryAddScoped<IOperationalWorkItemLifecycleService, OperationalWorkItemLifecycleService>();
        services.TryAddScoped<IOperationalWorkItemExecutionSynchronizer, OperationalWorkItemExecutionSynchronizer>();

        services.TryAddScoped<IOperationalWorkItemDispatchService, OperationalWorkItemDispatchService>();
        services.TryAddScoped<IOperationalRunDispatchService, OperationalRunDispatchService>();

        services.TryAddScoped<IOperationalRunDispatchCommandService, OperationalRunDispatchCommandService>();
        services.TryAddScoped<IOperationalRunDispatchRequestValidator, OperationalRunDispatchRequestValidator>();
        services.TryAddScoped<IOperationalRunDispatchRequestHandler, OperationalRunDispatchRequestHandler>();
        services.TryAddScoped<IOperationalRunDispatchSampleRequestFactory, OperationalRunDispatchSampleRequestFactory>();

        return services;
    }
}