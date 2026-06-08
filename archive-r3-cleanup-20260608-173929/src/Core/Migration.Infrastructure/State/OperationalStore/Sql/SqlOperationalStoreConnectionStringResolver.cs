using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Infrastructure.State.OperationalStore.Sql
{
    public sealed class SqlOperationalStoreConnectionStringResolver
        : ISqlOperationalStoreConnectionStringResolver
    {
        private readonly IConfiguration _configuration;

        public SqlOperationalStoreConnectionStringResolver(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ResolveConnectionString()
        {
            var connectionString =
                _configuration.GetConnectionString("OperationalSql")
                ?? _configuration.GetConnectionString("OperationalStore")
                ?? _configuration["OperationalSql:ConnectionString"]
                ?? _configuration["OperationalStore:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Operational SQL connection string is not configured. Configure ConnectionStrings:OperationalSql.");
            }

            return connectionString;
        }
    }
}
