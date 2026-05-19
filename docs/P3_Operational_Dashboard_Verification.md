# P3 Operational Dashboard Verification

## Purpose

This set verifies that the operational dashboard endpoints are mapped and consistent with their source endpoints.

## Routes verified

```text
/api/operational/dispatcher/dashboard
/api/operational/runs/{runId:guid}/dashboard
```

## Consistency checks

### Run dashboard

Compares run dashboard against:

```text
/api/operational/runs/{runId}/status-projection
/api/operational/runs/{runId}/control-state
```

### Dispatcher dashboard

Compares dispatcher dashboard against:

```text
/api/operational/dispatcher/status
/api/operational/dispatcher/diagnostics
/api/operational/dispatcher/executions/metrics
```

## Run all checks

```powershell
./scripts/operational-dashboard-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
