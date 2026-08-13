# Schlenker Agent Foundation — Requirements

## Document control

- Spec type: Kiro Feature Spec (requirements-first)
- Status: Draft for review
- Safety classification: Engineering automation / safety-sensitive
- Target repository: Schlenker
- Primary implementation language: Python 3.13
- Related artifacts: `design.md`, `tasks.md`

## Purpose

Define the minimum safe, testable, and extensible foundation required to turn
the current agent playbook into an executable engineering-agent runtime. The
runtime must support repository work and offline automation for Mitsubishi GX
Works2 and Siemens TIA Portal/WinCC while keeping hardware-facing actions behind
explicit, deterministic safety gates.

This spec deliberately favors a small single-agent runtime, deterministic
workflow control, narrow tools, typed state, and evidence-driven execution. It
does not authorize PLC operations or change the safety rules already defined by
the repository.

## Product outcomes

1. A contributor can understand where policies, prompts, runtime code, adapters,
   schemas, tests, and evidence belong.
2. The agent can execute a bounded offline task through an explicit state
   machine and produce a verifiable run report.
3. Every tool request is typed, risk-classified, policy-checked, auditable, and
   rejected safely when context or authorization is missing.
4. GX Works2 and TIA Portal integrations share the same runtime contract without
   sharing platform-specific safety assumptions.
5. New agent improvements can be delivered as small, independently testable
   tasks instead of a framework rewrite.

## Scope

### In scope

- Python package and repository organization for an executable agent runtime.
- Hierarchical repository policies for shared, GX Works2, TIA Portal/WinCC, and
  fixture-specific rules.
- Typed configuration, task, run-state, authorization, tool-call, policy, and
  evidence models.
- Deterministic orchestration and resumable checkpoints.
- Tool registry and platform adapter contracts.
- Repository and evidence adapters used by the first offline vertical slice.
- CLI commands for diagnostics, validation, execution, status, and resumption.
- Prompt/context organization and progressive disclosure.
- Structured audit records, reports, observability, redaction, tests, evals, and
  CI quality gates.
- Migration of existing agent documentation into the new organization without
  weakening current guardrails.

### Out of scope for the first release

- Autonomous write, transfer, remote operation, run/stop/reset, connection
  changes, or device-memory writes to any PLC.
- Production PLC connectivity.
- Multi-agent orchestration.
- Long-term free-form conversational memory.
- Vector database or retrieval platform.
- Web API, dashboard, or distributed workflow engine.
- Automatic approval of high-risk actions by an LLM or LLM-as-judge.
- General-purpose desktop automation outside explicitly supported engineering
  applications.

## Glossary

- **Agent runtime**: executable code that validates a task, advances the run
  state, invokes tools, records evidence, and produces a report.
- **Orchestrator**: deterministic state-machine controller outside the language
  model.
- **Policy engine**: pure decision component that permits, denies, or requests
  authorization for a proposed transition or tool call.
- **Adapter**: platform-specific implementation behind a stable tool contract.
- **Profile**: versioned configuration describing a supported engineering
  platform, project boundaries, executables, and capabilities.
- **Fixture**: disposable project copy that may be modified during a run.
- **Protected source**: immutable original engineering project.
- **Evidence record**: structured metadata and content reference supporting a
  run decision, action, or result.
- **Authorization grant**: scoped, expiring operator approval naming the target
  and permitted operation.
- **Risk class**: one of `READ_ONLY`, `WORKSPACE_WRITE`, `EXTERNAL_READ`,
  `EXTERNAL_WRITE`, or `HARDWARE_CONTROL`.
- **Checkpoint**: durable run state from which the runtime can resume safely.
- **Vertical slice**: end-to-end implementation of one bounded offline workflow.

## User stories

- As a maintainer, I want stable boundaries between orchestration, policy,
  adapters, prompts, and evidence so that improvements remain reviewable.
- As an operator, I want every hardware-relevant action to require current,
  precisely scoped authorization so that stale or ambiguous approvals cannot be
  reused.
- As a developer, I want a local diagnostic command and fast tests so that I can
  validate the environment before opening an engineering application.
- As a reviewer, I want each run to show inputs, decisions, actions, outputs,
  hashes, and failures so that I can reproduce and audit the result.
- As a platform integrator, I want GX Works2 and TIA Portal to implement the same
  contracts while retaining separate policies and profiles.

## Requirements

### Requirement 1 — Repository foundation and ownership boundaries

