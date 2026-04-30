
using System.Collections.Generic;

namespace Migration.Connectors.Targets.Aprimo.Clients;

public interface IMetadataSerializer
{
    string ToJson(IDictionary<string, object> dictionary);
}
