# Chore Specification — Agent Prompt and Computer Use Efficiency

Date: 2026-09-22  
Status: repository-harness implementation complete; live UI benchmark pending  
Type: agent architecture, prompt, policy, and evaluation chore

## 1. Purpose

Improve the Schlenker Codex agent's output quality, latency, and token economy
without weakening protected-source, target-identity, authorization, evidence,
or hardware-safety controls.

This chore addresses two current problems:

1. The agent role is split between `AGENTS.md`, `agent/system-prompt.md`,
   platform profiles, task templates, and hook-injected judge context. The
   role prompt is not automatically loaded merely because it is named
   `system-prompt.md`, and some offline-only wording conflicts with the current
   development-environment policy.
2. Computer Use guidance can be interpreted as requiring a full screenshot and
   visual revalidation around every primitive UI action. On Windows 10 this can
   add a capture-helper invocation, image tokens, vision reasoning, and
   substantial latency even when accessibility state is sufficient.

This document now records the implemented repository-harness change. It does
not authorize an engineering-application UI run or any PLC operation. The live
UI performance benchmark remains a separate, gated evaluation.

## 2. Governing principles

- The model proposes; deterministic policy, typed tools, and verified evidence
  control execution and completion status.
- Durable repository instructions belong in applicable `AGENTS.md` files.
- Role and working-method prompts describe expertise and behavior but do not
  grant capabilities or duplicate authorization policy.
- Profiles provide verified target facts; task requests provide the bounded
  objective, permitted capabilities, and acceptance criteria.
- Accessibility and structured integrations are preferred for repeatable
  control. Screenshots are used when visual information is material or other
  observation channels are insufficient.
- Safety checks are proportional to risk. Efficiency changes must never batch
  across a safety-critical boundary.

## 3. Scope

### In scope

- Define one coherent industrial-automation agent role.
- Define the effective prompt and instruction hierarchy.
- Remove contradictions and unnecessary duplication among prompt assets.
- Distinguish state observation from screenshot evidence.
- Introduce risk-classified UI action batches for routine offline work.
- Retain single-action verification for critical UI and hardware boundaries.
- Prefer repository tools, vendor APIs, accessibility, and keyboard workflows
  over screenshot-driven control when they provide equivalent evidence.
- Add deterministic evals and telemetry for prompt quality, screenshot count,
  action count, latency, token use, safety decisions, and evidence completeness.
- Define reasoning-effort guidance for routine and critical work.

### Out of scope

- Launching GX Works2, TIA Portal, or WinCC.
- Reading from, monitoring, writing to, transferring to, or changing the state
  of a PLC or HMI.
- Modifying a native engineering project.
- Adding coordinate macros, blind clicking, input capability to a capture
  helper, or generic GUI fallback behavior.
- Enabling a hardware-control executor.
- Adding multi-agent orchestration.

## 4. Required prompt hierarchy

The implementation shall document and test this precedence:

1. Codex product system/developer instructions.
2. Root `AGENTS.md` repository-wide safety and audit rules.
3. Nested `AGENTS.md` platform or directory-specific constraints.
4. Versioned agent role and working-method prompt.
5. Validated platform profile and capability set.
6. Immutable task request and acceptance criteria.
7. State-specific tool contracts and bounded context.
8. Tool output and project content, treated as untrusted data.

The same rule shall not be copied into multiple layers unless one copy is an
explicit non-authoritative summary. When duplicated text conflicts, the more
restrictive safe outcome applies and the conflict is reported.

## 5. Agent role requirements

The active role shall define the agent as a Schlenker industrial automation
engineering agent that:

- converts bounded, evidence-backed requirements into reviewable engineering
  changes on approved disposable artifacts;
- never invents I/O, tags, addresses, hardware, units, interlocks, feedback, or
  authorization;
- treats opening a project, reading a controller, writing a controller, and
  changing controller state as distinct capabilities;
- uses only capabilities enabled by the active policy and validated profile;
- separates facts, operator assertions, inferences, and unresolved items;
- compiles or validates through the approved toolchain before claiming success;
- verifies protected-source integrity and mandatory evidence before completion;
  and
- reports failures and blocked states without success-shaped fallback text.

