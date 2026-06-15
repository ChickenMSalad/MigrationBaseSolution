using Microsoft.Extensions.Configuration;
using Migration.Application.Operational.Telemetry;
using Migration.Workers.ServiceBusDispatcher.Dispatching;
using Migration.Workers.ServiceBusDispatcher.Options;
using Migration.Workers.ServiceBusDispatcher.Runtime;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "MIGRATION_");

builder.Services
    .AddOptions<SqlServiceBusDispatcherOptions>()
    .Bind(builder.Configuration.GetSection("SqlServiceBusDispatcher"))
    .Validate(options => options.BatchSize > 0, "SqlServiceBusDispatcher:BatchSize must be greater than zero.")
    .Validate(options => options.PollIntervalSeconds > 0, "SqlServiceBusDispatcher:PollIntervalSeconds must be greater than zero.");

builder.Services.AddSingleton<SqlWorkItemDispatcher>();
builder.Services.AddHostedService<SqlServiceBusDispatcherWorker>();
builder.Services.AddOperationalOpenTelemetry(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Text("OK", "text/plain"));
app.MapGet("/health", () => Results.Text("Healthy", "text/plain"));
app.MapGet("/health/live", () => Results.Ok(new
{
    status = "live",
    component = "servicebus-dispatcher",
    generatedUtc = DateTimeOffset.UtcNow
}));
app.MapGet("/health/ready", () => BuildDispatcherReadinessResult(app.Configuration));
app.MapGet("/ready", () => BuildDispatcherReadinessResult(app.Configuration));

await app.RunAsync().ConfigureAwait(false);

static IResult BuildDispatcherReadinessResult(IConfiguration configuration)
{
    SqlServiceBusDispatcherOptions options = BindDispatcherOptions(configuration);
    (string Name, bool Ok, string Detail)[] checks = BuildDispatcherReadinessChecks(options);
    bool isReady = checks.All(check => check.Ok);

    var response = new
    {
        status = isReady ? "ready" : "not-ready",
        component = "servicebus-dispatcher",
        workerId = options.WorkerId,
        queueName = options.QueueName,
        enabled = options.Enabled,
        batchSize = options.BatchSize,
        pollIntervalSeconds = options.PollIntervalSeconds,
        leaseSeconds = options.LeaseSeconds,
        generatedUtc = DateTimeOffset.UtcNow,
        checks = checks.Select(check => new
        {
            name = check.Name,
            ok = check.Ok,
            detail = check.Detail
        }).ToArray()
    };

    return isReady
        ? Results.Ok(response)
        : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
}

static SqlServiceBusDispatcherOptions BindDispatcherOptions(IConfiguration configuration)
{
    SqlServiceBusDispatcherOptions options = new();
    configuration.GetSection("SqlServiceBusDispatcher").Bind(options);
    return options;
}

static (string Name, bool Ok, string Detail)[] BuildDispatcherReadinessChecks(SqlServiceBusDispatcherOptions options)
{
    return new (string Name, bool Ok, string Detail)[]
    {
        ("enabled", options.Enabled, options.Enabled ? "Dispatcher is enabled." : "Dispatcher is disabled."),
        ("sqlConnectionString", !string.IsNullOrWhiteSpace(options.SqlConnectionString), "SqlServiceBusDispatcher:SqlConnectionString must be configured."),
        ("serviceBusConnectionString", !string.IsNullOrWhiteSpace(options.ServiceBusConnectionString), "SqlServiceBusDispatcher:ServiceBusConnectionString must be configured."),
        ("queueName", !string.IsNullOrWhiteSpace(options.QueueName), "SqlServiceBusDispatcher:QueueName must be configured."),
        ("batchSize", options.BatchSize > 0, "SqlServiceBusDispatcher:BatchSize must be greater than zero."),
        ("pollIntervalSeconds", options.PollIntervalSeconds > 0, "SqlServiceBusDispatcher:PollIntervalSeconds must be greater than zero."),
        ("leaseSeconds", options.LeaseSeconds > 0, "SqlServiceBusDispatcher:LeaseSeconds must be greater than zero.")
    };
}
