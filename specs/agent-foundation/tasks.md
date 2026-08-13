# Schlenker Agent Foundation — Implementation Tasks

## Execution model

This plan is ordered to prove a small repository-only vertical slice before any
GX Works2 or TIA Portal GUI integration. Each checkbox is intended to be
implementable and reviewable as an isolated change. A parent task is complete
only when its subtasks, tests, documentation, generated artifacts, and mandatory
change-log entry are complete.

Task labels:

- `[P]` may run in parallel with adjacent `[P]` tasks after their stated
  dependencies are complete; parallel tasks must not edit the same files.
- `[GATE]` requires review or an explicit decision before dependent work starts.
- `[DEFERRED]` is intentionally outside the first release.
- Requirement references use acceptance-criterion IDs from `requirements.md`.

No task in this file authorizes opening engineering software, connecting to a
PLC, or performing hardware operations. Any future GUI run must independently
satisfy the repository's current-run operator-confirmation rules.

## Phase 0 — Baseline and governance

- [x] 0.1 [GATE] Establish a clean implementation baseline
  - Completed: the 2026-08-10 baseline inventory is recorded under
    `docs/agent-foundation/`; branch `codex/agent-foundation` and its clean
    worktree were created from merged commit `f96d2d0` without modifying the
    original dirty worktree.
  - Reconcile the merged remote commit with the current local worktree without
    discarding or overwriting user-owned changes.
  - Inventory tracked, untracked, generated, and large binary assets.
  - Record the base commit and branch used for the foundation work.
  - Confirm that the protected GX Works2 original and disposable fixture paths
    match current repository policy.
  - Produce no project or PLC side effects.
  - _Requirements: 1.6, 8.1, 8.2, 18.4_

- [ ] 0.2 [GATE] Approve the platform-neutral root policy boundary
  - Progress: the proposed hierarchy and ownership table are documented in
    `docs/agent-foundation/policy-boundaries.md`; maintainer/platform approval
    remains pending before policy files are changed.
  - Separate shared safety/audit/change-log rules from GX Works2-specific rules.
  - Define the intended nested policy locations for fixtures, GX Works2, TIA
    V19/WinCC, TIA Openness tools, and runtime source.
  - Review conflicts using the most restrictive rule.
  - Add a policy ownership table to contributor documentation.
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 18.1_

- [x] 0.3 [P] Record foundational architecture decisions
  - Add ADR-001 through ADR-008 listed in `design.md`.
  - Include context, decision, alternatives, consequences, and review triggers.
  - Link each ADR from the architecture documentation.
  - _Requirements: 18.2_

- [x] 0.4 [P] Define capability status vocabulary
  - Define `IMPLEMENTED`, `SIMULATED`, `DISABLED`, and `PLANNED` status values.
  - Create the initial GX Works2, TIA V19/WinCC, repository, and evidence
    capability matrix.
  - Mark all hardware-control executors `DISABLED`.
  - _Requirements: 9.5, 18.5, 19.5_

## Phase 1 — Repository and package foundation

- [ ] 1.1 Create the Python package skeleton
  - Add `pyproject.toml`, the `src/schlenker_agent/` package, CLI composition
    root, domain/orchestrator/policy/tools/ports/adapters/reporting/telemetry
    modules, and layered test directories.
  - Configure a `schlenker-agent` console entry point.
  - Keep placeholder modules free of platform behavior.
  - Verify editable installation and import on Python 3.13.
  - _Requirements: 1.1, 1.2, 1.3, 10.1_

- [ ] 1.2 [GATE] Select and configure the static type checker
  - Run a minimal Windows compatibility proof for mypy and Pyright.
  - Select one checker and record ADR-009.
  - Configure it in or from `pyproject.toml` with an explicit strictness policy.
  - Add a passing smoke check for the package skeleton.
  - _Requirements: 1.3, 16.1, 18.2_

