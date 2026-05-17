# Backend Auth Scaffold

## Current behavior

Authentication is not enforced yet.

The Admin API now has:

- centralized auth options
- a non-enforcing auth state middleware
- a future registration location for JWT Bearer / Entra ID

## Future implementation

A later set should add:

- Microsoft.Identity.Web or JwtBearer auth package
- authentication middleware
- authorization policies
- route-level policy enforcement
- frontend token acquisition

## Config keys

```json
{
  "Cloud": {
    "AuthMode": "entraId",
    "RequiresAuth": true
  },
  "Auth": {
    "Authority": "https://login.microsoftonline.com/<tenant-id>/v2.0",
    "Audience": "api://<admin-api-client-id>",
    "TenantId": "<tenant-id>"
  }
}
```
