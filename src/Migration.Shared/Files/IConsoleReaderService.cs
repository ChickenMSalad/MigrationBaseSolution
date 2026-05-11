using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Shared.Files
{
    public interface IConsoleReaderService
    {
        Task<string> ReadInputAsync();
    }
}