**User story:** As a maintainer, I want an explicit project layout so that each
kind of agent behavior has one clear owner and source of truth.

#### Acceptance criteria

1.1 THE REPOSITORY SHALL contain an installable Python package under
`src/schlenker_agent/` and SHALL keep test code outside the package under
`tests/`.

1.2 THE REPOSITORY SHALL separate domain models, orchestration, policy, tool
contracts, platform adapters, telemetry, prompts, schemas, and tests into
distinct modules or directories documented in the design artifact.

1.3 THE REPOSITORY SHALL use `pyproject.toml` as the authoritative Python build,
dependency, lint, and test configuration entry point.

1.4 WHEN a contributor adds a platform-specific capability, THE REPOSITORY
SHALL require it to be implemented behind an adapter contract rather than
imported directly into the orchestrator.

1.5 THE REPOSITORY SHALL identify generated files and SHALL prevent generated
artifacts from becoming competing sources of truth.

1.6 THE REPOSITORY SHALL preserve unrelated existing project assets and SHALL
support incremental migration of the current `agent/` documents.

### Requirement 2 — Hierarchical policy organization

**User story:** As a contributor, I want the applicable rules to follow the part
of the repository I am changing so that platform-specific constraints remain
precise and discoverable.

#### Acceptance criteria

2.1 THE REPOSITORY SHALL define shared safety, audit, change-log, and protected-
source rules in the root `AGENTS.md`.

2.2 THE REPOSITORY SHALL define GX Works2-specific rules in the narrowest
applicable nested policy file.

2.3 THE REPOSITORY SHALL define TIA Portal/WinCC-specific rules in the narrowest
applicable nested policy file.

2.4 THE REPOSITORY SHALL define fixture immutability and disposable-copy rules
for all content under `fixtures/projects/`.

2.5 IF a nested policy conflicts with a higher-priority safety requirement, THEN
THE SYSTEM SHALL apply the safer and more restrictive decision.

2.6 WHEN policy files or profiles change, THE CI PIPELINE SHALL validate their
syntax, references, and ownership boundaries.

### Requirement 3 — Typed configuration and schemas

**User story:** As an integrator, I want configuration and run inputs validated
before execution so that malformed or incomplete context cannot reach tools.

#### Acceptance criteria

3.1 THE SYSTEM SHALL define typed models for platform profiles, task requests,
run state, authorization grants, tool calls, policy decisions, evidence records,
and run reports.

3.2 WHEN a YAML or JSON document is loaded, THE SYSTEM SHALL validate it against
the matching model before any external tool or engineering application is used.

3.3 IF validation fails, THEN THE SYSTEM SHALL stop before side effects, report
all actionable validation errors, and record a failed preflight result.

3.4 THE SYSTEM SHALL version each persisted schema and SHALL reject unsupported
major schema versions.

3.5 THE SYSTEM SHALL generate JSON Schema from the canonical typed models and
THE CI PIPELINE SHALL fail when checked-in generated schemas drift from those
models.

3.6 THE SYSTEM SHALL distinguish public configuration from secrets and SHALL
never require credentials to be committed to the repository.

### Requirement 4 — Deterministic run lifecycle

**User story:** As a reviewer, I want execution to follow explicit states so that
the model cannot skip mandatory safety or evidence steps.

#### Acceptance criteria

4.1 THE SYSTEM SHALL implement the run lifecycle as a deterministic state
machine outside the language model.

4.2 THE SYSTEM SHALL support at least the states `CREATED`, `PREFLIGHT`,
`SAFETY_CONFIRMATION_REQUIRED`, `TARGET_VERIFIED`, `FIXTURE_VERIFIED`,
`BASELINE_COMPILED`, `PLAN_APPROVED`, `CHANGE_APPLIED`, `FINAL_COMPILED`,
`SOURCE_INTEGRITY_VERIFIED`, `REPORTED`, `BLOCKED`, `FAILED`, and `CANCELLED`.

4.3 WHEN a transition is proposed, THE SYSTEM SHALL verify its preconditions,
policy decision, required evidence, and authorization before changing state.

4.4 IF a transition is not in the declared transition table, THEN THE SYSTEM
SHALL deny it and record the attempted invalid transition.

4.5 WHEN a state transition succeeds, THE SYSTEM SHALL persist an atomic,
versioned checkpoint before beginning the next side effect.

