# P9G Admin/API Control-Plane Deployment Readiness

Purpose: prepare the Admin API/control-plane surface for cloud deployment verification after the SQL store, Service Bus topology, worker roles, and OpenTelemetry wiring are in place.

This set does not mutate runtime code. It defines the deployment checks for the Admin API/control plane and validates that the repo still contains the required health, readiness, telemetry, and operational endpoint surfaces.

## Scope

P9G covers:

- Admin API deployment settings
- health endpoint verification
- readiness endpoint verification
- OpenTelemetry configuration alignment
- operational control-plane endpoint readiness
- Azure Monitor proof-of-life queries for the Admin/API host
- no production RunId override policy

## Required cloud configuration

The Admin/API host should use the same configuration conventions as the worker hosts:

- `MIGRATION_` environment variable prefix
- `ConnectionStrings:MigrationOperationalStore`
- `OpenTelemetry:EnableTracing`
- `OpenTelemetry:EnableAzureMonitorExporter`
- `OpenTelemetry:AzureMonitorConnectionString` or `APPLICATIONINSIGHTS_CONNECTION_STRING`

## Deployment order

Deploy the Admin/API control plane after:

1. Azure SQL operational store is reachable.
2. Service Bus topology is configured.
3. Worker role settings are staged.
4. Azure Monitor / Application Insights connection string is available.

Before enabling real migration execution, verify:

- `/health/live` returns live.
- `/health/ready` returns ready only when the operational dependencies are healthy.
- telemetry/correlation endpoint reports the expected mode.
- operational SQL backbone endpoints can reach the operational store.

## Success criteria

P9G is successful when:

- Admin/API project contains cloud configuration loading with `MIGRATION_`.
- Admin/API has operational health/readiness endpoint surfaces.
- Admin/API has telemetry/correlation endpoint surfaces.
- Admin/API has operational SQL backbone/control-plane endpoint surfaces.
- Admin/API deployment template contains SQL, OTEL, Azure Monitor, and health probe settings.
- Build remains clean.

## Production rules

Do not configure a production RunId override.

Run discovery and operational work execution must be driven by the SQL operational store and queue runtime, not a pinned RunId in production configuration.
