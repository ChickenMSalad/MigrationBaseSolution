using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Migration.Workers.ServiceBusDispatcher.Dispatching;
using Migration.Workers.ServiceBusDispatcher.Options;
using Migration.Workers.ServiceBusDispatcher.Runtime;
using Migration.Application.Operational.Telemetry;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<SqlServiceBusDispatcherOptions>()
    .Bind(builder.Configuration.GetSection("SqlServiceBusDispatcher"))
    .Validate(options => options.BatchSize > 0, "SqlServiceBusDispatcher:BatchSize must be greater than zero.")
    .Validate(options => options.PollIntervalSeconds > 0, "SqlServiceBusDispatcher:PollIntervalSeconds must be greater than zero.");

builder.Services.AddSingleton<SqlWorkItemDispatcher>();
builder.Services.AddHostedService<SqlServiceBusDispatcherWorker>();
builder.Services.AddOperationalOpenTelemetry(builder.Configuration);

await builder.Build().RunAsync().ConfigureAwait(false);
