using Migration.Workers.ServiceBusExecutor.Smoke;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Migration.Application.Abstractions;
using Migration.Application.Operational.Telemetry;
using Migration.Infrastructure.Mapping;
using Migration.Infrastructure.Profiles;
using Migration.Infrastructure.Sql.Operational.WorkItems;
using Migration.Orchestration.Extensions;
using Migration.Workers.QueueExecutor.Registration;
using Migration.Workers.QueueExecutor.Services;
using Migration.Workers.ServiceBusExecutor.Options;
using Migration.Workers.ServiceBusExecutor.Processing;
using Migration.Workers.ServiceBusExecutor.Runtime;
using Migration.ControlPlane.Registration;


var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<SqlServiceBusExecutorOptions>()
    .Bind(builder.Configuration.GetSection(SqlServiceBusExecutorOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.QueueName), "SqlServiceBusExecutor:QueueName is required.")
    .ValidateOnStart();

builder.Services.AddSqlOperationalMigrationJobRuntime(builder.Configuration);
builder.Services.AddSqlOperationalWorkItemQueue();
builder.Services.AddSqlOperationalMigrationJobWorkItemExecutor(builder.Configuration);
builder.Services.AddRuntimeSmokeExecutionProviders();
builder.Services.AddSingleton<IServiceBusWorkItemExecutor, SqlOperationalServiceBusWorkItemExecutor>();
builder.Services.AddHostedService<SqlServiceBusExecutorWorker>();
builder.Services.AddOperationalOpenTelemetry(builder.Configuration);
builder.Services.AddMigrationOrchestration(builder.Configuration);
builder.Services.AddSingleton<IMappingProfileLoader, JsonMappingProfileLoader>();
builder.Services.AddSingleton<IMapper, CanonicalMapper>();
builder.Services.AddSingleton<ProjectCredentialJobSettingsHydrator>();
builder.Services.AddMigrationControlPlane(builder.Configuration);

await builder.Build().RunAsync();




