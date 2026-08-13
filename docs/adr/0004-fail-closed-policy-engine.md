# ADR-004: Fail-Closed Deterministic Policy Engine

- Status: Accepted
- Date: 2026-08-10
- Requirements: 5.3, 5.4, 7.1, 7.2, 7.4

## Context

Engineering tools can affect proprietary source projects and, in future phases,
external controllers. Prompt instructions and model judgment cannot provide a
stable authorization boundary.

## Decision

Implement policy as deterministic pure rules over normalized typed inputs. The
only outcomes are `ALLOW`, `DENY`, and `REQUIRE_AUTHORIZATION`, with stable
reason codes. Unknown or incomplete context defaults to `DENY`. No LLM or
LLM-as-judge makes the final authorization decision.

## Alternatives considered

- System-prompt enforcement: rejected because prompts are advisory to a
  probabilistic model and are vulnerable to untrusted context.
- LLM classifier or judge: permitted only for advisory quality evaluation, not
  authorization.
- Adapter-specific permission checks only: rejected because rules would drift
  and cross-platform audit would be inconsistent.

## Consequences

- Rules and normalization become safety-critical code with extensive negative
  and property-based tests.
- Every decision is versioned and auditable.
- A new capability is denied until its policy inputs and rules exist.

## Review triggers

- A regulation or safety review requires an external policy language.
- Rule volume becomes unmanageable in pure Python.
- A new risk class changes authorization semantics.
