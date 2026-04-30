using Microsoft.Extensions.Hosting;
using Migration.Workers.QueueExecutor.Registration;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables(prefix: "MIGRATION_");

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"ControlPlane:StorageRoot = {builder.Configuration["ControlPlane:StorageRoot"]}");
Console.WriteLine($"MigrationRunQueue:Provider = {builder.Configuration["MigrationRunQueue:Provider"]}");
Console.WriteLine($"MigrationRunQueue:QueueName = {builder.Configuration["MigrationRunQueue:QueueName"]}");

builder.Services.AddMigrationQueueExecutor(builder.Configuration);

await builder.Build().RunAsync().ConfigureAwait(false);
