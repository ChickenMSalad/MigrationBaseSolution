using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Migration.Hosts.Cloudinary.CsvToCloudinary.Console.Infrastructure;
using Migration.Hosts.Cloudinary.CsvToCloudinary.Console.Registration;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables(prefix: "MIGRATION_");

builder.Services.AddCloudinaryCsvToCloudinaryHost(builder.Configuration);

var host = builder.Build();

using var scope = host.Services.CreateScope();
var menu = scope.ServiceProvider.GetRequiredService<PluginMenuBuilder>();
var plugins = scope.ServiceProvider.GetRequiredService<IEnumerable<IPlugin>>()
    .OrderBy(x => x.Priority)
    .ThenBy(x => x.Name)
    .ToList();

menu.Create(plugins);