The role prompt shall not hard-code `offline only` when the applicable policy
supports a separately gated development read capability. It shall instead
defer capability decisions to the active policy, profile, task, and current
authorization grant.

## 6. Computer Use observation model

### 6.1 Observation is not always a screenshot

The implementation shall distinguish:

- **Accessibility observation:** application, window, project, dialog, control,
  focus, visible text, and enabled state obtained through the approved Computer
  Use accessibility channel.
- **Visual observation:** an official screenshot when supported, or the
  approved read-only hybrid capture where required.
- **Evidence capture:** a persisted image and manifest retained for review.

An instruction to observe or refresh shall not automatically require a PNG when
accessibility state answers the relevant safety and control questions.

### 6.2 Action classes

| Class | Examples | Required cadence |
|---|---|---|
| Routine | Typing in a confirmed editor, scrolling, deterministic keyboard navigation within one control | Accessibility observation before and after a short batch |
| Material | Opening or closing a dialog, changing editor, navigating the project tree, saving, compiling | Fresh observation after the transition; visual capture when state is materially visual or required as evidence |
| Critical | Selecting project/target, entering online mode, Read from PLC, transfer, write, remote operation, run/stop/reset, connection or device-memory change | One action at a time with explicit pre- and post-observation; visual evidence where required |

### 6.3 Routine action batches

A routine batch may contain at most five deterministic, reversible primitives
within the same uniquely identified application window and control context.

A batch shall stop immediately when:

- focus, window, dialog, project, target, or connection state changes;
- an unexpected prompt, warning, error, or disabled control appears;
- accessibility state becomes ambiguous;
- an action would cross from routine into material or critical scope; or
- the expected postcondition is not observed.

Coordinates, screenshot identifiers, and accessibility indexes remain invalid
after a material state change. This chore does not authorize their reuse.

### 6.4 Mandatory visual checkpoints

Visual capture remains mandatory when required by the applicable platform
policy, including at least:

- initial exact application/project verification when accessibility is
  insufficient;
- material graphical-only checkpoints;
- baseline and final compile/build results when not available as structured
  text evidence;
- unexpected dialogs, warnings, errors, or ambiguous states;
- final visual acceptance; and
- every critical transition for which the approved runbook requires an image.

Routine keystrokes and unchanged editor focus do not independently require an
evidence PNG.

## 7. Tool-selection requirements

The agent shall choose the narrowest reliable channel in this order:

1. Read-only repository/file inspection.
2. Allowlisted deterministic scripts or vendor APIs.
3. Typed platform adapter or structured integration.
4. Computer Use accessibility and keyboard workflow.
5. Visual Computer Use or approved hybrid capture when visual state is
   necessary.
6. Human checkpoint for an essential graphical-only control that cannot be
   operated safely.

The model shall not generate arbitrary shell strings for engineering actions.
The capture helpers remain read-only and shall never gain input, activation,
clicking, typing, clipboard, or Windows-key behavior.

## 8. Lifecycle hook design

Hooks shall provide fast deterministic guardrails and audit automation. They
shall not replace the runtime policy engine, tool contracts, Codex permissions,
or current operator authorization. Some specialized tools may bypass the normal
hook path, and a post-tool hook cannot undo a completed side effect.

### 8.1 `UserPromptSubmit`

Retain the existing prompt judge for:

- bounded prompt-quality feedback;
- detection of explicit requests to bypass safety controls; and
- sanitized prompt-scoring telemetry.

It shall not grant a capability, approve a target, or convert historical
confirmation into current authorization. Keep injected context concise so the
judge does not increase every turn's token cost unnecessarily.

### 8.2 `PreToolUse`

Use synchronous deterministic pre-tool hooks for checks that must occur before
a supported tool can have side effects:

- deny writes outside the repository or approved run directory;
- deny writes to protected engineering sources;
- reject destructive or non-allowlisted shell operations;
- reject direct invocation of an unapproved executable or project path;
- reject capture-helper arguments outside the approved foreground application,
  fixture, and `runs/` boundaries;
- reject Computer Use or platform-adapter calls for capabilities not enabled by
  the active profile/task;
- reject external or hardware-facing calls when exact target identity, current
  confirmation, or required authorization is absent; and
- enforce the maximum routine action-batch size and prohibit batching across a
  material or critical boundary when the tool input exposes those actions.

