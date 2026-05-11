using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Models
{
    public class MyRow
    {
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public decimal Column3 { get; set; }
    }
    public sealed class MyRowMap : ClassMap<MyRow>
    {
        public MyRowMap()
        {
            Map(m => m.Column1).Index(0);
            Map(m => m.Column2).Index(1);
            Map(m => m.Column3).Index(2);
        }
    }
}
