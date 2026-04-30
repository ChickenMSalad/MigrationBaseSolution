using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Migration.Orchestration.Abstractions;
using Migration.Orchestration.Options;

namespace Migration.Orchestration.Retry;

public sealed class SimpleMigrationRetryPolicy : IMigrationRetryPolicy
{
    private readonly RetryOptions _options;
    private readonly ILogger<SimpleMigrationRetryPolicy> _logger;
    private readonly Random _random = new();

    public SimpleMigrationRetryPolicy(IOptions<MigrationExecutionOptions> options, ILogger<SimpleMigrationRetryPolicy> logger)
    {
        _options = options.Value.Retry;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(string operationName, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var attempts = Math.Max(1, _options.MaxAttempts);
        var delay = Math.Max(0, _options.InitialDelayMilliseconds);
        var maxDelay = Math.Max(delay, _options.MaxDelayMilliseconds);

        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < attempts && IsRetryable(ex))
            {
                var actualDelay = CalculateDelay(delay, maxDelay);
                _logger.LogWarning(
                    ex,
                    "Operation {OperationName} failed on attempt {Attempt}/{MaxAttempts}. Retrying after {Delay} ms.",
                    operationName,
                    attempt,
                    attempts,
                    actualDelay);

                if (actualDelay > 0)
                {
                    await Task.Delay(actualDelay, cancellationToken).ConfigureAwait(false);
                }

                delay = (int)Math.Min(maxDelay, Math.Round(delay * Math.Max(1.0, _options.BackoffMultiplier)));
            }
        }
    }

    public Task ExecuteAsync(string operationName, Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
        ExecuteAsync<object?>(operationName, async token =>
        {
            await operation(token).ConfigureAwait(false);
            return null;
        }, cancellationToken);

    private int CalculateDelay(int delay, int maxDelay)
    {
        if (delay <= 0)
        {
            return 0;
        }

        var capped = Math.Min(delay, maxDelay);
        if (!_options.UseJitter)
        {
            return capped;
        }

        // +/- 20% jitter to avoid thundering herds when cloud-hosted.
        var min = (int)Math.Round(capped * 0.8);
        var max = (int)Math.Round(capped * 1.2);
        return _random.Next(Math.Max(0, min), Math.Max(min + 1, max));
    }

    private bool IsRetryable(Exception ex)
    {
        var type = ex.GetType();
        if (Matches(_options.NonRetryableExceptionTypeNames, type))
        {
            return false;
        }

        return _options.RetryableExceptionTypeNames.Count == 0 || Matches(_options.RetryableExceptionTypeNames, type);
    }

    private static bool Matches(IEnumerable<string> names, Type type)
    {
        return names.Any(name =>
            string.Equals(name, type.Name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, type.FullName, StringComparison.OrdinalIgnoreCase));
    }
}
