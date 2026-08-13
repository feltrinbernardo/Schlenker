# ADR-003: File-Backed Atomic Checkpoints for Release 1

- Status: Accepted
- Date: 2026-08-10
- Requirements: 4.5, 4.6, 12.1, 12.2, 14.6

## Context

The runtime needs resumable state and audit evidence. The first release runs one
bounded local workflow at a time on an engineering workstation and has no
distributed scheduling requirement.

## Decision

Persist each run under `runs/<run-id>/` using append-only JSON Lines event files,
checksummed content, and an atomically replaced versioned checkpoint. Verify the
event chain and checkpoint checksum before resume. Do not introduce a database
or workflow engine in release 1.

## Alternatives considered

- SQLite: reasonable for queries, but unnecessary before concurrency and
  retention needs are measured.
- Remote database or Temporal-style engine: rejected because deployment and
  operational complexity exceed the first-release requirements.
- In-memory state only: rejected because it cannot provide safe resume or audit.

## Consequences

- The run directory is portable and directly reviewable.
- Only one writer may own a run; locking and sequence checks enforce this.
- Cross-run queries are intentionally limited initially.

## Review triggers

- Concurrent writers or centralized retention become requirements.
- File durability proves insufficient in failure testing.
- Run volume makes filesystem discovery or audit queries impractical.