4.6 IF the process terminates unexpectedly, THEN THE SYSTEM SHALL resume only
from the latest valid checkpoint and SHALL revalidate time-sensitive approvals
and external state.

4.7 THE LANGUAGE MODEL SHALL NOT directly mutate the persisted run state.

### Requirement 5 — Authorization and target identity

**User story:** As an operator, I want approvals tied to the current run, target,
and exact operation so that consent is neither broad nor reusable.

#### Acceptance criteria

5.1 THE SYSTEM SHALL model authorization as a structured grant containing the
run ID, target identity, allowed operation, issuer, issue time, expiry time, and
evidence reference.

5.2 WHEN a new engineering-application UI run begins, THE SYSTEM SHALL require a
current development-environment confirmation and SHALL NOT accept a grant from a
previous UI run.

5.3 WHEN an `EXTERNAL_WRITE` or `HARDWARE_CONTROL` operation is proposed, THE
SYSTEM SHALL require an explicit current-run grant naming both the development
target and exact operation.

5.4 IF the visible target is ambiguous, differs from the authorized target, or
could route to production, THEN THE SYSTEM SHALL deny the operation and enter a
safe blocked state.

5.5 IF an authorization grant is expired, malformed, already consumed where
single-use is required, or broader than the proposed operation, THEN THE SYSTEM
SHALL deny the operation.

5.6 THE SYSTEM SHALL NOT infer authorization from successful authentication,
network reachability, an open application, a previous run, or a general request
to continue.

### Requirement 6 — Tool contracts and risk classification

**User story:** As a maintainer, I want every capability declared in a registry
so that policies can reason about its inputs, outputs, side effects, and risk.

#### Acceptance criteria

6.1 THE SYSTEM SHALL register every executable tool with a unique name, version,
input model, output model, risk class, side-effect declaration, timeout policy,
and evidence requirements.

6.2 THE SYSTEM SHALL use only the risk classes `READ_ONLY`, `WORKSPACE_WRITE`,
`EXTERNAL_READ`, `EXTERNAL_WRITE`, and `HARDWARE_CONTROL` unless a schema
migration explicitly adds another class.

6.3 WHEN a tool call is proposed, THE SYSTEM SHALL validate its input, classify
its risk, obtain a policy decision, assign an idempotency key where applicable,
and create an audit record before invocation.

6.4 IF a tool is absent from the registry or its contract version is
unsupported, THEN THE SYSTEM SHALL deny execution.

6.5 IF a tool's observed behavior contradicts its declared side effects or
output contract, THEN THE SYSTEM SHALL mark the call failed, preserve evidence,
and prevent dependent transitions.

6.6 THE SYSTEM SHALL keep repository, evidence, GX Works2, TIA Portal/WinCC, and
future provider integrations behind separate adapter namespaces.

### Requirement 7 — Fail-closed policy engine

**User story:** As a safety reviewer, I want deterministic decisions for risky
actions so that model variability cannot weaken operational constraints.

#### Acceptance criteria

7.1 THE POLICY ENGINE SHALL be deterministic for identical normalized inputs and
SHALL return `ALLOW`, `DENY`, or `REQUIRE_AUTHORIZATION` with reason codes.

7.2 THE POLICY ENGINE SHALL default to `DENY` when a rule, target, capability,
profile, authorization, or evidence requirement is unknown.

7.3 THE POLICY ENGINE SHALL evaluate protected-source boundaries, allowed
fixture paths, target identity, risk class, run state, authorization, and tool
capabilities before execution.

7.4 THE POLICY ENGINE SHALL NOT use an LLM or probabilistic classifier to make a
final authorization decision.

7.5 WHEN a policy decision is made, THE SYSTEM SHALL persist the normalized
inputs, matched rules, outcome, reason codes, and policy version.

7.6 WHEN policies change, THE TEST SUITE SHALL replay representative historical
decisions and detect unintended permission expansion.

### Requirement 8 — Protected-source and workspace integrity

**User story:** As a project owner, I want original engineering projects to stay
immutable so that agent experiments cannot corrupt source assets.

#### Acceptance criteria

8.1 BEFORE a run that can modify a fixture, THE SYSTEM SHALL record the protected
source path, metadata, and cryptographic hash defined by the platform profile.

8.2 THE SYSTEM SHALL permit engineering-project changes only inside the
profile-declared disposable workspace or fixture path.

