# Key Vault Secret Naming

## Purpose

Define predictable secret names for connector credentials without storing actual secrets in source control.

## Prefix pattern

```text
migration--tenant-{tenantId}--workspace-{workspaceId}
```

For single-tenant/dev:

```text
migration--workspace-{workspaceId}
```

## Connector secret pattern

```text
{prefix}--connector-{role}--{connectorKey}--credential-{credentialSetId}--{secretKind}
```

## Examples

```text
migration--workspace-dev--connector-source--aem--credential-default--username
migration--workspace-dev--connector-source--aem--credential-default--password
migration--tenant-contoso--workspace-prod--connector-target--aprimo--credential-prod--oauthClientSecret
```

## Secret kinds

Use the normalized secret kinds from connector capability contracts:

- username
- password
- bearerToken
- apiKey
- apiSecret
- oauthClientId
- oauthClientSecret
- connectionString
- accessKeyId
- secretAccessKey

## Rules

- Never store actual secret values in repo config.
- Prefer managed identity for cloud access.
- Keep secret names deterministic and environment/workspace-scoped.
- Avoid embedding raw URLs or user emails in secret names.
