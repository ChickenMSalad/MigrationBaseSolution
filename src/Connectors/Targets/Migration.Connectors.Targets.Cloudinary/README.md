# Migration.Connectors.Targets.Cloudinary

Cloudinary target connector and CSV-manifest migration services for `MigrationBaseSolution`.

## What this project contains

- `CloudinaryTargetConnector` for the generic `Migration.Runner.Cli` flow.
- Cloudinary upload service built around the .NET SDK.
- Cloudinary admin helper client for metadata-field lookup, asset existence checks, and delete helpers.
- Mapping/profile loader for CSV -> Cloudinary payload translation.
- Structured metadata resolver that translates business labels to Cloudinary datasource `external_id` values.

## Configuration sections

- `Cloudinary`
- `CloudinaryCsvMigration`

The menu-driven console host is in:

- `Migration.Hosts.Cloudinary.CsvToCloudinary.Console`
