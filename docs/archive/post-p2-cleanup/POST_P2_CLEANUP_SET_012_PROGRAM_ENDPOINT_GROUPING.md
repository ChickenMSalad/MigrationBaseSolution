# Post-P2 Cleanup Set 012 — Program.cs Endpoint Grouping

## Purpose

This set reduces the remaining endpoint noise in `Migration.Admin.Api/Program.cs`.

It adds:

```text
src/Migration.Admin.Api/Registration/AdminApiEndpointStartupExtensions.cs
```

and replaces the long endpoint mapping blocks with:

```csharp
AdminApiEndpointStartupExtensions.MapMigrationAdminApiRouteGroupEndpoints(api);
AdminApiEndpointStartupExtensions.MapMigrationAdminApiAppLevelEndpoints(app);
```

## Why

After Set 011, endpoint files were organized into folders and cloud startup helpers were moved out of `Program.cs`, but `Program.cs` still had many route-group endpoint calls.

This set makes `Program.cs` read more like startup orchestration instead of endpoint inventory.

## Behavior

No route names or route paths are intentionally changed.

The app-level endpoints remain mapped on `app`, not on the `/api` route group.

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Then start Admin API and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured
```
