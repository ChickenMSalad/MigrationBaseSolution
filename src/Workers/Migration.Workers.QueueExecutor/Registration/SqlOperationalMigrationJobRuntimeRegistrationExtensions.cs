using Migration.ControlPlane.Registration;
using Migration.GenericRuntime.Registration;
using Migration.Infrastructure.Sql.Registration;
using Migration.Workers.QueueExecutor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Workers.QueueExecutor.Registration
{
    public static class SqlOperationalMigrationJobRuntimeRegistrationExtensions
    {

        public static IServiceCollection AddSqlOperationalMigrationJobRuntime(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddMigrationRuntime(configuration);
            services.AddMigrationControlPlane(configuration);
            services.AddSqlOperationalStore(configuration);

            Migration.Connectors.Registration.ConnectorModuleRegistrationExtensions
                .AddMigrationConnectorModules(services, configuration);

            services.AddSingleton<ProjectCredentialJobSettingsHydrator>();

            return services;
        }

    }
}
