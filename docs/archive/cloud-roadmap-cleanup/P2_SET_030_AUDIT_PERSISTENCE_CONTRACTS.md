# P2 Set 030 — Audit Persistence Contracts

## Purpose

P2 Set 030 begins the audit persistence implementation slice.

It adds audit persistence contracts, a safe in-memory provider, API diagnostics, and smoke validation.

This does not replace existing audit event contracts yet.

## Program.cs additions

```csharp
using Migration.ControlPlane.Audit;

builder.Services.AddAuditPersistence(builder.Configuration);
api.MapAuditPersistenceEndpoints();
```

## New API routes

```http
GET /api/cloud/audit/persistence/provider
POST /api/cloud/audit/persistence/probe
GET /api/cloud/audit/persistence/recent
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-audit-persistence.ps1 -BaseUrl http://localhost:5173
```
