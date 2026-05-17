# Environment Configuration Templates

These templates are non-secret examples for local/dev/test/prod cloud promotion.

They are not automatically loaded by the application.

Use them as references when populating:

- `appsettings.Development.json`
- Azure App Service configuration
- container environment variables
- CI/CD variable groups
- future Bicep/Terraform outputs

## Files

- `local-dev.appsettings.example.json`
- `dev.appsettings.example.json`
- `test.appsettings.example.json`
- `prod.appsettings.example.json`

## Secret handling

Do not put secret values in these files.

Use:

- user-secrets for local development
- Azure Key Vault for cloud
- managed identity for Azure-hosted apps/workers

## Validation

After applying values to an environment, use:

- `GET /api/cloud/configuration-audit`
- `GET /api/cloud/readiness`
- `GET /api/cloud/deployment-profile`
