# P2 Set 002 — Blob Storage Provider Contracts

## Purpose

P2 Set 002 introduces provider contracts for binary/object storage.

No real storage provider is implemented yet.

## Added files

- `ICloudBinaryStorageProvider.cs`
- `CloudBinaryStorageProviderCapabilities.cs`
- `NullCloudBinaryStorageProvider.cs`
- `CloudBinaryStorageRegistrationExtensions.cs`
- `CloudStorageObjectReference.cs`

## Why this matters

Future P2 work needs a common abstraction for:

- artifact upload/download
- run checkpoint persistence
- manifest persistence
- audit/event persistence
- Azure Blob integration

## Current behavior

The registered provider intentionally throws if used.

This prevents accidental silent local-file fallback once cloud-backed persistence starts landing.

## Next likely steps

- Azure Blob provider implementation
- object metadata support
- blob leasing/checkpoint semantics
- signed URL generation
- upload/download APIs

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
builder.Services.AddCloudBinaryStorage();
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```
