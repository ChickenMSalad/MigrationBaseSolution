using System.Threading;
using System.Threading.Tasks;

using Migration.Connectors.Sources.WebDam.Models;

namespace Migration.Connectors.Sources.WebDam.Clients;

public interface IWebDamTokenStore
{
    Task<WebDamTokenState?> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(WebDamTokenState token, CancellationToken cancellationToken = default);
}
