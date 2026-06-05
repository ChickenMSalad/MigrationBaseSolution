# P4.5 SQL Service Bus Dispatcher

This set introduces a conservative worker host that bridges the SQL durable work-item queue to Azure Service Bus.

## Intent

SQL remains the source of truth for durable work item state. Service Bus is used as the cloud execution transport for scalable workers.

## Added project

```text
src/Workers/Migration.Workers.ServiceBusDispatcher
```

## Runtime flow

1. Poll SQL for pending or ready work items.
2. Claim a small batch using SQL row locks and leases.
3. Publish one Service Bus message per claimed work item.
4. Mark the work item as dispatched in SQL.

## Notes

This is intentionally a dispatcher only. Actual migration execution from Service Bus messages belongs in the next worker set.
