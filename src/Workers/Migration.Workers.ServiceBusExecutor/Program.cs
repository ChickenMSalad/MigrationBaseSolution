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

await app.RunAsync().ConfigureAwait(false);
