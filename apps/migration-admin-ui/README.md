# Migration Admin UI

First operator UI shell for the SQL-backed MigrationBaseSolution runtime.

## Local run

```powershell
cd apps/migration-admin-ui
copy .env.example .env.local
npm install
npm run dev
```

Set `VITE_ADMIN_API_BASE_URL` in `.env.local` to the running `Migration.Admin.Api` base URL.

## Purpose

This shell intentionally starts small:

- Admin API endpoint discovery
- SQL backbone health and summary probes
- runtime readiness probes
- queue/run/worker visibility placeholders

It does not mutate the .NET solution and does not introduce .NET package dependencies.
