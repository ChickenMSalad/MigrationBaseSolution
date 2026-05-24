using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Migration.Infrastructure.Runtime.Composition;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddP7SqlOperationalRuntime(builder.Configuration, createConnection: services =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("MigrationOperationalStore");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'MigrationOperationalStore' is required for the SQL operational worker host.");
    }

    IDbConnection connection = new SqlConnection(connectionString);
    return connection;
});

builder.Services.AddHostedService<SqlOperationalWorkerStartupProbe>();

await builder.Build().RunAsync();

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

        var readinessProbe = scope.ServiceProvider.GetRequiredService<SqlOperationalRuntimeReadinessProbe>();
        var ready = await readinessProbe.CheckAsync(stoppingToken).ConfigureAwait(false);

        if (!ready)
        {
            throw new InvalidOperationException("P7 SQL operational runtime readiness check failed. Confirm P7 SQL scripts 001, 002, and 003 have been applied.");
        }

        _logger.LogInformation("P7 SQL operational worker host readiness check passed.");
    }
}
