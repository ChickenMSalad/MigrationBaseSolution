using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Shared.Workflows.AemToAprimo.Models
{
    public class ExecutionContextState
    {
        public Guid InstanceId { get; } = Guid.NewGuid();

        public DataTable SuccessTable { get; } = new DataTable("Success");
        public DataTable RetryTable { get; } = new DataTable("Retry");

        public List<string> LogOutput { get; } = new List<string>();

        public List<Dictionary<string, string>> Successes { get; }
            = new List<Dictionary<string, string>>();

        public List<Dictionary<string, string>> Failures { get; }
            = new List<Dictionary<string, string>>();
    }
}
