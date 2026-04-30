using static System.Console;

namespace Migration.Hosts.Ashley.AemToAprimo.Console.Infrastructure;

/// <summary>
/// Top-level console menu for the Ashley AEM → Aprimo host. This mirrors the
/// PluginMenuBuilder used by the Crocs (Sitecore→Bynder) and WebDam→Bynder
/// hosts so every host in the solution navigates the same way.
/// </summary>
internal sealed class PluginMenuBuilder
{
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    private const string Indent = "  ";

    static PluginMenuBuilder()
    {
        CancelKeyPress += (_, e) =>
        {
            CancellationTokenSource.Cancel();
            e.Cancel = true;
        };
    }

    public void Create(IReadOnlyList<IPlugin> plugins)
    {
        while (true)
        {
            Clear();
            WriteHeader();
            WritePlugins(plugins);

            var input = ReadLine()?.Trim();
            if (string.Equals(input, "x", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!int.TryParse(input, out var selection) || selection < 1 || selection > plugins.Count)
            {
                continue;
            }

            var plugin = plugins[selection - 1];
            Execute(plugin).GetAwaiter().GetResult();

            WriteLine();
            WriteLine("Press any key to return to the main menu...");
            ReadKey(true);
        }
    }

    private static void WriteHeader()
    {
        WriteLine();
        WriteLine($"{Indent}=====================================================");
        WriteLine($"{Indent}|   Ashley AEM -> Aprimo Migration Console          |");
        WriteLine($"{Indent}=====================================================");
        WriteLine();
    }

    private static void WritePlugins(IEnumerable<IPlugin> plugins)
    {
        WriteLine($"{Indent}Available tools:");
        WriteLine();

        var index = 1;
        foreach (var plugin in plugins)
        {
            WriteLine($"{Indent}{index++}. {plugin.Name} - {plugin.Description}");
        }

        WriteLine();
        Write($"{Indent}Enter a number or 'x' to exit: ");
    }

    private static async Task Execute(IPlugin plugin)
    {
        try
        {
            Clear();
            WriteLine();
            WriteLine("=====================================================");
            WriteLine($"  Executing {plugin.Name}...");
            WriteLine("  Press Ctrl+C to cancel.");
            WriteLine("=====================================================");
            WriteLine();

            var start = DateTime.Now;
            await plugin.ExecuteAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            var elapsed = DateTime.Now - start;

            WriteLine();
            WriteLine($"Completed in {elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s.");
        }
        catch (OperationCanceledException)
        {
            WriteLine();
            WriteLine("Operation canceled.");
        }
    }
}