- [ ] 1.3 [P] Harden ignore and attribute rules
  - Ignore secret environment files while retaining `.env.example`.
  - Ignore local run output, caches, builds, temporary archives, and vendor
    transient files without hiding source fixtures or curated eval data.
  - Add `.gitattributes` classifications for text, PLC/HMI binaries, archives,
    screenshots, and generated JSON/Markdown.
  - Add tests or validation scripts for critical patterns.
  - _Requirements: 16.2, 16.3, 16.4_

- [ ] 1.4 [P] Add repository secret scanning
  - Add a Gitleaks configuration with justified allowlist entries only.
  - Scan the current tracked tree and remediate or document findings without
    printing secret values.
  - Add the scan to local validation and CI.
  - _Requirements: 3.6, 13.3, 16.2_

- [ ] 1.5 Add nested policy files
  - Implement the hierarchy approved in Task 0.2.
  - Keep the root policy platform-neutral and concise.
  - Add automated checks for expected files, syntax, and prohibited weakening
    of protected-source/hardware restrictions.
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [ ] 1.6 Migrate current agent assets without breaking references
  - Inventory references to `agent/system-prompt.md`, `agent/tools.md`,
    `agent/task-template.md`, and `agent/run-report-template.md`.
  - Move or split them into `agent/prompts/`, `agent/contracts/`, and
    `agent/templates/` only after compatibility documentation is ready.
  - Preserve current safety content and record prompt/template versions.
  - _Requirements: 1.5, 1.6, 11.1, 11.3, 18.1_

## Phase 2 — Canonical domain models and schemas

- [ ] 2.1 Implement schema primitives and version handling
  - Add semantic schema version types, stable identifiers, UTC timestamps,
    digests, sensitivity levels, and shared validation errors.
  - Reject unsupported major versions with actionable error details.
  - Add serialization round-trip and invalid-version tests.
  - _Requirements: 3.3, 3.4, 15.1_

- [ ] 2.2 [P] Implement `TaskRequest` and platform profile models
  - Model objectives, requested capabilities, allowed paths, protected sources,
    constraints, outputs, acceptance checks, platform/toolchain identity, and
    capability status.
  - Load YAML only through a validation boundary.
  - Add valid and adversarial fixtures for GX Works2 and TIA V19 profiles.
  - _Requirements: 3.1, 3.2, 3.6, 9.2, 9.3, 18.5_

- [ ] 2.3 [P] Implement run, transition, and failure models
  - Model all required states, event sequence numbers, terminal outcomes,
    checkpoint metadata, and typed failure categories.
  - Prevent direct unvalidated state deserialization.
  - Add tests for every enum and terminal-state invariant.
  - _Requirements: 3.1, 4.2, 14.1_

- [ ] 2.4 [P] Implement authorization and target-identity models
  - Model run-scoped exact operations, issue/expiry times, issuer, evidence,
    target identity, single-use behavior, and consumption state.
  - Prohibit high-risk wildcards and invalid time intervals.
  - Add expiry, target mismatch, scope, and reuse tests.
  - _Requirements: 3.1, 5.1, 5.3, 5.5_

- [ ] 2.5 [P] Implement tool, policy, evidence, and report models
  - Model risk classes, side effects, retry/idempotency modes, policy outcomes
    and reason codes, evidence provenance/sensitivity, and report fact classes.
  - Add JSON serialization and invalid-contract tests.
  - _Requirements: 3.1, 6.1, 6.2, 7.1, 12.3, 12.6_

- [ ] 2.6 Generate and verify JSON Schemas
  - Generate schemas from canonical Pydantic models into `schemas/generated/`.
  - Add stable ordering and a reproducible generation command.
  - Add a CI/local check that regenerates to a temporary location and fails on
    drift.
  - Document that generated schemas must not be hand-edited.
  - _Requirements: 1.5, 3.5, 16.1_

## Phase 3 — State machine and durable run store

- [ ] 3.1 Implement the transition table
  - Encode the states and allowed edges from `design.md` as data separate from
    the orchestration loop.
  - Model transition preconditions and mandatory evidence identifiers.
  - Deny undeclared transitions with stable reason codes.
  - Test every allowed edge and representative disallowed edges.
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 15.1_

