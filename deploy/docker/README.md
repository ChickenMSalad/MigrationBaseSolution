# Docker Container Scaffolding

## Purpose

These Dockerfiles are scaffolding for future Azure Container Apps or App Service container deployment.

They are not required for local development.

## Build Admin API

From repo root:

```powershell
docker build -f deploy/docker/AdminApi.Dockerfile -t migration-admin-api:local .
```

## Build Queue Executor

From repo root:

```powershell
docker build -f deploy/docker/QueueExecutor.Dockerfile -t migration-queue-executor:local .
```

## Local compose

From repo root:

```powershell
docker compose -f deploy/docker/docker-compose.local.yml up --build
```

Admin API should be available at:

```text
http://localhost:8080
```

Check:

```powershell
Invoke-RestMethod http://localhost:8080/health/live
Invoke-RestMethod http://localhost:8080/api/cloud/environment
```

## Important

This is a scaffold only.

Before production container deployment, confirm:

- private NuGet feed availability
- Azure credential strategy
- blob-backed control plane
- queue provider configuration
- auth enforcement
- health probe routing
