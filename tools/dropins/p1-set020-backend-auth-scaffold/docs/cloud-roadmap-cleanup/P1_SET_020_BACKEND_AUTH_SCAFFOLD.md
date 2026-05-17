# P1 Set 020 — Backend Auth Scaffold

## Purpose

P1 Set 020 adds backend authentication scaffolding without enforcing login yet.

This creates a safe place to centralize auth configuration before adding JWT Bearer / Entra ID packages and endpoint policies.

## Added files

- `src/Migration.Admin.Api/Authentication/AdminApiAuthenticationOptions.cs`
- `src/Migration.Admin.Api/Authentication/AdminApiAuthenticationStateMiddleware.cs`
- `src/Migration.Admin.Api/Registration/AdminApiAuthenticationServiceCollectionExtensions.cs`
- `docs/azure/BACKEND_AUTH_SCAFFOLD.md`
- `docs/cloud-roadmap-cleanup/P1_SET_020_BACKEND_AUTH_SCAFFOLD.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
builder.Services.AddMigrationAdminApiAuthentication(builder.Configuration, builder.Environment);
app.UseMigrationAdminApiAuthenticationState();
```

## Important

This set does not enforce auth.

It only:

- reads auth configuration
- registers `AdminApiAuthenticationOptions`
- annotates `HttpContext.Items` with auth enabled/required state

## Validation

Run:

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Then verify existing endpoints still work.