- [ ] 3.2 Implement atomic file-backed run storage
  - Create unique run directories and normalized task/profile snapshots.
  - Append events with monotonic sequence numbers and checksum linkage.
  - Atomically replace checkpoints and verify them during load.
  - Add interrupted-write and corrupted-checkpoint tests.
  - _Requirements: 4.5, 4.6, 12.1, 12.2, 14.6_

- [ ] 3.3 Implement the orchestrator transition service
  - Make it the only module that commits state transitions.
  - Validate preconditions, policy outcome, authorization, and evidence before
    storage mutation.
  - Prevent provider/model code from receiving a mutable run-store handle.
  - Add concurrency/sequence and direct-mutation regression tests.
  - _Requirements: 4.3, 4.5, 4.7, 11.6_

- [ ] 3.4 Implement safe resume and cancellation
  - Revalidate event chain, checkpoint, task/profile/policy compatibility,
    external state, and time-sensitive grants.
  - Support explicit cancellation from non-terminal safe states.
  - Enter `BLOCKED` when reconciliation is required.
  - Add restart, stale-grant, changed-policy, and uncertain-outcome scenarios.
  - _Requirements: 4.6, 10.6, 14.4, 14.5, 14.6_

## Phase 4 — Policy and authorization enforcement

- [ ] 4.1 Implement input normalization and stable reason codes
  - Normalize Windows paths, case, target identities, capabilities, state, and
    contract versions before rule evaluation.
  - Keep raw values only in redacted audit context.
  - Add Windows path alias, separator, traversal, and symlink/junction tests.
  - _Requirements: 7.1, 7.5, 8.3, 15.3_

- [ ] 4.2 Implement fail-closed policy rules
  - Evaluate known profile/tool/target, capability, run state, allowed path,
    protected source, risk class, evidence, and authorization conditions.
  - Return only `ALLOW`, `DENY`, or `REQUIRE_AUTHORIZATION`.
  - Default every unknown or incomplete case to denial.
  - Add a decision table test for every rule and outcome.
  - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [ ] 4.3 Implement current-run authorization matching
  - Require current UI-run development confirmation where applicable.
  - Require exact target and operation for external writes and hardware control.
  - Enforce expiry and single-use consumption atomically with transition/tool
    commitment.
  - Add negative tests for authentication, open application, reachability,
    previous run, and generic continuation requests.
  - _Requirements: 5.2, 5.3, 5.4, 5.5, 5.6_

- [ ] 4.4 Persist and replay policy decisions
  - Store normalized input digest, matched rules, outcome, reason codes, policy
    version, and authorization references.
  - Build a replay command/library API for curated historical decisions.
  - Fail tests on unintended permission expansion.
  - _Requirements: 7.5, 7.6, 12.2_

- [ ] 4.5 Add policy property tests and adversarial corpus
  - Prove path containment, denial monotonicity for missing context, exact grant
    matching, and non-retry of high-risk calls.
  - Add prompt-injection text as untrusted inputs and verify that decisions do
    not change.
  - _Requirements: 11.5, 15.3, 15.5_

## Phase 5 — Tool registry and execution boundary

- [ ] 5.1 Implement tool registration and contract validation
  - Support unique name/version lookup, typed inputs/outputs, risk,
    side-effects, timeout, evidence, retry, idempotency, and adapter binding.
  - Allow documented but disabled/unbound contracts.
  - Reject duplicates, unknown versions, unknown tools, and unbound execution.
  - _Requirements: 6.1, 6.2, 6.4, 9.5_

- [ ] 5.2 Implement the policy-gated tool executor
  - Validate input, resolve contract, request policy, create pre-invocation audit
    record, invoke the adapter, validate output, and persist result evidence.
  - Prevent direct adapter execution from the orchestrator or model provider.
  - Add malformed input/output and observed-side-effect failure tests.
  - _Requirements: 6.3, 6.5, 7.3, 17.4_

- [ ] 5.3 Implement timeout, retry, and idempotency behavior
  - Apply bounded retries only to contracts explicitly marked retryable.
  - Never automatically retry external writes or hardware control.
  - Persist and reuse idempotency keys for safe logical retries.
  - Block on uncertain side-effect outcomes.
  - _Requirements: 14.2, 14.3, 14.4, 14.5_

