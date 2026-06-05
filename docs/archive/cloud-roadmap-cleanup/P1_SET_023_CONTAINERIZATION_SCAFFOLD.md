# P1 Set 023 — Containerization Scaffold

## Purpose

P1 Set 023 adds Docker/containerization scaffolding for the Admin API and Queue Executor.

This does not change runtime behavior and does not deploy anything.

## Added files

- `deploy/docker/AdminApi.Dockerfile`
- `deploy/docker/QueueExecutor.Dockerfile`
- `deploy/docker/docker-compose.local.yml`
- `deploy/docker/.dockerignore`
- `deploy/docker/README.md`
- `docs/cloud-roadmap-cleanup/P1_SET_023_CONTAINERIZATION_SCAFFOLD.md`

## Why this matters

Cloud deployment options will likely include one or more of:

- Azure App Service
- Azure App Service for Containers
- Azure Container Apps
- Azure Container Apps Jobs
- containerized queue workers

This set establishes container build shape before deployment automation.

## Validation

Optional, only if Docker is installed:

```powershell
docker build -f deploy/docker/AdminApi.Dockerfile -t migration-admin-api:local .
docker build -f deploy/docker/QueueExecutor.Dockerfile -t migration-queue-executor:local .
```

Or:

```powershell
docker compose -f deploy/docker/docker-compose.local.yml up --build
```

Then:

```powershell
Invoke-RestMethod http://localhost:8080/health/live
```
