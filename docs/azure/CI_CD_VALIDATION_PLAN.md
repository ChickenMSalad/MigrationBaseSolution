# CI/CD Validation Plan

## Current scope

The first CI/CD scaffold validates build and cloud-planning assets only.

It does not deploy.

## Workflow

```text
.github/workflows/migration-platform-validation.yml
```

## Required secrets

None.

## Future CI/CD expansion

Later sets can add:

- Azure login
- Bicep/Terraform validation
- container build
- deployment slots
- environment approvals
- smoke tests against deployed Admin API
- frontend artifact publishing
- worker deployment