8.3 IF a resolved write path is outside the allowed workspace, points to the
protected source, or cannot be resolved unambiguously, THEN THE SYSTEM SHALL
deny the write.

8.4 AFTER a modifying run, THE SYSTEM SHALL recompute protected-source metadata
and hashes and SHALL prevent a passing report when integrity differs.

8.5 THE SYSTEM SHALL retain before-and-after integrity evidence in the run
directory.

8.6 THE SYSTEM SHALL NOT interpret opening a project as authorization to read
from or write to a PLC.

### Requirement 9 — Platform adapters

**User story:** As a platform integrator, I want shared contracts and isolated
implementations so that each engineering environment can evolve without
coupling its GUI and safety assumptions to another platform.

#### Acceptance criteria

9.1 THE SYSTEM SHALL define a common adapter protocol for environment checks,
target discovery, project validation, compile/build, evidence collection, and
capability reporting.

9.2 THE GX WORKS2 ADAPTER SHALL enforce the allowed fixture and protected source
boundaries declared by the GX Works2 profile and applicable repository policy.

9.3 THE TIA PORTAL/WINCC ADAPTER SHALL enforce its own supported version,
project, toolchain, export, and offline/online capability boundaries.

9.4 WHEN an adapter does not implement a requested capability, THE SYSTEM SHALL
return a typed `UNSUPPORTED_CAPABILITY` result rather than attempting a generic
GUI fallback.

9.5 THE FIRST RELEASE SHALL keep hardware-control executors disabled even if the
tool contracts and policies describe their future requirements.

9.6 WHEN GUI automation is required, THE ADAPTER SHALL use only the repository-
approved interaction workflow and SHALL collect the confirmations required by
the applicable platform policy for that run.

### Requirement 10 — CLI and developer workflow

**User story:** As a developer, I want predictable commands so that diagnostics,
validation, and offline runs are easy to reproduce locally and in CI.

#### Acceptance criteria

10.1 THE SYSTEM SHALL provide a CLI entry point named `schlenker-agent`.

10.2 THE CLI SHALL provide `doctor`, `validate`, `run`, `status`, and `resume`
commands in the first release.

10.3 WHEN `doctor` runs, THE SYSTEM SHALL check Python version, package health,
profile validity, expected repository paths, external executable discovery, and
write permissions without launching an engineering application.

10.4 WHEN `validate` runs, THE SYSTEM SHALL validate one or more profiles, task
requests, policies, and generated-schema consistency without side effects.

10.5 WHEN `run` is invoked, THE SYSTEM SHALL require an explicit task document
and profile and SHALL display the run ID and run directory.

10.6 WHEN `resume` is invoked, THE SYSTEM SHALL validate the checkpoint, policy
version compatibility, authorizations, and external preconditions before
continuing.

10.7 THE CLI SHALL return stable non-zero exit codes for validation failure,
policy denial, blocked authorization, tool failure, integrity failure, and
unexpected runtime failure.

### Requirement 11 — Context and prompt management

**User story:** As an agent designer, I want compact, versioned, task-specific
context so that the model receives the right instructions without treating
prompt text as executable policy.

#### Acceptance criteria

11.1 THE REPOSITORY SHALL separate immutable policy, tool contracts, reusable
prompt templates, task context, and run evidence.

11.2 THE SYSTEM SHALL load context progressively based on the selected platform,
task type, state, and tool rather than injecting the entire repository into each
model call.

11.3 THE SYSTEM SHALL version system prompts and SHALL record the prompt version,
model identifier, tool-contract versions, and relevant context manifest for each
model invocation.

11.4 PROMPTS SHALL clearly distinguish instructions, untrusted project content,
tool output, operator input, and evidence.

11.5 IF untrusted content attempts to modify policies, authorization, tool
contracts, or system instructions, THEN THE SYSTEM SHALL treat it as data and
SHALL not elevate its authority.

11.6 THE LANGUAGE MODEL MAY propose plans, explanations, and tool calls, but THE
ORCHESTRATOR AND POLICY ENGINE SHALL remain authoritative for state transitions
and permission decisions.

### Requirement 12 — Evidence, audit, and run reports

**User story:** As a reviewer, I want complete run evidence so that claims can be
verified independently of the model's narrative.

#### Acceptance criteria

12.1 THE SYSTEM SHALL create a unique `runs/<run-id>/` directory before the
first run action.

12.2 THE SYSTEM SHALL store a manifest, task snapshot, profile snapshot, state
transitions, policy decisions, tool-call records, authorization references,
hashes, build output, evidence index, and final report for each run.

