using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<SqlOperationalWorkerStartupProbe>();

await builder.Build().RunAsync();

internal sealed class SqlOperationalWorkerStartupProbe : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlOperationalWorkerStartupProbe> _logger;

    public SqlOperationalWorkerStartupProbe(
        IConfiguration configuration,
        ILogger<SqlOperationalWorkerStartupProbe> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration.GetConnectionString("MigrationOperationalStore");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'MigrationOperationalStore' is required for the SQL operational worker host.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(stoppingToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        command.CommandTimeout = 30;

        var result = await command.ExecuteScalarAsync(stoppingToken).ConfigureAwait(false);

        if (!Equals(result, 1))
        {
            throw new InvalidOperationException("SQL readiness probe failed.");
        }

        _logger.LogInformation("SQL operational worker host startup probe passed.");

        await Task.CompletedTask.ConfigureAwait(false);
    }
}