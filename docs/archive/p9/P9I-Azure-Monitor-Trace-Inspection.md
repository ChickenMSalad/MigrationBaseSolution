# P9I - Azure Monitor Trace Inspection

Purpose: prove that cloud smoke execution emits observable traces for the operational runtime.

## Proof order

1. Confirm the target runtime hosts have OpenTelemetry enabled.
2. Confirm Azure Monitor exporter is enabled only for cloud proof environments.
3. Run the smoke execution from P9H.
4. Open the Application Insights / Log Analytics workspace connected to the target Application Insights resource.
5. Run the KQL from `scripts/kql/P9I-OperationalTraceInspection.kql`.
6. Confirm traces exist for `Migration.Operational.Execution`.
7. Confirm all three operational activity names appear:
   - `SqlQueueWorkItemExecution`
   - `ServiceBusDispatch`
   - `ServiceBusWorkItemExecution`
8. Confirm `RunId`, `WorkItemId`, execution result, and duration dimensions are present where available.
9. Confirm failures, if present, include error/result dimensions.
10. Capture query results/screenshots as deployment evidence.

## Required cloud settings

Use the established `MIGRATION_` configuration style for deployed hosts.

Required for each runtime host that should emit traces:

```text
MIGRATION_OpenTelemetry__EnableTracing=true
MIGRATION_OpenTelemetry__EnableAzureMonitorExporter=true
MIGRATION_OpenTelemetry__TraceSamplingRatio=1.0
MIGRATION_OpenTelemetry__AzureMonitorConnectionString=<application-insights-connection-string>
```

Equivalent fallback supported by the registration layer:

```text
APPLICATIONINSIGHTS_CONNECTION_STRING=<application-insights-connection-string>
```

## Runtime hosts expected to emit traces

- `Migration.Hosts.SqlOperationalWorker`
- `Migration.Workers.ServiceBusDispatcher`
- `Migration.Workers.ServiceBusExecutor`

## Success criteria

P9I is successful when:

- Azure Monitor contains traces for `Migration.Operational.Execution`.
- At least one trace exists for SQL queue execution.
- At least one trace exists for Service Bus dispatch when distributed dispatch is enabled.
- At least one trace exists for Service Bus execution when the executor consumes a message.
- Run/work-item correlation is visible in custom dimensions where emitted.
- Execution duration and result metadata are visible where emitted.

## Troubleshooting

If no traces appear:

1. Confirm the cloud host has the Azure Monitor connection string.
2. Confirm `EnableTracing=true`.
3. Confirm `EnableAzureMonitorExporter=true`.
4. Confirm the host restarted after configuration changed.
5. Confirm the smoke run actually executed at least one work item.
6. Confirm you are querying the correct Application Insights / workspace resource.
7. Expand the time window to 24 hours.

If only SQL worker traces appear:

- Confirm dispatcher and executor are deployed.
- Confirm dispatcher and executor are enabled.
- Confirm Service Bus topology is correct.
- Confirm work items are dispatchable and not already completed.

If traces appear without dimensions:

- Confirm the current commit includes P8.3D/P8.3D.3 runtime Activity usage changes.
- Confirm `OperationalExecutionActivity.SetExecutionResult` and `SetExecutionDuration` exist in the success and failure paths.
