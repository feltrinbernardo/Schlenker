# Repository Policy Boundaries

## Status

Proposed for Gate 0.2 review. This document does not yet modify the applicable
`AGENTS.md` hierarchy.

## Problem

The root policy currently describes only GX Works2, while the repository now
contains shared agent infrastructure, Siemens TIA Portal/WinCC source, TIA
Openness helpers, GX Works2 profiles, disposable engineering fixtures, evals,
and repository automation. Keeping all rules in one GX-specific root file makes
TIA work appear out of scope and makes shared requirements harder to identify.

## Proposed hierarchy

```text
AGENTS.md
fixtures/projects/AGENTS.md
fixtures/projects/schlenker-working/AGENTS.md
REV12/AGENTS.md
tools/tia-v19-openness/AGENTS.md
src/schlenker_agent/AGENTS.md
```

## Ownership table

| Policy file | Applies to | Owns |
|---|---|---|
| Root `AGENTS.md` | Entire repository | Change log, secret handling, user-change preservation, evidence principles, destructive-action controls, cross-platform hardware safety |
| `fixtures/projects/AGENTS.md` | Native project fixtures | Protected-source immutability, disposable-copy requirements, ignored native projects, before/after hashes |
| `fixtures/projects/schlenker-working/AGENTS.md` | Schlenker GX Works2 fixture | Exact allowed `.gxw`, protected original, GX Works2 UI gate, hybrid capture, compile evidence |
| `REV12/AGENTS.md` | TIA/WinCC source and documentation | Source/export ownership, offline validation, safety-source constraints, manifest consistency, import order |
| `tools/tia-v19-openness/AGENTS.md` | TIA Openness helpers | Supported TIA version, allowlisted commands, offline/online boundary, build evidence, archive rules |
| `src/schlenker_agent/AGENTS.md` | Executable runtime | State mutation, policy authority, adapter boundaries, schema ownership, tests, redaction, no hardware executor in release 1 |

## Root policy content

The root policy should retain only rules that are true for every workstream:

- preserve user-owned dirty worktree changes;
- use safe filesystem resolution before writes, moves, or deletes;
- keep protected engineering sources immutable;
- require disposable copies for native engineering-project changes;
- treat opening a project, reading a PLC, and writing a PLC as distinct
  capabilities;
- require current, target-specific authorization for hardware-facing actions;
- default to denial when a target or route could reach production;
- store run evidence under a defined run directory and redact secrets;
- append the mandatory change-request log for repository changes.

## Platform-specific content to move

The following existing root sections should move to the GX Works2 fixture scope
without weakening their meaning:

- exact `GD2.exe` and project selection rules;
- current-run GX Works2 operator confirmation;
- Computer Use object/window requirements;
- Windows 10 hybrid capture restrictions;
- exact protected and disposable `.gxw` paths;
- GX Works2 compiler and screenshot evidence requirements.

TIA/WinCC policies must be written from verified TIA V19 project and toolchain
facts. GX rules must not be copied and renamed without confirming that the same
control or evidence mechanism exists.

## Conflict rule

A nested policy may make a rule narrower or add a platform-specific condition.
It may not loosen protected-source, production-route, authorization, audit, or
secret-handling rules. When two applicable rules conflict, execution uses the
more restrictive safe outcome and records the conflict for review.

## Gate 0.2 approval checklist

- [ ] Maintainer approves the proposed file locations.
- [ ] GX Works2 owner confirms no existing guardrail is lost during the move.
- [ ] TIA/WinCC owner confirms the protected-source and disposable-workspace
  model before TIA-specific rules are finalized.
- [ ] Runtime owner accepts deterministic state/policy/schema invariants.
- [ ] Policy validation tests are defined before the root policy is rewritten.

Until this checklist is approved, the existing root `AGENTS.md` remains
authoritative.
