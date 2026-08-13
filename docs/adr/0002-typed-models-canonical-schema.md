# ADR-002: Typed Models Are the Canonical Schema

- Status: Accepted
- Date: 2026-08-10
- Requirements: 1.5, 3.1, 3.4, 3.5

## Context

Profiles, tasks, state, authorizations, tools, policy decisions, evidence, and
reports must be validated in Python and consumable by external tooling. Hand-
maintaining Python classes and JSON Schema independently would create drift.

## Decision

Use versioned Pydantic v2 models as the canonical runtime schema. Generate
stable JSON Schema into `schemas/generated/`. CI regenerates schemas and fails
when checked-in output differs. Generated files are never hand-edited.

## Alternatives considered

- JSON Schema as canonical with generated Python: viable, but adds a generator
  toolchain before the first runtime and makes domain validation less direct.
- Dataclasses plus manual validation: rejected because nested validation,
  versioning, and schema generation would become custom infrastructure.
- Maintain both formats manually: rejected because two sources of truth are
  unsafe.

## Consequences

- Pydantic is a core dependency and its major upgrades require evaluation.
- Schema generation must be deterministic across the supported environment.
- YAML is only an input format; loaded values pass through canonical models.

## Review triggers

- A non-Python consumer becomes the primary schema owner.
- Pydantic cannot express a required compatibility or validation constraint.
- Generated-schema stability cannot be maintained in CI.