- [ ] 5.4 Add registry and adapter contract tests
  - Create a reusable contract-test suite for all adapter implementations.
  - Verify typed unsupported-capability results and no generic fallback.
  - Verify adapter namespace isolation.
  - _Requirements: 1.4, 6.6, 9.1, 9.4, 15.2_

## Phase 6 — Repository, evidence, and reporting adapters

- [ ] 6.1 Implement safe repository inspection
  - Resolve and validate allowed roots, protected sources, revision, worktree
    status, metadata, and SHA-256 hashes.
  - Preserve user-owned dirty worktree changes and report them as context.
  - Add case, traversal, junction, missing-path, and protected-source tests.
  - _Requirements: 8.1, 8.2, 8.3, 8.5_

- [ ] 6.2 Implement allowlisted validation commands
  - Define typed executable/argument/working-directory/timeout contracts.
  - Prohibit model-generated shell strings and unresolved broad targets.
  - Capture exit code, stdout/stderr references, duration, and artifact metadata.
  - _Requirements: 6.1, 6.3, 8.2, 12.2_

- [ ] 6.3 Implement the evidence store and index
  - Create the run layout from `design.md`.
  - Enforce run-directory containment, atomic writes, provenance labels,
    checksums, sensitivity, and evidence-to-event links.
  - Reject missing or checksum-invalid mandatory evidence.
  - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5_

- [ ] 6.4 Implement protected-source integrity verification
  - Record the configured before snapshot and compare the after snapshot.
  - Make integrity mismatch a deterministic release/report failure.
  - Add unchanged, modified, missing, and metadata-only test cases.
  - _Requirements: 8.1, 8.4, 8.5, 19.1_

- [ ] 6.5 Implement canonical JSON and Markdown reports
  - Render both formats from typed run data.
  - Separate verified facts, inferences, operator assertions, skipped steps,
    failures, and unresolved risks.
  - Include versions, state path, decisions, actions, validation, integrity, and
    evidence provenance.
  - Add consistency and golden-output tests.
  - _Requirements: 12.6, 12.7, 18.1_

## Phase 7 — CLI and local developer experience

- [ ] 7.1 Implement `doctor`
  - Check Python, install health, profiles, repository paths, executable
    discovery, run-directory write access, and capability status.
  - Do not launch engineering applications or create external side effects.
  - Return human-readable and optional JSON results.
  - _Requirements: 10.2, 10.3_

- [ ] 7.2 [P] Implement `validate`
  - Validate profiles, tasks, policy data, generated-schema consistency, and
    cross-file references.
  - Aggregate actionable errors and remain side-effect free.
  - _Requirements: 3.2, 3.3, 10.2, 10.4_

- [ ] 7.3 Implement `run`, `status`, and `resume`
  - Require explicit task/profile paths for a new run.
  - Print run ID and absolute run directory.
  - Render current state, material blockers, and evidence summary.
  - Connect resume to the revalidation service from Task 3.4.
  - _Requirements: 10.2, 10.5, 10.6_

- [ ] 7.4 Implement stable exit codes and CLI integration tests
  - Map validation, policy, authorization, tool, timeout, evidence, integrity,
    and internal failures to the codes in `design.md`.
  - Test help, invalid input, success, blocked, failed, and resumed runs.
  - _Requirements: 10.7, 15.2_

## Phase 8 — Model port, prompts, and context engineering

- [ ] 8.1 Define the provider-neutral model port
  - Model messages, structured tool proposals, usage, stop reason, provider/model
    identifiers, and typed provider errors.
  - Keep vendor SDK types inside provider adapters.
  - _Requirements: 17.1, 17.2_

- [ ] 8.2 Implement the deterministic fake model adapter
  - Replay scripted plans, valid tool proposals, malformed tool proposals,
    unknown tools, refusals, timeouts, and provider errors.
  - Use it in all deterministic CI and eval workflows.
  - _Requirements: 17.4, 17.5, 19.3_

