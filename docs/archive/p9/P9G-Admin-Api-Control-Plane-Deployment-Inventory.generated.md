# P9G Admin/API Control-Plane Deployment Inventory

GeneratedUtc: 2026-05-25T02:14:39.9389756+00:00

This inventory verifies repository-side Admin/API control-plane deployment readiness before cloud deployment proof.

## docs\p9\P9G-Admin-Api-Control-Plane-Deployment-Readiness.md

Present.
- Contains: Admin API
- Contains: health
- Contains: readiness
- Contains: Do not configure a production RunId override

## config\templates\p9g-admin-api-control-plane-settings.template.json

Present.
- Contains: MigrationOperationalStore
- Contains: OpenTelemetry
- Contains: AzureMonitorConnectionString
- Contains: /health/live
- Contains: /health/ready

## src\Core\Migration.Admin.Api\Configuration\AdminApiConfigurationExtensions.cs

Present.
- Contains: AddEnvironmentVariables(prefix: "MIGRATION_")

## src\Core\Migration.Admin.Api\Contracts\TelemetryCorrelationContracts.cs

Present.
- Contains: OpenTelemetry
- Contains: ApplicationInsights
- Contains: X-Correlation-Id

## src\Core\Migration.Admin.Api\Endpoints\Telemetry\TelemetryCorrelationEndpointExtensions.cs

Present.
- Contains: ApplicationInsights:ConnectionString
- Contains: APPLICATIONINSIGHTS_CONNECTION_STRING
- Contains: OpenTelemetry

## Recommended next checks

- Deploy the Admin/API control plane after SQL and Service Bus validation.
- Verify /health/live and /health/ready.
- Verify telemetry/correlation endpoint output.
- Confirm no production RunId override is configured.
