# P3 Operational Global Run Health Snapshot Verification

Verifies the global run health snapshot aggregate against its component endpoints.

## Component endpoints

```http
GET /api/operational/runs/health-summary
GET /api/operational/activity/recent?limit=10
GET /api/operational/failures/dashboard?recentLimit=10&metricsSampleLimit=100
GET /api/operational/runs/health-snapshot?recentLimit=10&metricsSampleLimit=100
```

## Run

```powershell
./scripts/operational-global-run-health-snapshot-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