- [ ] 8.3 Version and validate prompt assets
  - Add prompt metadata for ID, version, intended state, required inputs, allowed
    tools, and output schema.
  - Separate system, planning, execution, and reporting assets.
  - Record prompt/model/tool-contract versions for every invocation.
  - _Requirements: 11.1, 11.3, 18.1_

- [ ] 8.4 Implement progressive context assembly
  - Select only the platform, task, state, tool, and evidence context required by
    the current call.
  - Label system instructions, operator input, untrusted project content, tool
    output, and evidence.
  - Produce a digest manifest for every context bundle.
  - Add prompt-injection boundary tests.
  - _Requirements: 11.2, 11.4, 11.5_

- [ ] 8.5 [GATE] Add one real model provider adapter
  - Select the first provider and version through an ADR.
  - Implement the model port without adding vendor types to domain code.
  - Add contract tests using mocks/recorded sanitized fixtures.
  - Run golden/adversarial evals before enabling it by configuration.
  - _Requirements: 17.2, 17.3, 17.5_

## Phase 9 — Telemetry, privacy, and auditability

- [ ] 9.1 Implement structured JSONL telemetry
  - Add run/transition/tool/model correlation IDs, timestamps, duration, outcome,
    retry, usage, and failure-category fields.
  - Keep the domain dependent only on `TelemetryPort`.
  - _Requirements: 13.1, 13.2, 13.5_

- [ ] 9.2 Implement centralized redaction and sensitivity policy
  - Redact secret fields, environment values, credentials, tokens, and sensitive
    connection details before persistence.
  - Fall back to checksums/minimal metadata or reject unsafe persistence.
  - Add canary-secret and nested-payload tests.
  - _Requirements: 13.3, 13.4, 15.3_

- [ ] 9.3 Connect audit records across state, policy, tools, model, and evidence
  - Verify every material action can be traced from report to evidence and from
    evidence to the originating transition/tool call.
  - Add orphan-record and broken-reference tests.
  - _Requirements: 7.5, 12.2, 12.3, 13.1_

- [ ] 9.4 [DEFERRED] Add an optional OpenTelemetry exporter
  - Implement only after local JSONL events are stable and a deployment target
    with retention/redaction requirements is approved.
  - _Requirements: 13.5_

## Phase 10 — Repository-only vertical slice

- [ ] 10.1 Define the first golden task and profile
  - Create a sanitized repository-only task with explicit allowed paths,
    protected-source hashes, validation command, evidence, and expected state
    path.
  - Ensure it requires no engineering application or PLC access.
  - _Requirements: 19.1, 19.3_

- [ ] 10.2 Wire the end-to-end offline workflow
  - Compose the real state machine, policy engine, registry, repository adapter,
    evidence adapter, fake model/deterministic plan, run store, telemetry, and
    report renderer.
  - Advance through all applicable lifecycle states without bypass hooks.
  - _Requirements: 19.1, 19.2, 19.3_

- [ ] 10.3 Add interruption, denial, corruption, and integrity scenarios
  - Test restart from valid checkpoints, invalid transitions, protected-source
    write attempts, corrupted evidence, stale grant, malformed model tool input,
    unsupported capability, and changed protected-source hash.
  - Verify no failure path reports success.
  - _Requirements: 4.6, 7.2, 8.4, 12.5, 14.4, 14.6, 15.5_

- [ ] 10.4 [GATE] Accept the first-release offline foundation
  - Run all deterministic tests and evals.
  - Inspect the complete run directory and both report formats.
  - Verify that no GX Works2/TIA application or hardware executor is reachable
    from the first-release CLI.
  - Record remaining risks and deferred capability statuses.
  - _Requirements: 9.5, 19.4, 19.5_

## Phase 11 — Evaluation and CI release gates

- [ ] 11.1 Build layered deterministic test suites
  - Organize unit, integration, contract, property, and eval tests with markers
    and documented local commands.
  - Keep hardware and GUI requirements out of generic CI.
  - _Requirements: 15.1, 15.2, 15.3_

