# Schlenker

Local test harness for an industrial automation agent that engineers and edits
offline Mitsubishi GX Works projects through a desktop-control loop.

## What is in this repository

- `agent/`: the stable system prompt, the per-task brief template, and the
  computer-use contract.
- `profiles/`: target-specific facts such as PLC family, GX Works version,
  programming language, and test I/O mappings.
- `fixtures/`: local offline GX Works sample projects and requirements. Project
  files are deliberately ignored because they may be proprietary.
- `evals/`: repeatable acceptance tasks and their expected outcomes.
- `runs/`: generated screenshots, action traces, build output, and reports.

## Local test loop

1. Choose a profile in `profiles/`.
2. Copy `agent/task-template.md` into a new task brief and fill in its fields.
3. Give the agent the system prompt, selected profile, and task brief.
4. Run the task against an offline fixture in GX Works.
5. Save evidence in `runs/<run-id>/` and assess it against the matching eval.

## Schlenker GX Works2 baseline

The existing project is stored outside the repository at
`D:\GX Works\schlenker.gxw`. Treat it as an immutable source: close it, verify
that GX Works2 is disconnected from physical equipment, and copy it to the
working-copy path in `profiles/schlenker-gxworks2.yaml` before testing.

Use `fixtures/requirements/schlenker-baseline.md` for the first baseline run and
record the result with `agent/run-report-template.md`. Never open the original
for an automated edit or commit a `.gxw` project file.

After completing the human safety gate and closing GX Works2, prepare the
disposable copy with:

```powershell
.\scripts\prepare-schlenker-fixture.ps1 -ConfirmDisconnected
```

The script refuses an open/locked source, refuses to overwrite an existing
working copy, verifies SHA-256 hashes, and writes an ignored local manifest.

## Windows desktop test setup

This project uses the official `computer-use@openai-bundled` plugin to operate
GX Works2. Project-local Codex defaults are in `.codex/config.toml`, while
`AGENTS.md` provides the mandatory offline and source-protection rules.

Before a test:

1. Keep Windows unlocked and leave GX Works2 visible on the active desktop.
2. Confirm no PLC or production equipment is connected for this run.
3. Close unrelated or sensitive applications.
4. Use only the ignored disposable project copy.
5. Apply the GX Works2 safe default/recent-project paths and run the environment
   verifier:

```powershell
.\scripts\configure-gxworks2-safe-paths.ps1
.\scripts\test-desktop-environment.ps1
```

Computer Use is allowed persistently only for the GX Works2 application ID.
Other Windows applications continue to require approval.

Run `evals/cases/000-desktop-smoke.md` successfully before attempting Eval 001
or any logic modification.

On Windows 10 build 19045, GX Works2 accessibility capture is usable but the
current Computer Use screenshot path is not. This machine will remain on its
current Windows version. The replacement workflow combines:

- official Computer Use discovery, accessibility, and keyboard input;
- GX Works2 Guided Editing and documented keyboard shortcuts; and
- a read-only, foreground-only GX Works2 capture helper for visual evidence.

Validate the helper without launching GX Works2:

```powershell
.\scripts\capture-gxworks2-window.ps1 -ValidateOnly
```

After a fresh disconnection confirmation, pass
`evals/cases/000b-windows10-hybrid-proof.md` before Eval 001. External captures
are evidence only and never authorize blind coordinate clicks. See
`logs/2026-08-03-windows-desktop-setup.md` for the rationale and safeguards.

Prepare a unique second disposable copy for that proof with:

```powershell
.\scripts\prepare-gxworks2-hybrid-proof.ps1 `
  -ConfirmDisconnected `
  -RunId <run-id>
```

## Initial model setting

Use `gpt-5.6-sol` with `reasoning.effort: high` as the baseline. Keep the
configuration fixed for the first evaluation batch, then compare alternatives
using the same cases and evidence requirements.

## Change-request audit log

Repository modifications are recorded in append-only daily Markdown files under
`logs/change-log/`. After completing a requested change, create its entry with:

```powershell
.\scripts\write-change-log.ps1 `
  -Request 'Add a reusable project-change audit trail' `
  -Summary 'Added the writer, policy, and pre-commit enforcement' `
  -Files 'AGENTS.md', 'README.md', 'scripts/write-change-log.ps1'
```

Install the tracked Git hook once per clone:

```powershell
.\scripts\install-git-hooks.ps1
```

The hook rejects a commit with staged repository changes unless that commit
also contains a newly added ISO-timestamped entry in
`logs/change-log/YYYY-MM-DD.md`. It does not replace the agent policy: entries
should be written when work is completed, even when no commit is made.

## Safety-policy variants

The active `AGENTS.md` policy supports a development-only PLC environment while
keeping the protected source immutable and requiring exact authorization for
writes, transfers, remote operations, run-state changes, and connection edits.
The previous strictly offline policy is preserved at
`policies/AGENTS.offline-2026-08-03.md`.

Use `profiles/schlenker-gxworks2-development.yaml` together with
`fixtures/requirements/schlenker-commenting-development.md` for the controlled
development commenting workflow. The original profile and commenting brief are
retained as the stricter offline variants.

## External functional references

`fixtures/references/tia-import-rev10/` preserves a user-supplied Siemens TIA
Portal SCL/CSV package for a `Refurbished Monoblocco Schenker`. It is available
only as a provenance-tracked functional reference for inspecting and commenting
the Mitsubishi FX3G ladder project. Read its `INTEGRATION.md` before use.

The package is not executable GX Works2 logic and its Siemens addresses,
hardware, networks, state model, and call order are not Schlenker facts until
they are correlated with the actual `.gxw` and authoritative machine evidence.

## Definition of a passing local test

- The project was changed only as requested.
- The requested program structure and comments exist.
- The project compiles without errors.
- The run record identifies changed artefacts, build result, and open items.

This repository is intentionally an offline-test environment. Do not place live
PLC credentials, production projects, or physical-equipment connection details
in it.
