using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Shared.Storage
{
    public class AzureConfiguration
    {
        public string ConnectionString { get; set; }

        public string AssetsContainer { get; set; }

        public string MetadataContainer { get; set; }

        public string LogContainer { get; set; }

    }
}
