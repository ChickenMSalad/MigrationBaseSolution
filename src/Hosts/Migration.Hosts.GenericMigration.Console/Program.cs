using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Migration.Hosts.GenericMigration.Console.Infrastructure;
using Migration.Hosts.GenericMigration.Console.Registration;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables(prefix: "MIGRATION_");

builder.Services.AddLogging(logging => logging.AddConsole());
builder.Services.AddGenericMigrationConsole(builder.Configuration);

var host = builder.Build();

using var scope = host.Services.CreateScope();
var menu = scope.ServiceProvider.GetRequiredService<GenericMigrationMenu>();
await menu.RunAsync(args);
