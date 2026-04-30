# Migration Platform

A modular, cloud-ready asset migration platform with a Control Plane, Execution Plane, artifact-driven runs, and queue-based orchestration.

## Overview

This solution provides a reliable, resumable, and observable asset migration system.

Core concepts:

- **Control Plane**: ASP.NET Minimal API and React UI for managing projects, runs, artifacts, credentials, connectors, and preflight validation.
- **Execution Plane**: Queue-based workers that execute migration runs.
- **Artifacts**: Manifest CSV files and JSON mapping profiles used to drive migrations.
- **Orchestration**: `GenericMigrationJobRunner` coordinates manifest loading, mapping, connector execution, and run processing.
- **Preflight**: Validation flow used to detect configuration, artifact, and connector issues before a run starts.

## Current Capabilities

The current system supports the following end-to-end flow:

```text
Create Project → Upload Artifacts → Bind Manifest + Mapping → Run → Worker Executes
```

Known working areas:

- Project creation
- Artifact upload and binding
- Manifest and mapping loading
- Queue-based run execution
- Worker execution through `GenericMigrationJobRunner`
- Preflight endpoint
- Router-based React UI

## Architecture

```text
Control Plane
├── Admin API
├── React UI
├── Projects
├── Runs
├── Artifacts
├── Credentials
├── Connectors
└── Preflight

Execution Plane
├── Queue Worker
├── Azure Queue / Azurite
└── GenericMigrationJobRunner

Artifacts
├── Manifest CSV
└── MappingProfile JSON
```

## Frontend

The frontend is built with:

- React
- Vite
- react-router-dom

Primary routes:

```text
/
/projects
/projects/:projectId
/projects/:projectId/preflight
/runs
/runs/:runId
/artifacts
/credentials
/connectors
/mapping-builder
```

CSS is organized into:

```text
variables.css
base.css
layout.css
components.css
pages.css
preflight.css
```

When adding UI, prefer existing page/component structure and styles before introducing new CSS patterns.

## Backend

The backend uses ASP.NET Minimal API.

Current API areas include:

- Projects
- Runs
- Artifacts
- Credentials
- Connectors
- Preflight

Preflight endpoints:

```http
POST /api/preflight
POST /api/projects/{projectId}/preflight
```

## Local Development

### Prerequisites

Install the following:

- .NET SDK
- Node.js
- npm
- Azurite or Azure Storage Emulator-compatible local queue setup

### Backend

From the API project directory:

```bash
dotnet restore
dotnet build
dotnet run
```

### Frontend

From the frontend project directory:

```bash
npm install
npm run dev
```

### Worker

From the worker project directory:

```bash
dotnet restore
dotnet build
dotnet run
```

## Configuration

Use local configuration files for development only.

Do not commit real credentials, connection strings, API keys, tokens, or production environment values.

Recommended local-only files:

```text
appsettings.Local.json
appsettings.Development.local.json
.env
.env.local
```

## Development Guidelines

This project is now a real distributed migration system. Favor stability and consistency over speed.

When making changes:

- Do not introduce parallel systems.
- Do not introduce new patterns unless clearly needed.
- Follow the existing architecture.
- Keep changes incremental and surgical.
- Match current CSS and component structure.
- Avoid UI/API drift.
- Keep connector behavior behind connector services.
- Prefer artifact-driven flows over ad hoc file handling.
- Validate with preflight before enabling run execution.

## Current Priorities

1. Stabilize Preflight
2. Add run gating so failed preflight blocks runs
3. Improve UX for validation issues
4. Add retry and recovery for failed rows
5. Improve progress and observability, with SignalR as a later enhancement

## Known Issues

- CSS inconsistencies around alignment and specificity
- Endpoint duplication causing Swagger errors
- Configuration confusion between appsettings files and environments
- Worker queue/config mismatches
- Dependency injection registration gaps
- UI/API drift between frontend calls and backend endpoints

## Repository Hygiene

Before committing, make sure the repository does not include:

- Build outputs
- `bin/`
- `obj/`
- `node_modules/`
- `.vs/`
- User-specific IDE files
- Real secrets
- Real credential artifacts
- Local queue/storage data
- Generated migration output
- Large exported manifests unless intentionally used as fixtures

## License

Private/internal project unless otherwise specified.
