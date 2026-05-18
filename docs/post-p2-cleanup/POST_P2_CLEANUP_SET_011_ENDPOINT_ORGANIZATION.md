# Post-P2 Cleanup Set 011 — Admin API Endpoint Organization

## Purpose

This set reduces `Migration.Admin.Api` clutter before P3.

It does two cleanup tasks:

1. Moves endpoint extension files under meaningful feature subfolders.
2. Moves the P2 cloud/control-plane startup helper functions out of `Program.cs` into a registration class.

## Endpoint folder organization

Endpoint files are moved under:

```text
src/Migration.Admin.Api/Endpoints/Cloud
src/Migration.Admin.Api/Endpoints/Queue
src/Migration.Admin.Api/Endpoints/Audit
src/Migration.Admin.Api/Endpoints/Telemetry
src/Migration.Admin.Api/Endpoints/Auth
src/Migration.Admin.Api/Endpoints/Operations
src/Migration.Admin.Api/Endpoints/Artifacts
src/Migration.Admin.Api/Endpoints/Projects
src/Migration.Admin.Api/Endpoints/Runs
src/Migration.Admin.Api/Endpoints/Workspace
src/Migration.Admin.Api/Endpoints/Connectors
src/Migration.Admin.Api/Endpoints/System
```

Namespaces are intentionally left unchanged.

Because SDK-style `.csproj` files include `**/*.cs` recursively by default, moving files into subfolders should not require project-file edits.

## Program.cs cleanup

If `Program.cs` contains local helper functions:

```csharp
static void AddMigrationAdminApiCloudServices(...)
static void MapMigrationAdminApiCloudEndpoints(...)
```

this set moves those helper bodies into:

```text
src/Migration.Admin.Api/Registration/AdminApiCloudStartupExtensions.cs
```

and changes the calls in `Program.cs` to:

```csharp
AdminApiCloudStartupExtensions.AddMigrationAdminApiCloudServices(builder.Services, builder.Configuration);
AdminApiCloudStartupExtensions.MapMigrationAdminApiCloudEndpoints(api);
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Then start Admin API and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured
```
