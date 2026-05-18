# Post-P2 Cleanup Set 008 — Program.cs Local Helper Consolidation

## Purpose

This set consolidates the large P2 cloud/control-plane service and endpoint blocks in `Migration.Admin.Api/Program.cs`.

It does not create new extension classes, use reflection, or map routes twice.

Instead, it reads the exact known-good blocks from the current `Program.cs`, moves them into local helper functions in the same file, and replaces the long blocks with:

```csharp
AddMigrationAdminApiCloudServices(builder.Services, builder.Configuration);
MapMigrationAdminApiCloudEndpoints(api);
```

## Why this is safer

This preserves the exact calls already present in the known-good `Program.cs`.

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Then start Admin API and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured
```
