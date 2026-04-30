using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Migration.Hosts.Ashley.AemToAprimo.Console.Infrastructure;
using Migration.Hosts.Ashley.AemToAprimo.Console.Registration;
using Migration.Shared.Extensions;

SQLitePCL.Batteries.Init();
StartupExtensions.ConfigureThirdPartyLicenses();

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables(prefix: "MIGRATION_");

builder.Services.AddAshleyAemToAprimoHost(builder.Configuration);

var host = builder.Build();

using var scope = host.Services.CreateScope();
var menu = scope.ServiceProvider.GetRequiredService<PluginMenuBuilder>();
var plugins = scope.ServiceProvider.GetRequiredService<IEnumerable<IPlugin>>()
    .OrderBy(x => x.Priority)
    .ThenBy(x => x.Name)
    .ToList();

menu.Create(plugins);
