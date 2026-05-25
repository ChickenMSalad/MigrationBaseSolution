# P9A - Operational Cloud Convergence Proof

This set starts P9 by shifting from platform foundation work to operational proof-of-life.

## Goal

Prove the operational migration runtime can be configured, deployed, started, and observed as a coherent cloud execution platform.

P9A does not mutate runtime code. It defines the proof checklist and validates that the repo already has the required surfaces from P8:

- SQL operational worker host
- Service Bus dispatcher worker
- Service Bus executor worker
- OpenTelemetry runtime registration
- Azure Monitor exporter registration
- runtime Activity usage
- health/readiness endpoints
- cloud/container topology documents

## Proof order

1. Build repo locally.
2. Confirm LocalDB operational store is healthy.
3. Confirm SQL operational worker starts and idles cleanly.
4. Confirm OpenTelemetry registration is active with local no-exporter settings.
5. Confirm Azure Monitor exporter can be enabled by configuration only.
6. Confirm Service Bus dispatcher/executor hosts build and start with required configuration.
7. Deploy cloud infrastructure only after local proof passes.
8. Run one no-op or smoke work item through SQL worker.
9. Run one distributed Service Bus dispatch/execution smoke.
10. Confirm traces by ActivitySource name: `Migration.Operational.Execution`.

## Local minimum settings

Use local user-secrets or environment variables for the runtime host being tested.

For local tracing without Azure export:

```json
{
  "OpenTelemetry": {
    "EnableTracing": true,
    "EnableAzureMonitorExporter": false,
    "TraceSamplingRatio": 1.0
  }
}
```

For Azure Monitor proof:

```json
{
  "OpenTelemetry": {
    "EnableTracing": true,
    "EnableAzureMonitorExporter": true,
    "AzureMonitorConnectionString": "InstrumentationKey=...;IngestionEndpoint=...",
    "TraceSamplingRatio": 1.0
  }
}
```

Prefer `APPLICATIONINSIGHTS_CONNECTION_STRING` or `MIGRATION_OpenTelemetry__AzureMonitorConnectionString` for deployed environments.

## Runtime hosts to verify first

- `src/Hosts/Migration.Hosts.SqlOperationalWorker`
- `src/Workers/Migration.Workers.ServiceBusDispatcher`
- `src/Workers/Migration.Workers.ServiceBusExecutor`

## Success criteria

P9A is successful when the repo can prove these surfaces exist:

- `AddOperationalOpenTelemetry` is wired into all three operational hosts.
- Activity usage exists in SQL worker, Service Bus dispatcher, and Service Bus executor.
- Azure Monitor exporter registration exists.
- cloud/container docs exist.
- health/readiness endpoint docs exist.

Actual cloud deployment happens in later P9 sets.