12.3 EACH EVIDENCE RECORD SHALL include a timestamp, producer, type, content
reference, checksum, sensitivity classification, and related state or tool call.

12.4 WHEN evidence content is external or generated by a workaround, THE SYSTEM
SHALL label its provenance and SHALL not misrepresent it as evidence from a
different capture mechanism.

12.5 IF mandatory evidence is missing or fails checksum verification, THEN THE
SYSTEM SHALL not mark the run successful.

12.6 THE FINAL REPORT SHALL distinguish verified facts, model inferences,
operator assertions, skipped steps, failures, and unresolved risks.

12.7 THE SYSTEM SHALL produce machine-readable JSON and human-readable Markdown
reports from the same canonical run data.

### Requirement 13 — Observability, privacy, and redaction

**User story:** As an operator, I want useful diagnostics without leaking
credentials or sensitive project information.

#### Acceptance criteria

13.1 THE SYSTEM SHALL emit structured local events correlated by run ID,
transition ID, and tool-call ID.

13.2 THE SYSTEM SHALL record durations, retries, policy outcomes, token/model
usage when available, and failure categories without recording secret values.

13.3 THE SYSTEM SHALL redact configured secret fields, environment values,
credentials, tokens, and sensitive connection details before logs or reports are
persisted.

13.4 IF redaction cannot be applied to a sensitive payload, THEN THE SYSTEM
SHALL store only a checksum and minimal metadata or SHALL reject persistence.

13.5 THE FIRST RELEASE SHALL operate with local JSON Lines telemetry and SHALL
expose a stable interface for an optional future OpenTelemetry exporter.

### Requirement 14 — Failure handling and idempotency

**User story:** As an operator, I want failures to stop safely and resume
predictably so that retries do not duplicate harmful side effects.

#### Acceptance criteria

14.1 THE SYSTEM SHALL classify failures as validation, policy, authorization,
timeout, tool, adapter, evidence, integrity, or unexpected runtime failures.

14.2 WHEN a retryable read-only operation fails transiently, THE SYSTEM MAY retry
within the tool contract's bounded retry policy and SHALL record each attempt.

14.3 THE SYSTEM SHALL NOT automatically retry `EXTERNAL_WRITE` or
`HARDWARE_CONTROL` operations.

14.4 WHEN a side-effecting operation has an uncertain outcome, THE SYSTEM SHALL
enter `BLOCKED`, preserve evidence, and require reconciliation before resumption.

14.5 WHEN a tool supports idempotency, THE SYSTEM SHALL reuse the persisted
idempotency key for safe resumption of the same logical operation.

14.6 IF checkpoint or evidence integrity cannot be verified, THEN THE SYSTEM
SHALL refuse automatic resumption.

### Requirement 15 — Testing and evaluation

**User story:** As a maintainer, I want layered tests and repeatable agent evals
so that safety and behavior regressions are found before a real engineering run.

#### Acceptance criteria

15.1 THE TEST SUITE SHALL contain unit tests for typed models, transitions,
authorization, policies, path boundaries, redaction, and tool contracts.

15.2 THE TEST SUITE SHALL contain integration tests for the CLI, persisted
checkpoints, repository adapter, evidence adapter, and at least one offline
platform-adapter path.

15.3 THE TEST SUITE SHALL use property-based tests for state-transition
invariants, path containment, authorization scope, schema round trips, and
redaction invariants.

15.4 THE EVALUATION SUITE SHALL include golden tasks, expected state paths,
required evidence, prohibited tool calls, and pass/fail graders.

15.5 THE EVALUATION SUITE SHALL include adversarial cases for prompt injection,
stale authorization, target ambiguity, protected-source writes, missing
evidence, invalid schemas, and unsupported capabilities.

15.6 AN LLM-AS-JUDGE MAY provide an advisory quality score, but THE RELEASE GATE
SHALL use deterministic assertions for safety, authorization, integrity, and
required evidence.

15.7 WHEN a defect is fixed, THE TEST OR EVALUATION SUITE SHALL add a regression
case demonstrating the previous failure.

### Requirement 16 — CI and repository hardening

**User story:** As a maintainer, I want automated repository checks so that
configuration, secrets, generated schemas, and code quality remain consistent.

#### Acceptance criteria

