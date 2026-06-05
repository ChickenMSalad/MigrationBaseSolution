# P1 Set 015 — Telemetry + Correlation Contract

## Purpose

P1 Set 015 adds a safe telemetry/correlation contract for cloud operations.

This does not add Application Insights packages and does not change logging providers yet.

It establishes the headers and log properties that future cloud telemetry should consistently carry.

## Added files

- `src/Migration.Admin.Api/Contracts/TelemetryCorrelationContracts.cs`
- `src/Migration.Admin.Api/Endpoints/TelemetryCorrelationEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/telemetryCorrelation.ts`
- `docs/cloud-roadmap-cleanup/P1_SET_015_TELEMETRY_CORRELATION.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Only adds:

```csharp
api.MapTelemetryCorrelationEndpoints();
```

## New API route

```http
GET /api/cloud/telemetry/correlation
```

## Recommended headers

- `X-Correlation-Id`
- `X-Workspace-Id`
- `X-Tenant-Id`
- `X-Run-Id`

## Recommended log properties

- `correlationId`
- `requestId`
- `workspaceId`
- `tenantId`
- `runId`
- `projectId`
- `jobName`
- `queueMessageId`
- `leaseResource`
- `idempotencyKey`

## Validation

Run:

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Then start the Admin API and call:

```powershell
Invoke-RestMethod http://localhost:5173/api/cloud/telemetry/correlation
```

Optional header test:

```powershell
Invoke-RestMethod http://localhost:5173/api/cloud/telemetry/correlation -Headers @{
  "X-Correlation-Id" = "manual-test-001"
  "X-Workspace-Id" = "workspace-dev"
  "X-Tenant-Id" = "tenant-dev"
}
```
