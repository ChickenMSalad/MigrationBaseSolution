# Post-P2 Docs and Tools Cleanup Guide

## What should stay

Keep final checkpoint docs and aggregate validators.

These are not throwaway artifacts anymore; they are the operational validation harness.

## What is usually safe to remove

After P2 is committed, these are usually safe to remove:

```text
tools/dropins/p2-set*/
tools/dropins/apply-p2-set*.ps1
tools/dropins/*fix*.ps1
tools/dropins/*corrective*.ps1
```

Those are installation payloads and corrective patch scripts, not runtime code.

## What not to delete yet

Do not bulk-delete endpoint smoke tests. They are still useful for focused validation.
