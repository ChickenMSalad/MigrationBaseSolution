using Microsoft.Extensions.Hosting;
using Migration.Workers.QueueExecutor.Configuration;
using Migration.Workers.QueueExecutor.Registration;

var builder = Host.CreateApplicationBuilder(args);

builder.ConfigureQueueExecutorConfiguration();
builder.LogQueueExecutorConfiguration();

builder.Services.AddMigrationQueueExecutor(builder.Configuration);

await builder.Build().RunAsync().ConfigureAwait(false);
