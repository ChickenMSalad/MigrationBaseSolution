using static System.Console;

namespace Migration.Hosts.Cloudinary.CsvToCloudinary.Console.Infrastructure;

internal sealed class PluginMenuBuilder
{
    private static readonly CancellationTokenSource CancellationTokenSource = new();

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
            WriteLine();
            WriteLine("==============================================================");
            WriteLine("  Cloudinary CSV -> Cloudinary Migration Console");
            WriteLine("==============================================================");
            WriteLine();

            for (var i = 0; i < plugins.Count; i++)
            {
                WriteLine($"  {i + 1}. {plugins[i].Name} - {plugins[i].Description}");
            }

            WriteLine();
            Write("  Enter a number or 'x' to exit: ");
            var input = ReadLine()?.Trim();

            if (string.Equals(input, "x", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!int.TryParse(input, out var selected) || selected < 1 || selected > plugins.Count)
            {
                continue;
            }

            var plugin = plugins[selected - 1];
            try
            {
                Clear();
                WriteLine();
                WriteLine($"Executing {plugin.Name}...");
                WriteLine("Press Ctrl+C to cancel.");
                WriteLine();
                plugin.ExecuteAsync(CancellationTokenSource.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                WriteLine();
                WriteLine("Operation canceled.");
            }

            WriteLine();
            WriteLine("Press any key to return to the main menu...");
            ReadKey(true);
        }
    }
}
