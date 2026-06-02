# Admin Web Shared API Surface

This folder contains shared/core Admin Web API clients, contracts, readiness surfaces, storage helpers, queue/runtime helpers, and cross-feature utilities.

Feature-specific API clients should live with their feature under `src/features/<domain>/<feature>/api`.

Do not add new feature-only APIs here unless they are intentionally shared by multiple feature areas.

The `api/core` folder remains the shared lower-level client boundary.
