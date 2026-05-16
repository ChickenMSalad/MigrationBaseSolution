using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Migration.Workers.QueueExecutor.Configuration;

public static class QueueExecutorConfigurationExtensions
{
    public static HostApplicationBuilder ConfigureMigrationQueueExecutorConfiguration(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.Sources.Clear();

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables(prefix: "MIGRATION_");

        return builder;
    }

    public static HostApplicationBuilder LogMigrationQueueExecutorConfiguration(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
        Console.WriteLine($"ControlPlane:StorageRoot = {builder.Configuration["ControlPlane:StorageRoot"]}");
        Console.WriteLine($"MigrationRunQueue:Provider = {builder.Configuration["MigrationRunQueue:Provider"]}");
        Console.WriteLine($"MigrationRunQueue:QueueName = {builder.Configuration["MigrationRunQueue:QueueName"]}");

        return builder;
    }
}
