# Azure App Service Deployment Scaffold

## Current scope

P1 Set 024 adds an infrastructure scaffold for Azure App Service hosting of the Admin API.

## Not production-ready yet

Before production use, add:

- Key Vault
- managed identity role assignments
- auth configuration
- private networking if required
- Application Insights
- CI/CD deployment
- queue worker hosting
- storage lifecycle policies

## Validation path

1. Run Bicep what-if.
2. Review resources and names.
3. Deploy to dev resource group.
4. Deploy Admin API package.
5. Call:

```http
GET /health/live
GET /health/ready
GET /api/cloud/readiness
GET /api/cloud/configuration-audit
```
