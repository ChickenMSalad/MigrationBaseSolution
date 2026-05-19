# P3 Operational Admin API Endpoint Organization

## Goal

Keep `Migration.Admin.Api` endpoint files grouped by feature area instead of allowing root-level endpoint sprawl under:

```text
src/Migration.Admin.Api/Endpoints
```

## Operational endpoint layout

Operational endpoint files should live under:

```text
src/Migration.Admin.Api/Endpoints/Operational
```

Recommended subfolders:

```text
Operational/Dispatch
Operational/Diagnostics
Operational/Runs
Operational/WorkItems
```

## Current organization

### Dispatch

```text
Endpoints/Operational/Dispatch/OperationalDispatchEndpointExtensions.cs
```

### Diagnostics

```text
Endpoints/Operational/Diagnostics/OperationalHealthEndpointExtensions.cs
Endpoints/Operational/Diagnostics/OperationalMirrorDiagnosticsEndpointExtensions.cs
Endpoints/Operational/Diagnostics/OperationalSqlSchemaDiagnosticsEndpointExtensions.cs
Endpoints/Operational/Diagnostics/OperationalMetricsEndpointExtensions.cs
```

### Runs

```text
Endpoints/Operational/Runs/OperationalMirrorReadEndpointExtensions.cs
Endpoints/Operational/Runs/OperationalRunStatusProjectionEndpointExtensions.cs
Endpoints/Operational/Runs/OperationalRunControlEndpointExtensions.cs
Endpoints/Operational/Runs/OperationalRunStatusReconciliationEndpointExtensions.cs
```

### WorkItems

```text
Endpoints/Operational/WorkItems/OperationalWorkItemLeaseEndpointExtensions.cs
Endpoints/Operational/WorkItems/OperationalWorkItemLeaseExpirationEndpointExtensions.cs
```

## Namespace decision

For this cleanup set, namespaces remain:

```csharp
namespace Migration.Admin.Api.Endpoints;
```

That keeps the refactor low-risk and avoids touching endpoint startup imports.

Folder cleanup first, namespace cleanup later if desired.

## Program.cs rule

`Program.cs` should stay high-level.

Preferred pattern:

```csharp
var api = app.MapGroup("/api");

AdminApiEndpointStartupExtensions.MapMigrationAdminApiRouteGroupEndpoints(api);
AdminApiEndpointStartupExtensions.MapMigrationAdminApiAppLevelEndpoints(app);
```

Avoid adding operational route-group mappings directly in `Program.cs`.

The only currently acceptable direct operational app-level mapping is:

```csharp
app.MapOperationalHealthEndpoints();
```

because that endpoint was introduced as app-level health/diagnostics and may not be route-group-relative.

## Guard scripts

Run:

```powershell
./patches/P3_Set_061_Verify_Admin_Api_Endpoint_Folder_Cleanup.ps1
./patches/P3_Set_061_Check_Program_For_Adhoc_Operational_Mappings.ps1
```

These are intended as lightweight local hygiene checks before adding more P3 endpoint sets.
