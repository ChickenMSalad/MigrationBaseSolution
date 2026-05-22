using System.Reflection;

namespace Migration.Admin.Api.Endpoints;

/// <summary>
/// Queue pressure action-plan endpoints composed from already-registered operational queue pressure APIs/services.
/// This keeps Set 143 compile-safe by avoiding new DTO/service dependencies and by returning operational guidance metadata.
/// </summary>
public static class OperationalQueuePressureActionPlanEndpointExtensions
{
    public static RouteGroupBuilder MapOperationalQueuePressureActionPlanApi(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);

        api.MapGet(
                "/operational/queue-pressure/action-plan",
                async (int? sampleLimit, IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
                {
                    var limit = Math.Clamp(sampleLimit.GetValueOrDefault(25), 1, 250);

                    var queueDepth = await TryInvokeOperationalServiceAsync(
                        serviceProvider,
                        serviceTypeNameContains: new[] { "GlobalQueueDepth", "QueueDepth" },
                        preferredMethodNameContains: new[] { "Analytics", "Dashboard", "Summary", "Metrics", "Trend" },
                        sampleLimit: limit,
                        cancellationToken);

                    var dispatcherPressure = await TryInvokeOperationalServiceAsync(
                        serviceProvider,
                        serviceTypeNameContains: new[] { "DispatcherPressure", "Dispatcher" },
                        preferredMethodNameContains: new[] { "Pressure", "Dashboard", "Analytics", "Summary", "Trend" },
                        sampleLimit: limit,
                        cancellationToken);

                    var trend = await TryInvokeEndpointLikeServiceAsync(
                        serviceProvider,
                        serviceTypeNameContains: new[] { "QueuePressureTrend" },
                        preferredMethodNameContains: new[] { "Trend", "Readiness", "Dashboard" },
                        sampleLimit: limit,
                        cancellationToken);

                    var signals = new[] { queueDepth, dispatcherPressure, trend };

                    return Results.Ok(new
                    {
                        actionPlan = new
                        {
                            generatedAtUtc = DateTimeOffset.UtcNow,
                            sampleLimit = limit,
                            pressurePosture = BuildPressurePosture(signals),
                            recommendedActions = BuildRecommendedActions(signals),
                            signals = new
                            {
                                queueDepth,
                                dispatcherPressure,
                                trend
                            },
                            readiness = BuildReadiness(signals)
                        }
                    });
                })
            .WithName("GetOperationalQueuePressureActionPlan")
            .WithTags("Operational Store")
            .WithSummary("Gets an operator action plan for queue pressure signals.")
            .Produces(StatusCodes.Status200OK);

        api.MapGet(
                "/operational/queue-pressure/action-plan/readiness",
                async (IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
                {
                    var queueDepth = await TryInvokeOperationalServiceAsync(
                        serviceProvider,
                        serviceTypeNameContains: new[] { "GlobalQueueDepth", "QueueDepth" },
                        preferredMethodNameContains: new[] { "Analytics", "Dashboard", "Summary", "Metrics", "Trend" },
                        sampleLimit: 10,
                        cancellationToken);

                    var dispatcherPressure = await TryInvokeOperationalServiceAsync(
                        serviceProvider,
                        serviceTypeNameContains: new[] { "DispatcherPressure", "Dispatcher" },
                        preferredMethodNameContains: new[] { "Pressure", "Dashboard", "Analytics", "Summary", "Trend" },
                        sampleLimit: 10,
                        cancellationToken);

                    return Results.Ok(new
                    {
                        readiness = BuildReadiness(new[] { queueDepth, dispatcherPressure })
                    });
                })
            .WithName("GetOperationalQueuePressureActionPlanReadiness")
            .WithTags("Operational Store")
            .WithSummary("Gets queue pressure action-plan readiness.")
            .Produces(StatusCodes.Status200OK);

        return api;
    }

    private static object BuildPressurePosture(IReadOnlyCollection<OperationalServiceInvocationResult> signals)
    {
        var availableSignalCount = signals.Count(x => x.Available);
        var failedSignalCount = signals.Count(x => !x.Available && !string.IsNullOrWhiteSpace(x.Error));
        var posture = availableSignalCount switch
        {
            >= 2 when failedSignalCount == 0 => "Ready",
            >= 1 => "Partial",
            _ => "Limited"
        };

        return new
        {
            posture,
            availableSignalCount,
            expectedSignalCount = signals.Count,
            failedSignalCount
        };
    }

    private static IReadOnlyCollection<object> BuildRecommendedActions(IReadOnlyCollection<OperationalServiceInvocationResult> signals)
    {
        var actions = new List<object>
        {
            new
            {
                priority = 1,
                category = "Queue",
                action = "Review queue depth and outstanding work item count before scaling migration workers.",
                reason = "Queue pressure should be understood before adding dispatcher capacity."
            },
            new
            {
                priority = 2,
                category = "Dispatcher",
                action = "Compare dispatcher pressure with queue pressure to identify whether work is building up or draining.",
                reason = "Dispatcher pressure and queue depth together provide the safest operational read."
            }
        };

        if (signals.Any(x => !x.Available))
        {
            actions.Add(new
            {
                priority = 3,
                category = "Readiness",
                action = "Verify the missing queue pressure services are registered before using this view for operational decisions.",
                reason = "One or more composed signals were unavailable."
            });
        }
        else
        {
            actions.Add(new
            {
                priority = 3,
                category = "Validation",
                action = "Use the dashboard and trend endpoints to confirm whether pressure is transient or sustained.",
                reason = "All core queue pressure signals were available."
            });
        }

        return actions;
    }

    private static object BuildReadiness(IReadOnlyCollection<OperationalServiceInvocationResult> signals)
    {
        var availableSignalCount = signals.Count(x => x.Available);
        var missingSignals = signals
            .Where(x => !x.Available)
            .Select(x => new
            {
                x.ServiceSearch,
                x.Error
            })
            .ToArray();

        return new
        {
            availableSignalCount,
            expectedSignalCount = signals.Count,
            isFullyComposed = availableSignalCount == signals.Count,
            missingSignals
        };
    }

    private static Task<OperationalServiceInvocationResult> TryInvokeEndpointLikeServiceAsync(
        IServiceProvider serviceProvider,
        IReadOnlyCollection<string> serviceTypeNameContains,
        IReadOnlyCollection<string> preferredMethodNameContains,
        int sampleLimit,
        CancellationToken cancellationToken)
    {
        return TryInvokeOperationalServiceAsync(
            serviceProvider,
            serviceTypeNameContains,
            preferredMethodNameContains,
            sampleLimit,
            cancellationToken);
    }

    private static async Task<OperationalServiceInvocationResult> TryInvokeOperationalServiceAsync(
        IServiceProvider serviceProvider,
        IReadOnlyCollection<string> serviceTypeNameContains,
        IReadOnlyCollection<string> preferredMethodNameContains,
        int sampleLimit,
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

        var method = FindPreferredMethod(serviceType, preferredMethodNameContains);
        if (method is null)
        {
            return OperationalServiceInvocationResult.NoMethod(serviceType.FullName ?? serviceType.Name);
        }

        try
        {
            var result = method.Invoke(service, BuildArguments(method, sampleLimit, cancellationToken));
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

    private static MethodInfo? FindPreferredMethod(Type serviceType, IReadOnlyCollection<string> preferredMethodNameContains)
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

    private static object?[] BuildArguments(MethodInfo method, int sampleLimit, CancellationToken cancellationToken)
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
                    return sampleLimit;
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
