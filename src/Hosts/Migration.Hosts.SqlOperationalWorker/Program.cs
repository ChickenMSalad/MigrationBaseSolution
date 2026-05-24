using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Migration.Application.Operational.Readiness;
using Migration.Workers.QueueExecutor.Registration;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables(prefix: "MIGRATION_");

builder.Services.AddSqlOperationalRuntimeReadiness(builder.Configuration);
builder.Services.AddSqlOperationalQueueExecutor(builder.Configuration);
builder.Services.AddSqlOperationalMigrationJobWorkItemExecutor(builder.Configuration);
builder.Services.AddHostedService<SqlOperationalWorkerStartupProbe>();

await builder.Build().RunAsync().ConfigureAwait(false);

internal sealed class SqlOperationalWorkerStartupProbe : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SqlOperationalWorkerStartupProbe> _logger;

    public SqlOperationalWorkerStartupProbe(
        IServiceProvider serviceProvider,
        ILogger<SqlOperationalWorkerStartupProbe> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var readiness = scope.ServiceProvider.GetRequiredService<IOperationalRuntimeReadinessService>();
        var report = await readiness.GetReadinessAsync(stoppingToken).ConfigureAwait(false);

        if (!report.IsReady)
        {
            var issues = string.Join("; ", report.BlockingIssues);
            throw new InvalidOperationException($"SQL operational runtime readiness check failed: {issues}");
        }

        _logger.LogInformation(
            "SQL operational worker host readiness check passed. Status={Status}; EvaluatedUtc={EvaluatedUtc}",
            report.Status,
            report.EvaluatedUtc);
    }
}
