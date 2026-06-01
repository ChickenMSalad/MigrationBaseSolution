# Admin Web Capacity Forecast Client

The capacity forecast client has been added to the canonical Admin Web path.

- Canonical deployable UI: `src/Admin/Migration.Admin.Web`
- Historical feature-source only: `apps/migration-admin-ui`
- Page: `src/Admin/Migration.Admin.Web/src/pages/CapacityForecast.tsx`
- API client: `src/Admin/Migration.Admin.Web/src/api/capacityForecastApi.ts`
- Types: `src/Admin/Migration.Admin.Web/src/types/capacityForecast.ts`

This set intentionally avoids route patching because prior route scripts were fragile against current React source formatting.
