# P3 Operational Global Failure Dashboard Verification

Verifies the global failure dashboard aggregate against its component endpoints.

## Component endpoints

```http
GET /api/operational/failures/recent?limit=10
GET /api/operational/failures/metrics?sampleLimit=100
GET /api/operational/failures/dashboard?recentLimit=10&metricsSampleLimit=100
```

## Run

```powershell
./scripts/operational-global-failure-dashboard-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
