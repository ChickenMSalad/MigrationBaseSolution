# Post-P2 Cleanup Set 011 Fix

## Purpose

Set 011 moved `Program.cs` helper methods into `AdminApiCloudStartupExtensions.cs`.

That class needs the same extension-method namespaces that were previously available in `Program.cs`.

This fix patches the file header with the required `using` directives.

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Then start Admin API and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured
```
