namespace Migration.Hosts.Ashley.AemToAprimo.Console.Infrastructure;

public sealed record MenuOption(string Key, string Description, Func<CancellationToken, Task> ExecuteAsync);
