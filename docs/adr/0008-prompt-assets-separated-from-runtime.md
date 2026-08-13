# ADR-008: Prompt Assets Separated from Runtime Code

- Status: Accepted
- Date: 2026-08-10
- Requirements: 1.5, 11.1, 11.3, 11.6

## Context

The current `agent/` directory mixes system instructions, tool descriptions,
and task/report templates. The executable runtime also needs code for loading,
validating, and assembling prompts. Mixing assets and code would blur ownership
and encourage prompt text to become a hidden policy layer.

## Decision

Store versioned prompt, contract-guidance, and template assets under `agent/`.
Store prompt loading and context-assembly code under
`src/schlenker_agent/prompts/`. Record prompt version and context manifest for
each model call. Enforce permission through the policy engine, never through a
prompt alone.

## Alternatives considered

- Embed prompts in Python modules: rejected because review, versioning, and
  non-code ownership become harder.
- Keep runtime code inside `agent/`: rejected because assets would become an
  ambiguous importable package.
- Treat repository policy files as prompts: rejected because enforcement and
  model guidance require separate trust boundaries.

## Consequences

- Asset moves require reference inventory and compatibility documentation.
- Prompt metadata and output schemas are validated.
- Model/prompt upgrades trigger the eval suite without changing policy code.

## Review triggers

- A provider requires compiled or remote prompt assets.
- Prompt packaging makes reproducible release builds impractical.
- Context manifests cannot represent a new multimodal input type safely.
