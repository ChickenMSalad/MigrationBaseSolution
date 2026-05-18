# Post-P2 Cleanup Set 002 — Drop-in Artifact Cleanup

## Purpose

This set adds a conservative cleanup script for removing applied P2 drop-in artifacts from the repository.

It does **not** remove runtime code, source files, endpoint smoke tests, final validators, or final checkpoint docs.

## Why this cleanup exists

During P2, drop-in sets were useful for applying incremental generated changes. After those changes have been committed into their final source locations, the drop-in payload folders and apply scripts are installation artifacts.

Keeping them forever makes the repo noisy.

## Candidate cleanup targets

The cleanup script targets:

```text
tools/dropins/p2-set*/
tools/dropins/apply-p2-set*.ps1
tools/dropins/*fix*.ps1
tools/dropins/*corrective*.ps1
```

It intentionally does not remove:

```text
tools/test/*.ps1
tools/maintenance/*.ps1
docs/cloud-roadmap-cleanup/*.md
docs/post-p2-cleanup/*.md
src/**
```

## Usage

Preview only:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\maintenance\remove-post-p2-dropin-artifacts.ps1
```

Generate git rm commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\maintenance\remove-post-p2-dropin-artifacts.ps1 -WriteCommandFile
```

Apply removals directly:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\maintenance\remove-post-p2-dropin-artifacts.ps1 -Apply
```

Recommended flow:

1. Run preview.
2. Review output.
3. Run with `-WriteCommandFile`.
4. Review generated command file.
5. Run generated command file or rerun with `-Apply`.
6. Run full P2 validation.
7. Commit cleanup.
