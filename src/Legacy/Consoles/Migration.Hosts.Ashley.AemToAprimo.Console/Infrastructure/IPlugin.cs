namespace Migration.Hosts.Ashley.AemToAprimo.Console.Infrastructure;

public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    int Priority { get; }
    Task ExecuteAsync(CancellationToken cancellationToken);
}
