using Microsoft.Extensions.Logging;
using SysConsole = global::System.Console;

namespace Migration.Hosts.Ashley.AemToAprimo.Console.Infrastructure;

public sealed class ConsoleHost(IEnumerable<IPlugin> plugins, ILogger<ConsoleHost> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var ordered = plugins.OrderBy(p => p.Priority).ThenBy(p => p.Name).ToList();

        while (!cancellationToken.IsCancellationRequested)
        {
            SysConsole.WriteLine();
            SysConsole.WriteLine("Ashley AEM to Aprimo Console");
            SysConsole.WriteLine(new string('=', 36));

            for (var i = 0; i < ordered.Count; i++)
            {
                SysConsole.WriteLine($"{i + 1}. {ordered[i].Name} - {ordered[i].Description}");
            }

            SysConsole.WriteLine("0. Exit");
            SysConsole.Write("Choose a plugin: ");
            var input = SysConsole.ReadLine();

            if (string.Equals(input, "0", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!int.TryParse(input, out var idx) || idx < 1 || idx > ordered.Count)
            {
                logger.LogWarning("Invalid selection: {Input}", input);
                continue;
            }

            var plugin = ordered[idx - 1];
            try
            {
                await plugin.ExecuteAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Operation canceled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Plugin {PluginName} failed.", plugin.Name);
            }
        }
    }
}
