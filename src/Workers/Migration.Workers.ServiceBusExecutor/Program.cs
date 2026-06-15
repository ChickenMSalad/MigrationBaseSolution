using Microsoft.Extensions.Configuration;
using Migration.Application.Operational.Telemetry;
using Migration.ControlPlane.Registration;
using Migration.Infrastructure.Sql.Operational.WorkItems;
using Migration.Workers.QueueExecutor.Registration;
using Migration.Workers.ServiceBusExecutor.Options;
using Migration.Workers.ServiceBusExecutor.Processing;
using Migration.Workers.ServiceBusExecutor.Runtime;
using Migration.Workers.ServiceBusExecutor.Smoke;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "MIGRATION_");

builder.Services
    .AddOptions<SqlServiceBusExecutorOptions>()
    .Bind(builder.Configuration.GetSection(SqlServiceBusExecutorOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.QueueName), "SqlServiceBusExecutor:QueueName is required.")
    .ValidateOnStart();

// This composes the control plane, credential hydration, generic runtime, manifests,
// source connectors, target connectors, mapping, validation, and orchestration.
builder.Services.AddSqlOperationalMigrationJobRuntime(builder.Configuration);
builder.Services.AddSqlOperationalWorkItemQueue();
builder.Services.AddSqlOperationalMigrationJobWorkItemExecutor(builder.Configuration);
builder.Services.AddRuntimeSmokeExecutionProviders();
builder.Services.AddSingleton<IServiceBusWorkItemExecutor, SqlOperationalServiceBusWorkItemExecutor>();
builder.Services.AddHostedService<SqlServiceBusExecutorWorker>();
builder.Services.AddOperationalOpenTelemetry(builder.Configuration);

// Keep the SQL-backed credential metadata store overlay available for this host.
builder.Services.AddMigrationControlPlane(builder.Configuration);
builder.Services.AddSqlOperationalRunCoordinator(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Text("OK", "text/plain"));
app.MapGet("/health", () => Results.Text("Healthy", "text/plain"));
app.MapGet("/health/live", () => Results.Ok(new
{
    status = "live",
    component = "servicebus-executor",
    generatedUtc = DateTimeOffset.UtcNow
}));
app.MapGet("/health/ready", () => BuildExecutorReadinessResult(app.Configuration));
app.MapGet("/ready", () => BuildExecutorReadinessResult(app.Configuration));

await app.RunAsync().ConfigureAwait(false);

static IResult BuildExecutorReadinessResult(IConfiguration configuration)
{
    SqlServiceBusExecutorOptions options = BindExecutorOptions(configuration);
    (string Name, bool Ok, string Detail)[] checks = BuildExecutorReadinessChecks(options);
    bool isReady = checks.All(check => check.Ok);

    var response = new
    {
        status = isReady ? "ready" : "not-ready",
        component = "servicebus-executor",
        workerId = options.WorkerId,
        queueName = options.QueueName,
        maxConcurrentCalls = options.MaxConcurrentCalls,
        retryDelaySeconds = options.RetryDelaySeconds,
        completeWithoutExecutingMigration = options.CompleteWithoutExecutingMigration,
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

static SqlServiceBusExecutorOptions BindExecutorOptions(IConfiguration configuration)
{
    SqlServiceBusExecutorOptions options = new();
    configuration.GetSection(SqlServiceBusExecutorOptions.SectionName).Bind(options);
    return options;
}

static (string Name, bool Ok, string Detail)[] BuildExecutorReadinessChecks(SqlServiceBusExecutorOptions options)
{
    return new (string Name, bool Ok, string Detail)[]
    {
        ("serviceBusConnectionString", !string.IsNullOrWhiteSpace(options.ServiceBusConnectionString), "SqlServiceBusExecutor:ServiceBusConnectionString must be configured."),
        ("queueName", !string.IsNullOrWhiteSpace(options.QueueName), "SqlServiceBusExecutor:QueueName must be configured."),
        ("maxConcurrentCalls", options.MaxConcurrentCalls > 0, "SqlServiceBusExecutor:MaxConcurrentCalls must be greater than zero."),
        ("retryDelaySeconds", options.RetryDelaySeconds > 0, "SqlServiceBusExecutor:RetryDelaySeconds must be greater than zero.")
    };
}
