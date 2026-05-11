using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Migration.Hosts.Crocs.SitecoreToBynder.Functions.Registration;
using Migration.Shared.Extensions;
using Microsoft.Extensions.Hosting;

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

builder.Services.AddCrocsSitecoreToBynderFunctions(builder.Configuration);
builder.Build().Run();
