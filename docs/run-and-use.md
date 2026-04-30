# Run and use the consolidated migration base solution

## What changed
This solution now keeps only the **new base migration projects** inside `MigrationBaseSolution.sln`.

The old solutions are no longer loaded as first-class projects in Visual Studio. Their reusable code has been
folded into the new `src/` projects as imported source:

- `src/Migration.Shared` - shared file helpers and Azure storage wrappers
- `src/Migration.Connectors.Sources.WebDam/Imported`
- `src/Migration.Connectors.Targets.Bynder/Imported`
- `src/Migration.Connectors.Sources.Aem/Imported`
- `src/Migration.Connectors.Targets.Aprimo/Imported`
- `src/Migration.Connectors.Sources.Sitecore/Imported`
- `src/Migration.Manifest.Sql/Imported`
- `src/Migration.Manifest.Sqlite/Imported`

## Configuration model

The CLI loads configuration from:

1. `appsettings.json`
2. `appsettings.Local.json`
3. `secrets.json`
4. environment variables prefixed with `MIGRATION_`

Put your real values in `secrets.json` at the repo root.

Example:
```json
{
  "ConnectionStrings": {
    "ManifestDb": "Server=...;Database=...;Trusted_Connection=False;User Id=...;Password=..."
  },
  "Settings": {
    "WebDam:BaseUrl": "https://apiv2.webdamdb.com/",
    "WebDam:ClientId": "your-client-id",
    "WebDam:ClientSecret": "your-client-secret",
    "Bynder:BaseUrl": "https://your-brand.bynder.com/",
    "Bynder:ClientId": "your-bynder-client-id",
    "Bynder:ClientSecret": "your-bynder-client-secret",
    "Bynder:Scopes": "asset:read asset:write",
    "Aem:BaseUrl": "https://your-aem-host/",
    "Aem:ClientId": "your-aem-client-id",
    "Aem:DeveloperTokenOrUser": "your-token-or-user",
    "Aem:Password": "optional-password",
    "Aprimo:BaseUrl": "https://your-aprimo-host/",
    "Aprimo:ClientId": "your-aprimo-client-id",
    "Aprimo:ClientSecret": "your-aprimo-client-secret"
  }
}
```

## Job files

Each job file describes the migration lane to execute:

```json
{
  "jobName": "webdam-to-bynder",
  "sourceType": "WebDam",
  "targetType": "Bynder",
  "manifestType": "Csv",
  "manifestPath": "profiles/manifests/webdam-sample.csv",
  "mappingProfilePath": "profiles/mappings/webdam-to-bynder.default.json",
  "dryRun": true,
  "parallelism": 1
}
```

## Running the base CLI

```bash
dotnet restore
dotnet build MigrationBaseSolution.sln
dotnet run --project src/Migration.Runner.Cli -- profiles/jobs/webdam-to-bynder.sample.json
```

## How to use the imported code

The imported source is intentionally grouped by capability, not by old client solution:

- Use `Imported/WebDam` for WebDam auth, DTOs, and API export logic
- Use `Imported/Api` + `Imported/Bynder` for Bynder OAuth and asset upload/update logic
- Use `Imported/Services` + `Imported/Models` in the AEM and Aprimo projects for the Ashley migration lane
- Use `Imported/ContentHub` and `Imported/Node` in Sitecore source work for the Crocs lane
- Use `Migration.Shared` for reusable storage wrappers and file helpers
- Use `Migration.Manifest.Sql` and `Migration.Manifest.Sqlite` imported folders for SQL-backed state/manifest helpers

## Recommended next coding step

The solution is now organized so you can port each working migration lane into the new abstractions:

1. make the WebDam source connector call the imported `WebDamApiClient`
2. make the Bynder target connector call the imported `BynderRestClient` / Bynder services
3. move Ashley mapping/upsert logic into Aprimo target transforms
4. move Ashley SQL/SQLite state pieces into the job state store
5. replace manifest/profile stubs with lane-specific adapters

This keeps one solution and one architecture, while still preserving the implementation work you already paid for.
