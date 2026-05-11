using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Files
{
    public class ConsoleReaderService : IConsoleReaderService
    {
        public async Task<string> ReadInputAsync()
        {
            return await Task.Run(() => System.Console.ReadLine());
        }
    }
}
