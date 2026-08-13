# ADR-005: Platform Capability Adapters without Generic GUI Fallback

- Status: Accepted
- Date: 2026-08-10
- Requirements: 1.4, 6.6, 9.1, 9.4, 9.6

## Context

GX Works2 and TIA Portal/WinCC have different project formats, toolchains, UI
constraints, target concepts, and evidence mechanisms. Sharing raw GUI actions
would hide those differences and create unsafe fallback behavior.

## Decision

Define a common capability-oriented platform port for diagnostics, project and
target validation, baseline/final validation, change application, evidence, and
capability reporting. Implement GX Works2 and TIA V19 behind independent
adapters. An unsupported capability returns a typed result; it never falls back
to generic desktop automation.

## Alternatives considered

- One generic GUI adapter: rejected because labels such as “compile” or “target”
  do not have identical safety semantics across products.
- Import vendor SDKs in the orchestrator: rejected because it couples workflow
  control to platform implementation details.
- Separate complete runtimes per platform: rejected because state, policy,
  evidence, and reporting should remain consistent.

## Consequences

- Some adapter code is deliberately duplicated where platform semantics differ.
- Shared contract tests verify consistent typed behavior.
- Platform capability rollout can be reviewed independently.

## Review triggers

- A third platform demonstrates missing common capability semantics.
- A vendor integration must run out of process.
- Contract evolution would otherwise require breaking all adapters.
