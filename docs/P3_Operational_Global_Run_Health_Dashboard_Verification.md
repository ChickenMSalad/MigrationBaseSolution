# P3 Operational Global Run Health Dashboard Verification

Verifies that the global run health dashboard aggregate matches its component endpoints.

## Component endpoints

```http
GET /api/operational/runs/health-summary
GET /api/operational/activity/dashboard?recentLimit=10&metricsSampleLimit=100
GET /api/operational/failures/analytics-dashboard?recentLimit=10&metricsSampleLimit=100
GET /api/operational/runs/health-dashboard?activityRecentLimit=10&metricsSampleLimit=100
```

## Run

```powershell
./scripts/operational-global-run-health-dashboard-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
