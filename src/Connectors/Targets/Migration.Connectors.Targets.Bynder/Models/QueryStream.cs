using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bynder.Sdk.Query.Upload;

namespace Migration.Connectors.Targets.Bynder.Models
{
    public class QueryStream
    {
        public UploadQuery Query { get; set; }

        public Stream Stream { get; set; }

        public Dictionary<string, string> RowData { get; set; }
    }
}
