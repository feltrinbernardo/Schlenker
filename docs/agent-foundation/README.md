# Agent Foundation — Phase 0

This directory contains the governance artifacts created before implementation
of the executable Schlenker agent runtime.

## Phase status

| Task | Status | Evidence |
|---|---|---|
| 0.1 Establish a clean implementation baseline | Complete | [Baseline inventory](baseline-inventory-2026-08-10.md) |
| 0.2 Approve the policy boundary | Review required | [Policy boundary proposal](policy-boundaries.md) |
| 0.3 Record foundational decisions | Complete | [ADR index](../adr/README.md) |
| 0.4 Define capability status vocabulary | Complete | [Capability matrix](capability-matrix.md) |

## Current gate

The clean implementation worktree now exists at
`C:\www\Schlenker-agent-foundation` on branch `codex/agent-foundation`, based on
merged commit `f96d2d0`. Runtime implementation remains gated only by Task 0.2:
review and approval of the proposed policy hierarchy before any `AGENTS.md` file
is rewritten.

No artifact in this directory authorizes an engineering-application UI run or a
PLC operation.
