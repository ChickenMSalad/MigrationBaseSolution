
using System.Collections.Generic;

namespace Migration.Connectors.Sources.Aem.Clients;

public interface IMetadataSerializer
{
    string ToJson(IDictionary<string, object> dictionary);
}
