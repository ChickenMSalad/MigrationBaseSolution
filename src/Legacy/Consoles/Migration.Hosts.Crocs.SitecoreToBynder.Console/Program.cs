using Amazon.S3;
using Bynder.Sdk.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Migration.Application.Configuration.Workflows;
using Migration.Connectors.Sources.S3.Clients;
using Migration.Connectors.Sources.S3.Configuration;
using Migration.Connectors.Sources.Sitecore.Configuration;
using Migration.Connectors.Targets.Bynder.Configuration;
using Migration.Hosts.Crocs.SitecoreToBynder.Console.Infrastructure;
using Migration.Hosts.Crocs.SitecoreToBynder.Console.Registration;
using Migration.Shared.Configuration.Hosts.Bynder;
using Migration.Shared.Configuration.Hosts.Sitecore;
using Migration.Shared.Extensions;
using Migration.Shared.Files;
using Migration.Shared.Storage;

StartupExtensions.ConfigureThirdPartyLicenses();

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables(prefix: "MIGRATION_");

builder.Services.AddSingleton<IConsoleReaderService, ConsoleReaderService>();
builder.Services.AddSingleton<PluginMenuBuilder>();

builder.Services.Configure<BynderOptions>(builder.Configuration.GetSection(BynderOptions.SectionName));
builder.Services.Configure<ContentHubOptions>(builder.Configuration.GetSection("ContentHub"));
builder.Services.Configure<NodeBynderScriptOptions>(builder.Configuration.GetSection("NodeBynderScript"));
builder.Services.Configure<SitecoreHostOptions>(builder.Configuration.GetSection("Sitecore"));
builder.Services.Configure<BynderHostOptions>(builder.Configuration.GetSection("BynderHost"));
builder.Services.Configure<SitecoreToBynderWorkflowOptions>(builder.Configuration.GetSection("Workflow"));
builder.Services.Configure<List<BlobStorageSettings>>(builder.Configuration.GetSection("BlobStorage"));

builder.Services.AddMemoryCache();
builder.Services.AddScoped<Migration.Connectors.Targets.Bynder.Models.ExecutionContextState>();

builder.Services.AddSitecoreHostServices(builder.Configuration);
builder.Services.AddBynderHostServices();
builder.Services.AddPlugins();

var host = builder.Build();

using var scope = host.Services.CreateScope();
var menu = scope.ServiceProvider.GetRequiredService<PluginMenuBuilder>();
var plugins = scope.ServiceProvider.GetRequiredService<IEnumerable<IPlugin>>()
    .OrderBy(x => x.Priority)
    .ToList();

menu.Create(plugins);
