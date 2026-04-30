using System.Reflection;

namespace Migration.Hosts.Ashley.AemToAprimo.Console.Infrastructure;

public static class ServiceMethodInvoker
{
    public static Task InvokeAsync(object target, string methodName, CancellationToken cancellationToken)
        => InvokeAsync(target, methodName, null, cancellationToken);

    public static async Task InvokeAsync(object target, string methodName, object? fixedArgument, CancellationToken cancellationToken)
    {
        var methods = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal))
            .ToList();

        if (methods.Count == 0)
        {
            throw new MissingMethodException(target.GetType().FullName, methodName);
        }

        Exception? lastError = null;
        foreach (var method in methods)
        {
            try
            {
                var args = BuildArguments(method, fixedArgument, cancellationToken);
                var result = method.Invoke(target, args);
                if (result is Task task)
                {
                    await task.ConfigureAwait(false);
                }
                return;
            }
            catch (Exception ex)
            {
                lastError = ex is TargetInvocationException tie && tie.InnerException is not null ? tie.InnerException : ex;
            }
        }

        throw lastError ?? new InvalidOperationException($"Unable to invoke {methodName} on {target.GetType().Name}.");
    }

    private static object?[] BuildArguments(MethodInfo method, object? fixedArgument, CancellationToken cancellationToken)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return Array.Empty<object?>();
        }

        var values = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (p.ParameterType == typeof(CancellationToken))
            {
                values[i] = cancellationToken;
                continue;
            }

            if (fixedArgument is not null && p.ParameterType.IsInstanceOfType(fixedArgument))
            {
                values[i] = fixedArgument;
                fixedArgument = null; // consume once
                continue;
            }

            if (p.HasDefaultValue)
            {
                values[i] = p.DefaultValue;
                continue;
            }

            throw new InvalidOperationException($"No value available for parameter '{p.Name}' on method '{method.Name}'.");
        }

        return values;
    }
}
