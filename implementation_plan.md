# B2 Live Trip + B7 Currency — RFC-Lite

## Objective

Deliver timezone-aware live-trip visit logging, deterministic currency snapshots, owner-only trip summaries, and resilient background reconciliation in one API release.

## Technical Strategy

- Preserve the existing Clean Architecture/CQRS layout and PostgreSQL-backed integration tests.
- Centralize time calculations in `ITripTemporalService` and currency rules/conversion in dedicated application interfaces.
- Persist original visit values before attempting external conversion; use database-backed pending work for restart safety.
- Keep the existing roadmap edits as the product contract and do not modify them during implementation.

## Execution Sequence

1. Add failing unit tests for currency policy, timezone resolution, and temporal state.
2. Add currency/timezone domain fields, services, API contracts, and foundation migration.
3. Add failing Visit Log validation/handler/API tests, then implement the feature slice and migration.
4. Add failing summary/privacy tests, then implement aggregates and owner-only projections.
5. Add workers, resilience configuration, error codes, and full regression verification.

## Verification

- Release build succeeds without warnings introduced by this work.
- Unit, infrastructure, and API integration suites pass against PostgreSQL.
- Migration scripts contain the required checks, filtered unique indexes, and safe backfills.
- Swagger exposes the authenticated currency, Visit Log, preference, and summary contracts.
