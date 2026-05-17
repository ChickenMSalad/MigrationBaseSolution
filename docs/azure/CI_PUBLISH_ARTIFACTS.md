# CI Publish Artifacts

## Purpose

The validation workflow now emits artifacts that future deployment jobs can consume.

## Artifacts

| Artifact | Contents |
|---|---|
| `admin-api-publish` | Published Admin API output |
| `queue-executor-publish` | Published Queue Executor output |
| `publish-manifest` | Publish metadata |
| `admin-web-dist` | Built frontend assets |

## Future usage

Deployment workflows can download these artifacts and deploy them to:

- Azure App Service
- Azure Container Apps
- blob/static frontend hosting
- release packages