- [ ] 11.2 Build the golden and adversarial eval corpus
  - Define tasks, expected state paths, allowed/prohibited calls, mandatory
    evidence, and deterministic graders.
  - Cover prompt injection, stale authorization, target ambiguity, protected
    source, missing evidence, schema errors, and unsupported capabilities.
  - _Requirements: 15.4, 15.5, 15.6_

- [ ] 11.3 Add regression-case policy
  - Document that every corrected behavioral or safety defect adds a failing-
    before/passing-after test or eval.
  - Add a template linking the defect, requirement, trace/evidence, and grader.
  - _Requirements: 15.7, 18.4_

- [ ] 11.4 Implement pull-request CI
  - Run policy/profile/spec validation, Ruff, the selected type checker, tests,
    schema drift, Gitleaks, attribute/ignore checks, and requirement reference
    validation.
  - Preserve the existing mandatory change-log hook.
  - Publish concise sanitized failure artifacts.
  - _Requirements: 2.6, 16.1, 16.2, 16.5, 16.6_

- [ ] 11.5 [DEFERRED] Add an advisory LLM report-quality grader
  - Keep safety, authorization, integrity, and evidence release gates fully
    deterministic.
  - Version prompts/model and store the advisory score separately.
  - _Requirements: 15.6, 17.3_

## Phase 12 — GX Works2 offline adapter

- [ ] 12.1 [GATE] Confirm GX Works2 adapter scope and profile
  - Validate executable identity, version, protected source, disposable fixture,
    approved helper, capability statuses, and evidence requirements.
  - Confirm nested policies match the repository safety gate.
  - This task is read-only and does not launch GX Works2.
  - _Requirements: 2.2, 8.1, 9.2, 18.5_

- [ ] 12.2 Implement GX Works2 diagnostics and fixture validation
  - Implement `doctor`, project resolution, protected-source snapshot, fixture
    containment, helper validation, and supported-capability reporting.
  - Return typed unsupported results for unimplemented GUI/hardware operations.
  - Add tests using filesystem and process-discovery fakes.
  - _Requirements: 8.2, 8.3, 9.1, 9.2, 9.4_

- [ ] 12.3 [GATE] Design the offline compile integration runbook
  - Map the approved Computer Use accessibility/keyboard workflow and hybrid
    read-only capture evidence into adapter tool contracts.
  - Require fresh operator development-environment confirmation for every UI run.
  - Keep project-open, Read from PLC, and Write to PLC as distinct capabilities;
    opening a project must not imply either PLC operation.
  - Define human checkpoints for graphical-only controls.
  - Do not execute the run as part of this task.
  - _Requirements: 5.2, 8.6, 9.6, 12.4_

- [ ] 12.4 Implement and test GX Works2 offline compile capability
  - Implement only after Task 12.3 review and current-run confirmation for each
    real UI test.
  - Verify exact `GD2.exe`, foreground ownership, fixture path, capture manifest,
    compiler output, and source integrity.
  - Keep online and hardware executors unbound.
  - _Requirements: 8.4, 9.2, 9.5, 9.6, 12.2, 12.4_

## Phase 13 — TIA Portal / WinCC V19 offline adapter

- [ ] 13.1 [GATE] Define the TIA V19 protected-source and workspace model
  - Confirm canonical protected artifacts, disposable workspace, supported TIA
    and WinCC versions, Openness prerequisites, exports, and capability statuses.
  - Add/review the applicable nested policies before implementation.
  - Resolve Open Decision 2 in `design.md`.
  - _Requirements: 2.3, 8.1, 9.3, 18.5_

- [ ] 13.2 Inventory and contract existing TIA Openness helpers
  - Map each helper command to a typed capability, input/output schema, risk,
    side effects, timeout, evidence, and failure modes.
  - Reject shell-string passthrough and undocumented commands.
  - Mark unsupported or hardware-relevant functions disabled.
  - _Requirements: 6.1, 6.6, 9.3, 9.4, 9.5_

- [ ] 13.3 Implement TIA V19 diagnostics and offline project validation
  - Implement environment checks, version validation, project/workspace
    containment, protected-source snapshots, and capability reporting.
  - Add adapter contract and sanitized integration tests without PLC access.
  - _Requirements: 8.2, 8.3, 9.1, 9.3, 15.2_

