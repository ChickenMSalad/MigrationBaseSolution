# Post-P2 Cleanup Set 004 — Documentation Inventory

## Purpose

This set inventories the P2 documentation folders and classifies documents into:

- final reference docs
- checkpoint docs
- set-history docs
- post-P2 cleanup docs
- review candidates

It does not delete or move anything.

## Why this matters

During P2, individual set docs were useful. After P2 completion, the repo should clearly distinguish between:

- docs developers should read
- docs that prove historical progression
- docs that can eventually be archived
- cleanup reports

## Usage

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\maintenance\audit-p2-docs.ps1
```

Output:

```text
docs/post-p2-cleanup/P2_DOC_INVENTORY_REPORT.md
```
