# P2 Set 037 — Telemetry Event Writer

## Purpose

P2 Set 037 adds a telemetry event writer abstraction on top of the telemetry sink.

This gives cloud/queue/runtime components a simple normalized writer without coupling directly to sink details.

## Added files

- `src/Migration.ControlPlane/Telemetry/ITelemetryEventWriter.cs`
- `src/Migration.ControlPlane/Telemetry/TelemetryEventWriter.cs`
- `src/Migration.ControlPlane/Telemetry/TelemetryEventWriterRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/TelemetryEventWriterEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/telemetryEventWriter.ts`
- `tools/test/smoke-telemetry-event-writer.ps1`
- `tools/test/smoke-telemetry-event-writer.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_037_TELEMETRY_EVENT_WRITER.md`

## Program.cs additions

```csharp
builder.Services.AddTelemetryEventWriter();
api.MapTelemetryEventWriterEndpoints();
```

## New API route

```http
POST /api/cloud/telemetry/writer/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-telemetry-event-writer.ps1 -BaseUrl http://localhost:5173
```
