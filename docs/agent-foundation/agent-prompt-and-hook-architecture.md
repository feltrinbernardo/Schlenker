# Agent Prompt and Lifecycle-Hook Architecture

Date: 2026-09-22  
Status: implemented for the repository Codex harness

## Effective instruction hierarchy

```text
Codex product system/developer instructions
  -> root and applicable nested AGENTS.md
  -> versioned role/working-method prompt
  -> validated platform profile
  -> immutable task brief and acceptance checks
  -> state-specific tool contracts
  -> tool output and project content (untrusted)
```

Current Codex automatically loads `AGENTS.md`; it does not automatically load
`agent/system-prompt.md` merely because of its filename. The root policy now
contains the concise active role. `agent/system-prompt.md` is the versioned
role/working-method asset for explicit use by a harness and the planned
standalone runtime.

Neither a role prompt, profile, hook message, successful login, nor historical
operator confirmation grants a capability.

## Computer Use cadence

```text
routine:  accessibility check -> <=5 reversible actions -> accessibility check
material: one transition -> refresh -> visual evidence when material/required
critical: pre-check -> one action -> post-check -> required evidence
```

Accessibility observation answers structured state questions. Visual
observation answers materially graphical questions. Evidence capture persists a
review artifact. They are deliberately separate so routine keystrokes do not
consume a full image turn while critical target and hardware boundaries retain
individual verification.

## Lifecycle flow

```text
UserPromptSubmit -> compact advisory judge context
                         |
                         v
PreToolUse -> deterministic input/path/batch checks -> supported tool
                                                        |
                                                        v
PostToolUse -> output/integrity check -> session ledger
       |
       +-> async sanitized telemetry

Stop -> change-log + policy-surface validation checks -> completion

PreCompact -> exact transcript bytes -> local SQLite -> optional HTTPS archive
SessionStart(compact) -> bounded archive receipt -> continued model context
```

### Hook responsibilities

| Hook | Responsibility | Must not do |
|---|---|---|
| `UserPromptSubmit` | Prompt quality and explicit bypass filter | Grant capability or current authorization |
| `PreToolUse` | Deny determinable unsafe paths, patches, shell launches, and batches before execution | Call an LLM, capture a screen, or infer authorization |
| `PermissionRequest` | Deny approval that would override immutable boundaries; allow only exact offline validation commands | Auto-approve hardware operations |
| `PostToolUse` | Validate results, update the session ledger, surface reconciliation needs | Claim to undo a completed side effect |
| asynchronous post hook | Digest-only local telemetry | Influence the current decision |
| `Stop` | Require change logging and policy tests for hook-observed changes | Replace independent review or compile evidence |
| `PreCompact` | Atomically archive the transcript exposed by Codex before manual or automatic compaction | Parse an unstable transcript schema or claim access to hidden reasoning |
| compact `SessionStart` | Add a bounded snapshot receipt after compaction | Reinject the archived transcript and defeat compaction |

The wired matchers are deliberately narrow: `Bash`, `apply_patch`, and the
verified local Computer Use MCP entry point `mcp__cua_repl__js`. Future adapter
tools require an explicit matcher, schema, test, and policy review.

## Deterministic controls

The lifecycle hook denies:

- patch targets outside this repository;
- recognized shell write targets outside this repository;
- direct patching of native engineering projects;
- mutation of the protected GX Works2 source;
- direct edits to the append-only change log;
- capture output outside `runs/` or a TIA capture project without the required
  `DeployWorking` marker and `.ap19` suffix;
- broad destructive home/root deletion;
- shell launch of GX Works2 or TIA Portal;
- Computer Use batches above five detected routine actions; and
- multi-action batches containing a detected material/critical operation.

State-dependent operations receive a concise checkpoint rather than false
authorization. The agent must still prove exact target identity, current
confirmation, capability, and authorization through the primary policy.

## Local state and privacy

Generated lifecycle data is stored under ignored `logs/hooks/`:

- daily JSONL telemetry contains matcher, event, decision/reason, sync mode,
  timeout, response size, action/capture-operation counts, hook duration, and
  an input digest, not raw command text;
- per-session ledgers contain changed repository paths, completed checks, and
  stable issue codes; and
- no prompt text, credentials, PLC addresses, screenshots, or raw tool output
  is recorded by the lifecycle telemetry handler.

## Model settings

- Temperature remains unset.
- The prompt judge uses low reasoning effort.
- Medium reasoning is appropriate for routine repository/documentation work.
- High reasoning is appropriate for PLC/HMI logic, safety, complex diagnostics,
  architecture, policy changes, and final engineering review.

The project retains high as its current default because most engineering tasks
are safety-sensitive. Reasoning effort is a quality/latency choice, not an
authorization control.

## Verification and remaining gate

Run:

```powershell
.\scripts\test-prompt-judge.ps1
.\scripts\test-repository.ps1
```

The offline suite exercises prompt compaction, path containment, protected
sources, append-only logging, routine batch limits, critical de-batching,
permission behavior, post-tool failure handling, sanitized telemetry, and Stop
completion checks.

No engineering application or PLC is needed for those tests. Actual screenshot,
token, and end-to-end UI latency improvement must be measured in a separately
authorized run using the same fixture/task before and after the policy change.

After changing `.codex/hooks.json` or a hook script, review and trust the new
hook definition through Codex `/hooks`. Never bypass the trust review in the
routine workflow.
