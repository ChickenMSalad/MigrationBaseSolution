using Migration.Workers.QueueExecutor.Registration;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

Migration.Workers.QueueExecutor.Configuration.QueueExecutorConfigurationExtensions.ConfigureMigrationQueueExecutorConfiguration(builder);
Migration.Workers.QueueExecutor.Configuration.QueueExecutorConfigurationExtensions.LogMigrationQueueExecutorConfiguration(builder);

builder.Services.AddMigrationQueueExecutor(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Text("OK", "text/plain"));

app.MapGet("/health", () => Results.Text("Healthy", "text/plain"));

await app.RunAsync().ConfigureAwait(false);