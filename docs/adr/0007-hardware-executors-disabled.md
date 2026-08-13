# ADR-007: Hardware Executors Disabled in Release 1

- Status: Accepted
- Date: 2026-08-10
- Requirements: 5.3, 9.5, 14.3, 19.5

## Context

Vendor tools can read, write, transfer, control run state, reset controllers,
change connections, and modify device memory. The first agent release is meant
to prove repository and offline engineering workflows, not hardware control.

## Decision

Hardware-facing contracts may document future requirements, but their executors
remain unbound and `DISABLED` in release 1. The CLI exposes no operational
hardware-control command. Enabling any such executor requires a separate Kiro
spec, isolated development-target definition, threat/safety review, exact
current-run authorization, recovery procedures, and operator acceptance.

## Alternatives considered

- Bind executors but rely on confirmation prompts: rejected because accidental
  reachability remains possible.
- Permit writes to a known development PLC in release 1: rejected because the
  runtime policy, target identity, evidence, and uncertain-outcome recovery have
  not yet been proven.
- Omit hardware concepts entirely: rejected because contracts and risk classes
  must clearly describe why such actions are unavailable.

## Consequences

- Release 1 remains testable without a PLC.
- Current repository rules remain stricter than runtime capability.
- Future hardware enablement is a product/safety decision, not a configuration
  toggle.

## Review triggers

- A new separately approved hardware-control spec is completed.
- An isolated test environment and exact target verification are demonstrated.
- Deterministic recovery for uncertain external side effects is accepted.
