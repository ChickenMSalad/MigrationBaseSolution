# P3 Recommended Plan

## Goal

P3 should convert the P2 diagnostics/governance foundation into controlled production execution.

## P3-A — SQL Operational Store

SQL Server should become the durable operational truth for:

- migration manifests
- run queues/work items
- source IDs
- target IDs
- mapping state
- failures
- retries
- checkpoints
- reconciliation
- reporting

CSV/Excel should be ingestion/export artifacts only.

## P3-B — Auth Enforcement Rollout

Attach policies to route groups while preserving local-development bypass behavior.

## P3-C — Live Queue Worker Enablement

Enable live queue execution only behind governance gates and manual approval.

## P3-D — Durable Audit + Telemetry

Add SQL-backed audit query and production telemetry integration.

## P3-E — Operational UI

Surface readiness, governance, queue state, audit, telemetry, and run status.
