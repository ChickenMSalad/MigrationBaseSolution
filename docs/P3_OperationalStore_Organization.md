# P3 OperationalStore Organization

## Goal

Keep `Migration.Admin.Api/OperationalStore` organized by operational concern instead of accumulating dozens of files directly under one folder.

## Target layout

```text
OperationalStore/
  Diagnostics/
  Dispatcher/
  Metrics/
  Mirror/
  Retention/
  Runs/
    Control/
    Finalization/
    Projection/
    Read/
    Reconciliation/
  Sql/
    Scripts/
  WorkItems/
```

## Namespace choice

This cleanup intentionally keeps the existing namespace:

```csharp
namespace Migration.Admin.Api.OperationalStore;
```

That makes this a low-risk folder-only refactor. No `using` changes should be required.

## SQL file rule

Loose `.sql` files should not live directly under:

```text
src/Migration.Admin.Api/OperationalStore
```

They should live under:

```text
src/Migration.Admin.Api/OperationalStore/Sql/Scripts
```

## Apply

```powershell
./patches/P3_Organize_OperationalStore_Folders.ps1
```

## Verify

```powershell
./patches/P3_Verify_OperationalStore_Folder_Cleanup.ps1
```

Then run:

```powershell
dotnet build
./scripts/admin-api-endpoint-map-smoke-test.ps1 -BaseUrl "https://localhost:55436"
./scripts/operational-metrics-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
