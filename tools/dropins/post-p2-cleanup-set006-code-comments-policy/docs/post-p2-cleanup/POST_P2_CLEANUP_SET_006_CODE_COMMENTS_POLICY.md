# Post-P2 Cleanup Set 006 — Code Comments Policy

## Purpose

This set adds a code-commenting policy and a maintenance audit for comment coverage in high-value P2 areas.

It does **not** modify source code.

## Why this matters

The repo does not need comments everywhere. It needs comments where they preserve architectural intent, safety constraints, and future maintenance context.

Good comments explain:

- why live execution is disabled
- why queue completion is gated
- why a policy exists
- how SQL operational state should eventually own run truth
- why audit and telemetry are separate
- why local development bypass exists
- how governance prevents destructive execution

Bad comments restate the code:

```csharp
// Gets the snapshot
public Snapshot GetSnapshot()
```

## Commenting standard

Use XML comments for public contracts when the contract is architectural or operationally sensitive:

```csharp
/// <summary>
/// Provides a read-only governance decision for whether live queue execution can be enabled.
/// This service does not enable execution; it only reports whether safety gates allow it.
/// </summary>
public interface IQueueExecutionGovernanceService
{
}
```

Use inline comments sparingly for non-obvious implementation choices:

```csharp
// Live queue execution requires manual approval even when all automated gates pass.
// This prevents accidental destructive runs from configuration drift alone.
RequiresManualApproval: true
```

## Priority areas for comments

1. Queue execution governance
2. Production safety gates
3. Operational mode state
4. Credential access policy readiness
5. Auth policy readiness
6. Audit persistence providers
7. Telemetry writers/sinks
8. Queue idempotency and lease behavior
9. Future SQL operational store boundaries

## Avoid

- comments on trivial getters/setters
- comments that repeat class names
- stale TODOs without issue links
- large essay comments inside methods
- comments that describe old behavior

## Usage

Run the comment audit:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\maintenance\audit-p2-comment-coverage.ps1
```

Output:

```text
docs/post-p2-cleanup/P2_COMMENT_COVERAGE_REPORT.md
```
