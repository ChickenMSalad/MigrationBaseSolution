using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Migration.Connectors.Sources.S3.Configuration;
using Migration.Connectors.Sources.WebDam.Configuration;
using Migration.Shared.Configuration.Hosts.WebDam;
using Migration.Application.Configuration.Workflows;
using Migration.Shared.Configuration.Hosts.Bynder;
using Migration.Connectors.Targets.Bynder.Models;
using Migration.Hosts.WebDamToBynder.Console.Infrastructure;
using Migration.Hosts.WebDamToBynder.Console.Registration;
using Migration.Shared.Extensions;
using Migration.Shared.Files;
using Migration.Shared.Storage;
using Migration.Connectors.Targets.Bynder.Configuration;

StartupExtensions.ConfigureThirdPartyLicenses();

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables(prefix: "MIGRATION_");

builder.Services.AddSingleton<IConsoleReaderService, ConsoleReaderService>();
builder.Services.AddSingleton<PluginMenuBuilder>();
builder.Services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ILogger<Program>>());

builder.Services.Configure<BynderOptions>(builder.Configuration.GetSection(BynderOptions.SectionName));
builder.Services.Configure<S3Options>(builder.Configuration.GetSection("S3"));
builder.Services.Configure<WebDamOptions>(builder.Configuration.GetSection(WebDamOptions.SectionName));
builder.Services.Configure<WebDamHostOptions>(builder.Configuration.GetSection("WebDamHost"));
builder.Services.Configure<WebDamToBynderWorkflowOptions>(builder.Configuration.GetSection("Workflow"));
builder.Services.Configure<List<BlobStorageSettings>>(builder.Configuration.GetSection("BlobStorage"));
builder.Services.Configure<BynderHostOptions>(builder.Configuration.GetSection("BynderHost"));

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ExecutionContextState>();

builder.Services.AddWebDamHostServices();
builder.Services.AddS3HostServices();
builder.Services.AddBynderHostServices();
builder.Services.AddPlugins();

var host = builder.Build();

using var scope = host.Services.CreateScope();
var menu = scope.ServiceProvider.GetRequiredService<PluginMenuBuilder>();
var plugins = scope.ServiceProvider.GetRequiredService<IEnumerable<IPlugin>>()
    .OrderBy(x => x.Priority)
    .ToList();

menu.Create(plugins);
