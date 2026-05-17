# P2 Set 036 — Telemetry Provider Contracts

## Purpose

P2 Set 036 begins the telemetry implementation slice.

It adds telemetry contracts, a safe in-memory sink, API diagnostics, and smoke validation.

## Added files

- `src/Migration.ControlPlane/Telemetry/TelemetryContracts.cs`
- `src/Migration.ControlPlane/Telemetry/ITelemetrySink.cs`
- `src/Migration.ControlPlane/Telemetry/InMemoryTelemetrySink.cs`
- `src/Migration.ControlPlane/Telemetry/TelemetryEventFactory.cs`
- `src/Migration.ControlPlane/Telemetry/TelemetryRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/TelemetrySinkEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/telemetrySink.ts`
- `tools/test/smoke-telemetry-sink.ps1`
- `tools/test/smoke-telemetry-sink.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_036_TELEMETRY_PROVIDER_CONTRACTS.md`

## Program.cs additions

```csharp
using Migration.ControlPlane.Telemetry;

builder.Services.AddTelemetrySink(builder.Configuration);
api.MapTelemetrySinkEndpoints();
```

## New API routes

```http
GET /api/cloud/telemetry/provider
POST /api/cloud/telemetry/probe
GET /api/cloud/telemetry/recent
```