- [ ] 13.4 [GATE] Implement reviewed offline compile/export evidence
  - Define exact build/export operations and evidence before binding executors.
  - Exercise only approved offline tooling and disposable artifacts.
  - Verify output contract, report evidence, and protected-source integrity.
  - _Requirements: 8.4, 9.3, 9.6, 12.2, 12.5_

## Phase 14 — Documentation and release readiness

- [ ] 14.1 Write contributor and operator documentation
  - Document setup, architecture, policies, profiles, CLI, test/eval commands,
    evidence interpretation, capability matrix, failure/resume behavior, and
    current safety boundaries.
  - Include a concise “first task” walkthrough using the repository-only slice.
  - _Requirements: 18.1, 18.5_

- [ ] 14.2 Add spec and requirement-link validation
  - Parse all `_Requirements:` references in this file.
  - Fail on missing acceptance IDs, malformed references, or mandatory
    requirements with no implementation task.
  - Produce a generated traceability report without modifying this spec.
  - _Requirements: 16.6, 18.3_

- [ ] 14.3 Define local evidence retention and sanitization
  - Set retention periods, deletion ownership, sensitivity classes, and the
    process for promoting sanitized runs into eval fixtures.
  - Confirm change-log and run logs never copy credentials or production
    connection details.
  - Resolve Open Decisions 3 and 4 in `design.md`.
  - _Requirements: 12.3, 13.3, 13.4, 18.1_

- [ ] 14.4 [GATE] Conduct release review
  - Verify requirements/design/tasks traceability and ADR status.
  - Run CI and the complete deterministic eval set from a clean checkout.
  - Review capability matrix, known limitations, disabled hardware actions,
    dependency/secret scan results, and evidence from the offline vertical slice.
  - Approve or block platform-adapter rollout separately from the runtime release.
  - _Requirements: 15.4, 16.1, 18.2, 19.4, 19.5_

## Phase 15 — Explicitly deferred architecture

- [ ] 15.1 [DEFERRED] Evaluate durable database or workflow-engine storage
  - Revisit only when file-backed checkpoints fail measured concurrency,
    retention, or recovery requirements.
  - Requires a new ADR and migration/recovery spec.
  - _Requirements: 4.5, 18.2_

- [ ] 15.2 [DEFERRED] Evaluate retrieval or vector storage
  - Revisit only after an eval demonstrates that bounded filesystem context is
    insufficient and defines retrieval quality/privacy requirements.
  - _Requirements: 11.2, 15.4_

- [ ] 15.3 [DEFERRED] Evaluate multi-agent orchestration
  - Revisit only after the single-agent vertical slices are reliable and a
    measured task class benefits from safe parallel delegation.
  - Requires explicit authority, shared-state, cancellation, and evidence design.
  - _Requirements: 4.1, 12.2, 18.2_

- [ ] 15.4 [DEFERRED] Specify any hardware-control executor
  - Create a separate requirements/design/tasks spec, safety/threat review,
    isolated test-environment definition, exact authorization semantics, recovery
    procedures, and operator acceptance before binding a hardware executor.
  - The default outcome remains disabled.
  - _Requirements: 5.3, 5.4, 9.5, 19.5_

## Suggested delivery increments

| Increment | Tasks | Demonstrable outcome |
|---|---|---|
| A — Governed skeleton | 0.1–1.6 | Clear ownership, installable package, hardened repository |
| B — Deterministic core | 2.1–5.4 | Typed schemas, state, policy, authorization, tool boundary |
| C — Auditable offline runtime | 6.1–9.3 | Safe repository/evidence adapters, CLI, model port, telemetry |
| D — First usable release | 10.1–11.4, 14.1–14.4 | End-to-end repository-only run with CI and eval gates |
| E — GX Works2 offline | 12.1–12.4 | Reviewed fixture diagnostics and offline compile evidence |
| F — TIA V19 offline | 13.1–13.4 | Reviewed TIA/WinCC offline validation and evidence |

Do not begin Increment E or F merely because Increment D is complete. Each
platform increment has its own policy and environment gates.
