namespace Migration.Connectors.Targets.Bynder.Clients;

public interface IMetapropertyOptionBuilder
{
    IList<string> this[string name] { get; set; }
    IDictionary<string, IList<string>> ToMetapropertyOptions();
}
