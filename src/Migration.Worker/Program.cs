using Migration.Infrastructure.Runtime.Composition;
using Migration.Infrastructure.Runtime.Hosted;
using Migration.Infrastructure.Runtime.SqlServer;
using Migration.Worker;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddP7SqlOperationalRuntime(
    builder.Configuration,
    static (provider, cancellationToken) => SqlConnectionFactory.OpenAsync(provider, cancellationToken),
    static provider => provider.GetRequiredService<SqlOperationalWorkerExecutor>().ExecuteAsync);

builder.Services.AddSingleton<SqlOperationalWorkerExecutor>();
builder.Services.AddHostedService<SqlOperationalStartupProbeHostedService>();

await builder.Build().RunAsync().ConfigureAwait(false);
