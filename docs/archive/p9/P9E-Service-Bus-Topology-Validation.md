# P9E - Service Bus Topology Validation

Purpose: validate the Azure Service Bus topology before cloud dispatcher/executor proof-of-life execution.

This set does not change runtime code. It defines the minimum Service Bus validation surface needed before running distributed execution in Azure.

## Required runtime roles

- SQL operational worker / queue executor
- Service Bus dispatcher
- Service Bus executor
- Azure SQL operational store
- Azure Service Bus namespace and queue
- Azure Monitor / Application Insights for trace verification

## Required configuration keys

Use `MIGRATION_` environment variables in deployed hosts.

### Common

- `MIGRATION_ConnectionStrings__MigrationOperationalStore`
- `MIGRATION_OpenTelemetry__EnableTracing`
- `MIGRATION_OpenTelemetry__EnableAzureMonitorExporter`
- `MIGRATION_OpenTelemetry__AzureMonitorConnectionString`
- `MIGRATION_OpenTelemetry__TraceSamplingRatio`

### Service Bus dispatcher

- `MIGRATION_ServiceBusDispatcher__Enabled`
- `MIGRATION_ServiceBusDispatcher__QueueName`
- `MIGRATION_ServiceBusDispatcher__BatchSize`
- `MIGRATION_ServiceBusDispatcher__PollingIntervalSeconds`
- `MIGRATION_ServiceBusDispatcher__WorkerId`

### Service Bus executor

- `MIGRATION_ServiceBusExecutor__Enabled`
- `MIGRATION_ServiceBusExecutor__QueueName`
- `MIGRATION_ServiceBusExecutor__MaxConcurrentCalls`
- `MIGRATION_ServiceBusExecutor__WorkerId`

### Service Bus connection

Use the repo-native option names already present in the dispatcher/executor hosts. Prefer managed identity in Azure when the current host implementation supports it. For local proof, connection string usage is acceptable.

Typical setting names to verify in the existing host code/options:

- `ServiceBus:ConnectionString`
- `ServiceBusDispatcher:ConnectionString`
- `ServiceBusExecutor:ConnectionString`
- `AzureWebJobsServiceBus`

Do not invent a new setting name until the current option classes are confirmed.

## Validation order

1. Confirm Azure SQL operational store is reachable.
2. Confirm Service Bus namespace exists.
3. Confirm queue exists.
4. Confirm dispatcher host can start with Service Bus configuration.
5. Confirm executor host can start with Service Bus configuration.
6. Confirm dispatcher sends one message for a runnable work item.
7. Confirm executor receives and settles the message.
8. Confirm SQL work item status is updated.
9. Confirm Azure Monitor traces include:
   - `ServiceBusDispatch`
   - `ServiceBusWorkItemExecution`
   - `SqlQueueWorkItemExecution`
10. Confirm failed messages can be dead-lettered and inspected.

## Success criteria

P9E is successful when the target cloud environment has a named Service Bus queue and the repository-side configuration/docs clearly identify the settings required by dispatcher and executor runtime hosts.

P9E does not require executing a migration. That belongs to the next proof set.