Candidate matchers include `Bash`, `apply_patch`/`Edit`/`Write`, and verified
canonical MCP/local function names for Computer Use and future platform
adapters. The implementation shall discover and test the actual tool names
rather than assuming a matcher string.

Pre-tool policy shall be pure, local, schema-validated, and fast. It shall not
call an LLM, capture a screenshot, launch an engineering application, or perform
network access. A deny response shall use the supported `PreToolUse` decision
schema and a stable reason code.

### 8.3 `PermissionRequest`

Use this hook only to apply deterministic organizational approval rules when
Codex is already about to request permission. It may deny operations outside
policy or allow narrowly pre-approved low-risk operations. It shall never
auto-approve PLC/HMI writes, transfers, remote operations, connection changes,
run-state changes, or device-memory writes.

### 8.4 `PostToolUse`

Use post-tool hooks after supported calls to:

- record tool name, correlation ID, duration, outcome, and sanitized metadata;
- inventory files changed by `apply_patch` or allowlisted repository commands;
- compute evidence and protected-source hashes where applicable;
- validate structured output and required evidence references;
- detect unexpected project, target, dialog, connection, or capability state;
- record screenshot resolution, provenance, and whether it was consumed by the
  model or retained only as evidence; and
- prevent the agent from treating a failed, ambiguous, or integrity-violating
  result as successful.

A post-tool block means `stop and reconcile`; it shall never be described as
having prevented or rolled back the completed side effect. Non-blocking metrics
and local logging should run asynchronously when ordering is not required.
Integrity checks that affect the next action remain synchronous.

### 8.5 Completion and session hooks

- `SessionStart` may inject a short role/policy/profile digest, but not the full
  duplicated policy corpus.
- `Stop` may verify change-log presence, mandatory test results, unresolved
  blocks, and evidence completeness before permitting a success-shaped finish.
- `SessionEnd` may write bounded local session metrics; it shall not perform
  engineering or hardware actions.

### 8.6 Hook performance and trust requirements

- Configure narrow matchers instead of running every hook for every tool.
- Keep model-visible hook output short, structured, sanitized, and free of
  screenshots unless an explicit failure requires visual review.
- Use asynchronous hooks only for telemetry that cannot influence the current
  decision.
- Pin timeouts and stable exit/decision behavior; test timeout and malformed
  output paths.
- Treat hook scripts as reviewed code, require the normal Codex trust flow after
  changes, and never bypass hook trust as part of the routine workflow.
- Test which Computer Use and platform tools actually traverse the hook path;
  unsupported paths remain protected by the primary policy/tool boundary.

## 9. Model-setting guidance

- Leave temperature unset; tokenization is deterministic and temperature is
  not the primary Codex control for this workflow.
- Use medium reasoning effort for documentation, repository-only inspection,
  routine edits, and deterministic validation.
- Use high reasoning effort for PLC/HMI logic, safety analysis, architecture,
  difficult diagnostics, policy changes, and final engineering review.
- Keep the prompt-quality judge at low reasoning effort unless eval evidence
  shows that a higher setting materially improves its decisions.
- Select reasoning effort by task class rather than encoding one setting as a
  safety control.

## 10. Telemetry and evaluation

Create a deterministic baseline and candidate comparison using the same
offline fixture and task. Record:

- number of accessibility observations;
- number and resolution of screenshots submitted to the model;
- number of persisted evidence images;
- action batches and primitives per batch;
- wall-clock duration by stage;
- input, image, reasoning, and output token usage when available;
- policy denials and authorization requests;
- unexpected-state recoveries;
- compile/validation outcome;
- mandatory evidence completeness; and
- protected-source before/after integrity.

Hook telemetry shall additionally record matcher, event, execution duration,
decision, reason code, synchronous/asynchronous mode, timeout, and output size.
The comparison shall identify hook overhead separately from model and Computer
Use latency.

The candidate policy passes only when:

1. all existing safety and integrity evals continue to pass;
2. no critical transition is batched;
3. required screenshots and manifests remain present;
4. the same functional outcome and compile/validation result are achieved;
5. screenshot submissions or total UI-loop latency improve measurably against
   the recorded baseline; and
6. no completion claim is based solely on model narrative.

