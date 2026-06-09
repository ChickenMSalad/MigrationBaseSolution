using System.Reflection;

namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure dashboard endpoints composed from already-registered operational analytics services.
/// This intentionally avoids hard dependencies on volatile DTO contracts.
/// </summary>
public static class OperationalQueuePressureDashboardEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureDashboardApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/dashboard",
                async (IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
                {
                    var queueDepth = await TryInvokeOperationalServiceAsync(
                        serviceProvider,
                        serviceTypeNameContains: new[] { "GlobalQueueDepth", "QueueDepth" },
                        preferredMethodNameContains: new[] { "Dashboard", "Analytics", "Metrics", "Summary" },
                        cancellationToken);

                    var dispatcherPressure = await TryInvokeOperationalServiceAsync(
                        serviceProvider,
                        serviceTypeNameContains: new[] { "DispatcherPressure", "Dispatcher" },
                        preferredMethodNameContains: new[] { "Dashboard", "Analytics", "Pressure", "Summary" },
                        cancellationToken);

                    return Results.Ok(new
                    {
                        dashboard = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            pressureSignals = new
                            {
                                queueDepth,
                                dispatcherPressure
                            },
                            readiness = BuildReadiness(queueDepth, dispatcherPressure)
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureDashboard")
            .WithTags("Operational Store")
            .WithSummary("Gets a composed operational queue pressure dashboard.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/summary",
                async (IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
                {
                    var queueDepth = await TryInvokeOperationalServiceAsync(
                        serviceProvider,
                        serviceTypeNameContains: new[] { "GlobalQueueDepth", "QueueDepth" },
                        preferredMethodNameContains: new[] { "Summary", "Dashboard", "Metrics", "Analytics" },
                        cancellationToken);

                    var dispatcherPressure = await TryInvokeOperationalServiceAsync(
                        serviceProvider,
                        serviceTypeNameContains: new[] { "DispatcherPressure", "Dispatcher" },
                        preferredMethodNameContains: new[] { "Summary", "Pressure", "Dashboard", "Analytics" },
                        cancellationToken);

                    return Results.Ok(new
                    {
                        summary = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            queueDepthAvailable = queueDepth.Available,
                            dispatcherPressureAvailable = dispatcherPressure.Available,
                            queueDepth,
                            dispatcherPressure
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureSummary")
            .WithTags("Operational Store")
            .WithSummary("Gets a composed operational queue pressure summary.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static object BuildReadiness(OperationalServiceInvocationResult queueDepth, OperationalServiceInvocationResult dispatcherPressure)
    {
        var availableSignalCount = new[] { queueDepth, dispatcherPressure }.Count(x => x.Available);
        var missingSignals = new[] { queueDepth, dispatcherPressure }
            .Where(x => !x.Available)
            .Select(x => x.ServiceSearch)
            .ToArray();

        return new
        {
            availableSignalCount,
            expectedSignalCount = 2,
            isFullyComposed = availableSignalCount == 2,
            missingSignals
        };
    }

    private static async Task<OperationalServiceInvocationResult> TryInvokeOperationalServiceAsync(
        IServiceProvider serviceProvider,
        IReadOnlyCollection<string> serviceTypeNameContains,
        IReadOnlyCollection<string> preferredMethodNameContains,
        CancellationToken cancellationToken)
    {
        var serviceType = FindServiceType(serviceTypeNameContains);
        if (serviceType is null)
        {
            return OperationalServiceInvocationResult.NotFound(string.Join("|", serviceTypeNameContains));
        }

        var service = serviceProvider.GetService(serviceType);
        if (service is null)
        {
            return OperationalServiceInvocationResult.NotRegistered(serviceType.FullName ?? serviceType.Name);
        }

        var method = FindPreferredAsyncMethod(serviceType, preferredMethodNameContains);
        if (method is null)
        {
            return OperationalServiceInvocationResult.NoMethod(serviceType.FullName ?? serviceType.Name);
        }

        try
        {
            var result = method.Invoke(service, BuildArguments(method, cancellationToken));
            var value = await AwaitIfNeededAsync(result);
            return OperationalServiceInvocationResult.Success(
                serviceType.FullName ?? serviceType.Name,
                method.Name,
                value);
        }
        catch (TargetInvocationException ex)
        {
            return OperationalServiceInvocationResult.Failed(
                serviceType.FullName ?? serviceType.Name,
                method.Name,
                ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return OperationalServiceInvocationResult.Failed(
                serviceType.FullName ?? serviceType.Name,
                method.Name,
                ex.Message);
        }
    }

    private static Type? FindServiceType(IReadOnlyCollection<string> serviceTypeNameContains)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(SafeGetTypes)
            .Where(t => t is { IsInterface: true })
            .Where(t => serviceTypeNameContains.Any(token =>
                t.Name.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                (t.FullName?.Contains(token, StringComparison.OrdinalIgnoreCase) ?? false)))
            .OrderByDescending(t => t.Name.Contains("AnalyticsService", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(t => t.Name.StartsWith("IOperational", StringComparison.OrdinalIgnoreCase))
            .ThenBy(t => t.FullName)
            .FirstOrDefault();
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static MethodInfo? FindPreferredAsyncMethod(Type serviceType, IReadOnlyCollection<string> preferredMethodNameContains)
    {
        return serviceType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
            .Where(m => IsAwaitableOrValueReturning(m.ReturnType))
            .Where(m => m.GetParameters().All(IsSupportedParameter))
            .OrderByDescending(m => preferredMethodNameContains.Any(token =>
                m.Name.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(m => m.GetParameters().Length)
            .ThenBy(m => m.Name)
            .FirstOrDefault();
    }

    private static bool IsAwaitableOrValueReturning(Type returnType)
    {
        return typeof(Task).IsAssignableFrom(returnType) || returnType != typeof(void);
    }

    private static bool IsSupportedParameter(ParameterInfo parameter)
    {
        var type = parameter.ParameterType;
        return type == typeof(CancellationToken)
            || type == typeof(int)
            || type == typeof(int?)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateTimeOffset?)
            || type == typeof(DateTime)
            || type == typeof(DateTime?)
            || type == typeof(string)
            || parameter.HasDefaultValue;
    }

    private static object?[] BuildArguments(MethodInfo method, CancellationToken cancellationToken)
    {
        return method.GetParameters()
            .Select(p =>
            {
                if (p.ParameterType == typeof(CancellationToken))
                {
                    return (object)cancellationToken;
                }

                if (p.HasDefaultValue)
                {
                    return p.DefaultValue;
                }

                if (p.ParameterType == typeof(int) || p.ParameterType == typeof(int?))
                {
                    return 25;
                }

                if (p.ParameterType == typeof(DateTimeOffset) || p.ParameterType == typeof(DateTimeOffset?))
                {
                    return DateTimeOffset.UtcNow.AddHours(-24);
                }

                if (p.ParameterType == typeof(DateTime) || p.ParameterType == typeof(DateTime?))
                {
                    return DateTime.UtcNow.AddHours(-24);
                }

                if (p.ParameterType == typeof(string))
                {
                    return string.Empty;
                }

                return null;
            })
            .ToArray();
    }

    private static async Task<object?> AwaitIfNeededAsync(object? result)
    {
        if (result is null)
        {
            return null;
        }

        if (result is Task task)
        {
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
            return resultProperty?.GetValue(task);
        }

        return result;
    }

    private sealed record OperationalServiceInvocationResult(
        bool Available,
        string ServiceSearch,
        string? ServiceType,
        string? Method,
        object? Payload,
        string? Error)
    {
        public static OperationalServiceInvocationResult Success(string serviceType, string method, object? payload) =>
            new(true, serviceType, serviceType, method, payload, null);

        public static OperationalServiceInvocationResult NotFound(string search) =>
            new(false, search, null, null, null, "No matching operational analytics service interface was found.");

        public static OperationalServiceInvocationResult NotRegistered(string serviceType) =>
            new(false, serviceType, serviceType, null, null, "Matching service interface exists but is not registered in DI.");

        public static OperationalServiceInvocationResult NoMethod(string serviceType) =>
            new(false, serviceType, serviceType, null, null, "No compatible public Get* method was found.");

        public static OperationalServiceInvocationResult Failed(string serviceType, string method, string error) =>
            new(false, serviceType, serviceType, method, null, error);
    }
}


