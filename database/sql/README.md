# SQL Setup

Active schema scripts live in `database/sql/operational`.

Archived phase-era scripts live in `database/sql/archive` and are retained for history only. Do not run archived scripts against a live database unless intentionally investigating old migration history.

Sample/seed scripts live in `database/sql/samples`.

Current setup direction:
1. Create/choose the operational SQL database.
2. Apply `database/sql/operational` scripts in numeric order.
3. Apply only documented reconciliation scripts if explicitly required.
4. Do not blindly run archive or sample scripts.