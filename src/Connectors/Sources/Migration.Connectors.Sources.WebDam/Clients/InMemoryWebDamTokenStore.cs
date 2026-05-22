using System.Threading;
using System.Threading.Tasks;

using Migration.Connectors.Sources.WebDam.Models;

namespace Migration.Connectors.Sources.WebDam.Clients;

public sealed class InMemoryWebDamTokenStore : IWebDamTokenStore
{
    private WebDamTokenState? _token;

    public Task<WebDamTokenState?> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_token);

    public Task SaveAsync(WebDamTokenState token, CancellationToken cancellationToken = default)
    {
        _token = token;
        return Task.CompletedTask;
    }
}
