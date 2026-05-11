using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bynder.Sdk.Model;

namespace Migration.Connectors.Sources.Sitecore.Models
{
    public class CustomSaveMediaResponse
    {
        public SaveMediaResponse SaveMediaResponse { get; set; }

        public Dictionary<string, string> RowData { get; set; }
    }
}
