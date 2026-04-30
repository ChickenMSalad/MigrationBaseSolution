# Migration.Hosts.Cloudinary.CsvToCloudinary.Console

Menu-driven console host for the Cloudinary CSV manifest migration flow.

## Configuration

Set credentials in user secrets or environment variables under the `Cloudinary` section:

- `Cloudinary:CloudName`
- `Cloudinary:ApiKey`
- `Cloudinary:ApiSecret`

Point the console to:

- `CloudinaryCsvMigration:ManifestPath`
- `CloudinaryCsvMigration:MappingPath`
- `CloudinaryCsvMigration:OutputRoot`

## Menu operations

1. Run migration
2. Preflight check
3. List metadata fields
4. Audit missing assets
5. Detect duplicate manifest public IDs
6. Delete assets from manifest