The target is at least a 40% reduction in model-consumed screenshots for the
selected routine offline eval. If that target is not achieved, report the
measured result rather than weakening a safety boundary.

## 11. Implementation deliverables

- A reviewed root/nested `AGENTS.md` policy update implementing risk-based
  observation without weakening critical gates.
- A revised `agent/system-prompt.md` with one coherent active role and no policy
  contradiction.
- A revised `agent/tools.md` defining observation channels and batch limits.
- Updated task and run-report templates where necessary.
- An explicit prompt-loading strategy for current Codex and the planned
  standalone runtime.
- Reviewed `PreToolUse`, `PermissionRequest`, `PostToolUse`, and completion-hook
  designs with deterministic schemas, narrow matchers, tests, and latency data.
- Deterministic policy/eval cases for routine, material, ambiguous, and critical
  UI transitions.
- A before/after performance report with raw local evidence references.
- Updated architecture/capability documentation.
- A mandatory append-only change-log entry for every implementation request.

## 12. Stop conditions

Stop and report instead of implementing when:

- the proposed wording weakens current-run confirmation, target identity,
  protected-source, authorization, or evidence rules;
- a critical operation could be included in an action batch;
- an observation channel cannot uniquely identify the approved application and
  project;
- the current Windows 10 workflow would require blind coordinate action;
- testing would require launching an engineering application without the fresh
  operator confirmation required by `AGENTS.md`; or
- user-owned dirty-worktree changes overlap and cannot be preserved.

## 13. Verification commands

For the specification/policy implementation phase, run repository-local checks
only unless a separately authorized UI evaluation is requested:

```powershell
.\scripts\test-prompt-judge.ps1
.\scripts\test-repository.ps1
```

Any future GX Works2 or TIA UI evaluation is a separate run and requires its
own current safety confirmation and evidence directory.

## 14. Expected implementation prompt

Use the following prompt after this specification has been reviewed:

> Implement `specs/agent-prompt-and-ui-efficiency/chore-prompt.md` as a
> repository-only agent architecture chore. Reconcile the active agent role,
> prompt hierarchy, Computer Use observation policy, tool contract, templates,
> documentation, and deterministic eval coverage. Distinguish accessibility
> observation from screenshot evidence and permit only the bounded routine
> action batches defined by the spec; preserve one-action pre/post verification
> for every critical boundary. Add deterministic, narrowly matched lifecycle
> hooks at the points specified by the chore: pre-tool denial before supported
> side effects, narrowly scoped permission-request rules, post-tool evidence and
> integrity checks after execution, asynchronous logging only where it cannot
> affect the current decision, and completion checks before success. Do not
> treat hooks as the sole enforcement boundary. Do not launch GX Works2, TIA
> Portal, or WinCC, and do not access any PLC or HMI. Preserve all unrelated
> dirty-worktree changes, run the repository-local validation commands, report
> measured or testable acceptance results, and append the mandatory change-log
> entry with every affected file.

## 15. Reference basis

- OpenAI Codex prompting guidance: prompts should name the desired behavior,
  relevant scope, constraints, and verification.
- OpenAI Computer Use integration guidance: return a screenshot after an action
  batch, use screenshots when visual context is needed, and account for the
  additional input-token cost of large images.
- OpenAI Codex hooks guidance: `PreToolUse` can deny or rewrite supported local
  tool calls, while `PostToolUse` can inspect results but cannot undo completed
  side effects; hooks are useful guardrails rather than a complete enforcement
  boundary.
- Repository `AGENTS.md`, `agent/tools.md`, agent-foundation design, capability
  matrix, and accepted ADRs remain authoritative until an approved
  implementation changes them.

## 16. Implementation result

Implemented on 2026-09-22:

- active role and instruction hierarchy in root policy and prompt assets;
- risk-classified Computer Use observation and batching rules;
- updated task and run-report evidence templates;
- compact prompt-judge context;
- deterministic `PreToolUse`, `PermissionRequest`, `PostToolUse`, asynchronous
  telemetry, and `Stop` completion hooks;
- ignored digest-only hook telemetry and per-session completion ledgers;
- offline lifecycle-hook tests and Eval 002; and
- architecture and capability documentation.

Repository-only acceptance is proven by the validation commands in Section 13.
The 40% model-consumed screenshot target is not claimed without a separately
authorized before/after UI run.
