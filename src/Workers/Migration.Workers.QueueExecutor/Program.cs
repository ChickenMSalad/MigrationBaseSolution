using Migration.Workers.QueueExecutor.Registration;

var builder = Host.CreateApplicationBuilder(args);

Migration.Workers.QueueExecutor.Configuration.QueueExecutorConfigurationExtensions.ConfigureMigrationQueueExecutorConfiguration(builder);
Migration.Workers.QueueExecutor.Configuration.QueueExecutorConfigurationExtensions.LogMigrationQueueExecutorConfiguration(builder);

builder.Services.AddMigrationQueueExecutor(builder.Configuration);

await builder.Build().RunAsync().ConfigureAwait(false);
