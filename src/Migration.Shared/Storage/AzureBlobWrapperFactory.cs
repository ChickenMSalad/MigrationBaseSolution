using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Migration.Shared.Storage
{

    public class AzureBlobWrapperFactory : IAzureBlobWrapperFactory
    {
        private readonly ConcurrentDictionary<string, AzureBlobWrapperAsync> _wrappers = new();
        private readonly ILogger<AzureBlobWrapperAsync> _logger;

        public AzureBlobWrapperFactory(IEnumerable<BlobStorageSettings> configs, ILogger<AzureBlobWrapperAsync> logger)
        {
            _logger = logger;

            foreach (var config in configs)
            {
                var wrapper = new AzureBlobWrapperAsync(
                    connectionString: config.ConnectionString,
                    containerName: config.ContainerName,
                    logger: _logger);

                _wrappers[config.Name] = wrapper;
            }
        }

        public AzureBlobWrapperAsync Get(string name)
        {
            if (_wrappers.TryGetValue(name, out var wrapper))
                return wrapper;

            throw new KeyNotFoundException($"No AzureBlobWrapper registered with name '{name}'");
        }
    }

}
