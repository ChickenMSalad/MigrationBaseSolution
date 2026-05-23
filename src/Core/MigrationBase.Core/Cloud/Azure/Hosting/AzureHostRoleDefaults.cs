namespace MigrationBase.Core.Cloud.Azure.Hosting;

/// <summary>
/// Provides the default role descriptors for the current MigrationBaseSolution Azure runtime shape.
/// </summary>
public static class AzureHostRoleDefaults
{
    public static IReadOnlyList<AzureHostRoleDescriptor> CreateDefaultDescriptors()
    {
        return new[]
        {
            new AzureHostRoleDescriptor
            {
                HostName = "admin-api",
                RoleKind = AzureHostRoleKind.AdminApi,
                DeploymentUnit = "Migration.Admin.Api",
                Description = "Administrative API surface for operational control-plane actions.",
                IsInteractive = true,
                PublishesOperationalEvents = true,
                Capabilities =
                {
                    new AzureHostWorkloadCapability
                    {
                        Name = "operational-control",
                        Description = "Reads and writes operational state through the SQL-backed control plane.",
                        RequiresSqlOperationalStore = true,
                        RequiresOperatorAccess = true
                    }
                }
            },
            new AzureHostRoleDescriptor
            {
                HostName = "operator-ui",
                RoleKind = AzureHostRoleKind.OperatorUi,
                DeploymentUnit = "Operator UI",
                Description = "Interactive operator experience for run monitoring and manual controls.",
                IsInteractive = true,
                Capabilities =
                {
                    new AzureHostWorkloadCapability
                    {
                        Name = "operator-experience",
                        Description = "Displays runtime state, approvals, policies, and execution history.",
                        RequiresOperatorAccess = true
                    }
                }
            },
            new AzureHostRoleDescriptor
            {
                HostName = "queue-executor",
                RoleKind = AzureHostRoleKind.QueueExecutor,
                DeploymentUnit = "Migration.Workers.QueueExecutor",
                Description = "Executes migration work items from queue-backed runtime orchestration.",
                ProcessesMigrationWork = true,
                PublishesOperationalEvents = true,
                Capabilities =
                {
                    new AzureHostWorkloadCapability
                    {
                        Name = "migration-work-execution",
                        Description = "Claims, executes, heartbeats, and completes migration work items.",
                        RequiresSqlOperationalStore = true,
                        RequiresQueueTopology = true,
                        RequiresArtifactStorage = true
                    }
                }
            },
            new AzureHostRoleDescriptor
            {
                HostName = "service-bus-dispatcher",
                RoleKind = AzureHostRoleKind.ServiceBusDispatcher,
                DeploymentUnit = "Migration.Workers.ServiceBusDispatcher",
                Description = "Dispatches admitted work into Service Bus-backed execution lanes.",
                ProcessesMigrationWork = false,
                PublishesOperationalEvents = true,
                Capabilities =
                {
                    new AzureHostWorkloadCapability
                    {
                        Name = "work-dispatch",
                        Description = "Publishes admitted work items to execution queues.",
                        RequiresSqlOperationalStore = true,
                        RequiresQueueTopology = true
                    }
                }
            },
            new AzureHostRoleDescriptor
            {
                HostName = "service-bus-executor",
                RoleKind = AzureHostRoleKind.ServiceBusExecutor,
                DeploymentUnit = "Migration.Workers.ServiceBusExecutor",
                Description = "Executes migration work received from Service Bus-backed queues.",
                ProcessesMigrationWork = true,
                PublishesOperationalEvents = true,
                Capabilities =
                {
                    new AzureHostWorkloadCapability
                    {
                        Name = "service-bus-work-execution",
                        Description = "Executes Service Bus-delivered migration work items.",
                        RequiresSqlOperationalStore = true,
                        RequiresQueueTopology = true,
                        RequiresArtifactStorage = true
                    }
                }
            },
            new AzureHostRoleDescriptor
            {
                HostName = "manifest-ingestion",
                RoleKind = AzureHostRoleKind.ManifestIngestion,
                DeploymentUnit = "Manifest ingestion pipeline",
                Description = "Ingests large migration manifests into SQL-first operational tables.",
                ProcessesMigrationWork = false,
                PublishesOperationalEvents = true,
                Capabilities =
                {
                    new AzureHostWorkloadCapability
                    {
                        Name = "manifest-ingestion",
                        Description = "Loads source manifests into durable SQL-backed execution state.",
                        RequiresSqlOperationalStore = true,
                        RequiresArtifactStorage = true
                    }
                }
            }
        };
    }
}
