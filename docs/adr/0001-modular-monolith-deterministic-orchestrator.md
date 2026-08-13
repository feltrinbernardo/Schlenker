# ADR-001: Modular Monolith with Deterministic Orchestration

- Status: Accepted
- Date: 2026-08-10
- Requirements: 1.1, 1.2, 4.1, 4.7, 11.6

## Context

The current agent is a set of policies, prompts, templates, profiles, and helper
workflows. The next version needs executable state, policy, tools, and evidence,
but does not yet need independent services or distributed coordination.

## Decision

Build one installable Python modular monolith. Keep domain, orchestration,
policy, tools, ports, adapters, reporting, and telemetry as explicit internal
boundaries. A deterministic orchestrator owns state transitions. The language
model may propose plans and tool calls but cannot commit state or invoke an
adapter directly.

## Alternatives considered

- Agent framework as the primary control plane: rejected because framework
  state and callbacks would obscure safety invariants at this stage.
- Microservices or distributed workflows: rejected because current scale does
  not justify deployment, recovery, and consistency complexity.
- Prompt-only orchestration: rejected because a probabilistic model cannot
  enforce mandatory transitions reliably.

## Consequences

- The complete first vertical slice is debuggable in one process.
- Module boundaries must be enforced by tests and dependency direction.
- A later split remains possible through the defined ports.

## Review triggers

- Measured concurrency or recovery needs cannot be met in one process.
- A platform adapter must be isolated for security or deployment reasons.
- A proposed agent framework can preserve every existing state/policy invariant
  with less total complexity.
