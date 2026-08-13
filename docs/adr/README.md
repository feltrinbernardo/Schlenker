# Architecture Decision Records

Architecture decisions for the executable Schlenker agent runtime are recorded
here. An ADR is immutable after supersession except for status and links; a new
decision supersedes an old one rather than rewriting its history.

| ADR | Status | Decision |
|---|---|---|
| [ADR-001](0001-modular-monolith-deterministic-orchestrator.md) | Accepted | Modular monolith with deterministic orchestration |
| [ADR-002](0002-typed-models-canonical-schema.md) | Accepted | Typed models are canonical; JSON Schema is generated |
| [ADR-003](0003-file-backed-atomic-checkpoints.md) | Accepted | File-backed atomic checkpoints for release 1 |
| [ADR-004](0004-fail-closed-policy-engine.md) | Accepted | Fail-closed deterministic policy engine outside the model |
| [ADR-005](0005-platform-capability-adapters.md) | Accepted | Platform capability adapters with no generic GUI fallback |
| [ADR-006](0006-local-jsonl-telemetry.md) | Accepted | Local JSONL telemetry with a future exporter port |
| [ADR-007](0007-hardware-executors-disabled.md) | Accepted | Hardware executors remain disabled in release 1 |
| [ADR-008](0008-prompt-assets-separated-from-runtime.md) | Accepted | Prompt assets are separated from runtime code |

These records implement Task 0.3 of the agent-foundation feature spec. Any
decision that changes a safety or trust boundary requires a new ADR and the
applicable policy/spec review.
