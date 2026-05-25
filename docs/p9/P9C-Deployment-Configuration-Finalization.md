# P9C Deployment Configuration Finalization

Purpose: finalize the concrete configuration contract for cloud proof-of-life deployment without mutating runtime code.

This set is intentionally operational and bounded. It does not add packages, SQL, runtime services, or repair scripts. It gives you one place to verify the environment variables/app settings that must exist before Azure SQL, Service Bus, worker hosts, and Azure Monitor are expected to run together.

## Runtime roles covered

- `Migration.Hosts.SqlOperationalWorker`
- `Migration.Workers.ServiceBusDispatcher`
- `Migration.Workers.ServiceBusExecutor`
- optional Admin/API control plane settings are listed, but not required for the first worker proof

## Required cloud configuration categories

### SQL operational store

Use one canonical connection string name:

```text
ConnectionStrings:MigrationOperationalStore
```

In Azure App Settings / Container Apps environment variables, prefer:

```text
MIGRATION_ConnectionStrings__MigrationOperationalStore
```

### SQL operational worker

```text
MIGRATION_SqlOperationalWorker__Enabled=true
MIGRATION_SqlOperationalWorker__PollingIntervalSeconds=5
MIGRATION_SqlOperationalWorker__BatchSize=10
MIGRATION_SqlOperationalWorker__LeaseSeconds=300
MIGRATION_SqlOperationalWorker__PartitionKey=default
MIGRATION_SqlOperationalWorker__CompleteNoOpWorkItems=true
```

Do not configure a production RunId override. The cloud worker should discover runnable runs from SQL.

### Service Bus dispatcher

```text
MIGRATION_ServiceBusDispatcher__Enabled=true
MIGRATION_ServiceBusDispatcher__QueueName=<queue-name>
MIGRATION_ServiceBusDispatcher__BatchSize=10
MIGRATION_ServiceBusDispatcher__PollingIntervalSeconds=5
```

Connection value depends on the existing dispatcher option names in the repo. If the worker already uses a different exact setting name, keep the repo-native option and record it in the generated inventory.

### Service Bus executor

```text
MIGRATION_ServiceBusExecutor__Enabled=true
MIGRATION_ServiceBusExecutor__QueueName=<queue-name>
MIGRATION_ServiceBusExecutor__MaxConcurrentCalls=4
MIGRATION_ServiceBusExecutor__RetryDelaySeconds=30
```

### OpenTelemetry / Azure Monitor

For local proof without Azure ingestion:

```text
MIGRATION_OpenTelemetry__EnableTracing=true
MIGRATION_OpenTelemetry__EnableAzureMonitorExporter=false
MIGRATION_OpenTelemetry__TraceSamplingRatio=1.0
```

For Azure Monitor proof:

```text
MIGRATION_OpenTelemetry__EnableTracing=true
MIGRATION_OpenTelemetry__EnableAzureMonitorExporter=true
MIGRATION_OpenTelemetry__AzureMonitorConnectionString=<application-insights-connection-string>
MIGRATION_OpenTelemetry__TraceSamplingRatio=1.0
```

The registration code also supports Application Insights style connection-string lookup if that was wired in the previous set:

```text
APPLICATIONINSIGHTS_CONNECTION_STRING=<application-insights-connection-string>
```

## P9C acceptance criteria

- runtime hosts still build
- `MIGRATION_` configuration provider exists in operational hosts
- OpenTelemetry registration remains wired to operational hosts
- cloud templates do not contain production RunId override settings
- generated inventory completes quickly and does not scan heavy folders
- next step can safely move to SQL operational store cloud validation

## Recommended next phase

P9D should validate the cloud SQL operational store: schema, compatibility scripts, readiness queries, and a tiny smoke run seed.