16.1 THE CI PIPELINE SHALL run formatting/lint checks, type-aware tests, unit and
integration tests, schema generation drift checks, profile validation, policy
validation, and spec-link validation.

16.2 THE CI PIPELINE SHALL scan committed content for secrets and SHALL block
known credentials, private keys, and unapproved environment files.

16.3 THE REPOSITORY SHALL ignore `.env`, environment variants, local run output,
temporary archives, caches, and build artifacts while allowing documented safe
examples such as `.env.example`.

16.4 THE REPOSITORY SHALL define text/binary attributes appropriate for PLC,
HMI, archives, screenshots, and generated text to prevent unsafe line-ending
conversion or meaningless diffs.

16.5 WHEN files outside the change log are staged, THE EXISTING PRE-COMMIT
POLICY SHALL continue to require a new append-only change-log entry.

16.6 THE CI PIPELINE SHALL fail if task requirement references point to missing
requirement IDs.

### Requirement 17 — Provider and model abstraction

**User story:** As an agent designer, I want the model provider behind a narrow
port so that model upgrades do not alter policy, orchestration, or adapter code.

#### Acceptance criteria

17.1 THE SYSTEM SHALL define a provider-neutral model request/response contract
for messages, tool proposals, usage, stop reason, and structured errors.

17.2 THE ORCHESTRATOR SHALL depend on the provider contract rather than a vendor
SDK.

17.3 WHEN a model or prompt version changes, THE EVALUATION SUITE SHALL run the
golden and adversarial cases before the version is accepted.

17.4 IF a provider returns malformed tool input or an unknown tool, THEN THE
SYSTEM SHALL reject the proposal before adapter execution.

17.5 THE FIRST RELEASE SHALL support one production provider implementation and
one deterministic fake provider for tests.

### Requirement 18 — Documentation and contribution workflow

**User story:** As a contributor, I want concise operational documentation and
traceable decisions so that I can implement one task without rediscovering the
architecture.

#### Acceptance criteria

18.1 THE REPOSITORY SHALL document setup, architecture, supported capabilities,
safety boundaries, CLI use, testing, evidence interpretation, and contribution
workflow.

18.2 THE REPOSITORY SHALL maintain architecture decision records for decisions
that change trust boundaries, schema ownership, state transitions, policy,
storage, provider abstraction, or platform integration.

18.3 EACH implementation task in `tasks.md` SHALL reference one or more
acceptance-criterion IDs from this document.

18.4 WHEN a task is completed, THE IMPLEMENTER SHALL update tests,
documentation, generated artifacts, and the mandatory change log as applicable
within the same change set.

18.5 THE REPOSITORY SHALL document which capabilities are implemented,
simulated, disabled, or planned for each platform profile.

### Requirement 19 — First vertical slice and release boundary

**User story:** As a product owner, I want a small end-to-end release so that the
architecture is proven before adding GUI or hardware complexity.

#### Acceptance criteria

19.1 THE FIRST VERTICAL SLICE SHALL execute an offline, repository-only task from
a validated task document through preflight, policy evaluation, evidence
capture, integrity verification, and final reporting.

19.2 THE FIRST VERTICAL SLICE SHALL use the real state machine, policy engine,
tool registry, repository adapter, evidence adapter, CLI, and report renderer.

19.3 THE FIRST VERTICAL SLICE SHALL use a deterministic fake model or a bounded
real-model plan step and SHALL remain executable without launching GX Works2 or
TIA Portal.

19.4 THE FIRST RELEASE SHALL pass all deterministic safety tests, schema checks,
CLI integration tests, and the defined offline eval set.

19.5 THE FIRST RELEASE SHALL document deferred capabilities and SHALL not expose
disabled hardware actions as operational CLI commands.

## Global constraints

- Existing repository safety instructions remain authoritative. This spec may
  make them more precise but may not weaken them.
- No task in this spec is authorization to operate engineering software or PLC
  hardware.
- All filesystem targets must be resolved and checked before write, move, or
  delete operations.
- User-owned dirty worktree changes must be preserved.
- Evidence and logs must avoid secrets and production connection details.

## Requirements completion gate

Requirements are ready for implementation when:

- all acceptance criteria have stable IDs;
- policy owners agree on the safety and authorization boundaries;
- GX Works2 and TIA Portal profile owners confirm protected-source and fixture
  definitions;
- unresolved assumptions are recorded as explicit design decisions or tasks;
- every mandatory requirement is mapped in `design.md` and `tasks.md`.
