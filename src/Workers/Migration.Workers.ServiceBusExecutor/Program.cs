using Migration.Infrastructure.Sql.Operational.WorkItems;
using Migration.Workers.ServiceBusExecutor.Options;
using Migration.Workers.ServiceBusExecutor.Processing;
using Migration.Workers.ServiceBusExecutor.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Migration.Workers.QueueExecutor.Registration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<SqlServiceBusExecutorOptions>()
    .Bind(builder.Configuration.GetSection(SqlServiceBusExecutorOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.QueueName), "SqlServiceBusExecutor:QueueName is required.")
    .ValidateOnStart();

builder.Services.AddSqlOperationalWorkItemQueue();
builder.Services.AddSqlOperationalMigrationJobWorkItemExecutor(builder.Configuration);
builder.Services.AddSingleton<IServiceBusWorkItemExecutor, SqlOperationalServiceBusWorkItemExecutor>();
builder.Services.AddHostedService<SqlServiceBusExecutorWorker>();

await builder.Build().RunAsync();


