namespace Migration.Hosts.Crocs.SitecoreToBynder.Console.Infrastructure;

public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    int Priority { get; }
    Task ExecuteAsync(CancellationToken cancellationToken);
}
