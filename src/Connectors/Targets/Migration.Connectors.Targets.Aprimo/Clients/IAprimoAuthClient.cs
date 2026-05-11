using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Clients
{
    public interface IAprimoAuthClient
    {
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    }
}
