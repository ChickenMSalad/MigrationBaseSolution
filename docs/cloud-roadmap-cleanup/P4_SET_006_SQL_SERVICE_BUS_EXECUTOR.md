# P4.6 — SQL Service Bus Executor

Adds the first Service Bus consumer worker for the SQL-backed work item queue.

## Project

```text
src/Workers/Migration.Workers.ServiceBusExecutor
```

## Purpose

The dispatcher introduced in P4.5 publishes SQL work item identifiers to Azure Service Bus. This executor consumes those messages, reloads the durable work item state from SQL, and transitions the work item to completed or failed.

Actual connector execution is intentionally represented by `IServiceBusWorkItemExecutor` so the worker can be wired and smoke-tested before binding real migration runtime execution into the queue pathway.

## Operational model

SQL remains the durable source of truth. Service Bus is a delivery signal, not the system of record.
