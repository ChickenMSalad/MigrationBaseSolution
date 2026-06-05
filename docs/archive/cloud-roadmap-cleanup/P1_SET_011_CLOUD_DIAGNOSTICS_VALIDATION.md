# P1 Set 011 — Cloud Diagnostics Validation

## Purpose

P1 Set 011 adds local validation tooling for the cloud planning/readiness endpoints introduced in P1 Sets 001–010.

This set does not change application behavior.

## Added files

- `tools/cloud/validate-cloud-diagnostics.ps1`
- `tools/cloud/validate-cloud-diagnostics.cmd`
- `docs/cloud-roadmap-cleanup/P1_SET_011_CLOUD_DIAGNOSTICS_VALIDATION.md`

## What it validates

The script checks:

- environment template files exist
- environment template JSON is valid
- cloud diagnostic endpoints are reachable
- each endpoint returns expected high-level properties

## Usage

Start the Admin API first:

```powershell
dotnet run --project .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -BaseUrl http://localhost:5173
```

If you only want to validate template files:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -SkipHttp
```

## Why this matters

P1 has introduced a growing cloud diagnostic surface:

- environment
- workspace context
- workspace storage plan
- credential provider plan
- artifact storage plan
- queue provider plan
- deployment profile
- configuration audit
- readiness summary

This script gives us a repeatable sanity check before we begin higher-impact work such as real Azure Blob storage, Key Vault resolution, authentication, and CI/CD.
