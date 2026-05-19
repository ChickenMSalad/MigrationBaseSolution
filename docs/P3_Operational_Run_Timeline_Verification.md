# P3 Operational Run Timeline Verification

## Purpose

This set verifies that the operational run timeline endpoint is mapped, readable, internally consistent, and chronologically ordered.

## Endpoint verified

```http
GET /api/operational/runs/{runId}/timeline
```

## Checks

- route exists in `/api/system/endpoints`
- timeline returns the selected run id
- event count matches event array length
- at least one `RunCreated` event exists
- events are sorted by `OccurredAt`

## Run all checks

```powershell
./scripts/operational-run-timeline-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
