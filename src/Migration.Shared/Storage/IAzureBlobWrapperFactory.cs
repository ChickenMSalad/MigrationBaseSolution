using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Shared.Storage
{
    public interface IAzureBlobWrapperFactory
    {
        AzureBlobWrapperAsync Get(string name);
    }
}
