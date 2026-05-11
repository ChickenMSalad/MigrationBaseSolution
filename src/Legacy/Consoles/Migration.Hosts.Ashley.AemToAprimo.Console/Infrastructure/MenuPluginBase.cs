using Microsoft.Extensions.Logging;
using SysConsole = global::System.Console;

namespace Migration.Hosts.Ashley.AemToAprimo.Console.Infrastructure;

public abstract class MenuPluginBase(ILogger logger) : IPlugin
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual int Priority => 100;

    protected abstract IReadOnlyList<MenuOption> GetOptions(CancellationToken cancellationToken);

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Launching {PluginName}", Name);
        var options = GetOptions(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            SysConsole.WriteLine();
            SysConsole.WriteLine(Name);
            SysConsole.WriteLine(new string('-', Name.Length));
            foreach (var option in options)
            {
                SysConsole.WriteLine($"{option.Key}: {option.Description}");
            }
            SysConsole.WriteLine("0: Back");
            SysConsole.Write("Enter your choice: ");
            var choice = SysConsole.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(choice))
            {
                continue;
            }
            if (choice == "0")
            {
                return;
            }
            var selected = options.FirstOrDefault(x => x.Key == choice);
            if (selected is null)
            {
                logger.LogWarning("Invalid choice {Choice}", choice);
                continue;
            }
            SysConsole.Write("Are you sure? (y/n): ");
            var confirmation = SysConsole.ReadLine()?.Trim().ToLowerInvariant();
            if (confirmation != "y")
            {
                logger.LogInformation("Operation canceled.");
                continue;
            }
            await selected.ExecuteAsync(cancellationToken);
            return;
        }
    }
}
