using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Migration.Hosts.Ashley.AemToAprimo.Functions.Registration;
using Migration.Shared.Extensions;
using Microsoft.Extensions.Hosting;

SQLitePCL.Batteries.Init();
StartupExtensions.ConfigureThirdPartyLicenses();

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton(_ =>
{
    var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
        ?? builder.Configuration["AzureWebJobsStorage"]
        ?? throw new InvalidOperationException("AzureWebJobsStorage is required.");
    return new BlobServiceClient(connectionString);
});

builder.Services.AddAshleyAemToAprimoFunctions(builder.Configuration);

builder.Build().Run();
