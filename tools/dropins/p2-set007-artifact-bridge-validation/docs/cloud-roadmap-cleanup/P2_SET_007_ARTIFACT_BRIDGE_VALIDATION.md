# P2 Set 007 — Artifact Bridge Validation

## Purpose

P2 Set 007 adds a repeatable smoke test for the new artifact storage bridge.

This does not change runtime behavior.

## Added files

- `tools/test/smoke-artifact-storage-bridge.ps1`
- `tools/test/smoke-artifact-storage-bridge.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_007_ARTIFACT_BRIDGE_VALIDATION.md`

## What it validates

The smoke test exercises:

1. Upload through the new artifact storage bridge.
2. Manifest index update.
3. Download through the bridge.
4. Content verification.
5. Delete through the bridge.

## Usage

Start Admin API, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-artifact-storage-bridge.ps1 -BaseUrl http://localhost:5173
```

Optional workspace:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-artifact-storage-bridge.ps1 `
  -BaseUrl http://localhost:5173 `
  -WorkspaceId workspace-dev
```

## Why this matters

Before replacing older artifact paths with the new storage abstraction, we need a stable validation script that proves the new path is working end-to-end.
