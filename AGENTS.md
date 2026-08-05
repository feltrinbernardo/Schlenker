# Schlenker Agent Instructions

## Scope

This repository is a development and evaluation environment for the Schlenker
GX Works2 project. Use the official Computer Use plugin for GX Works2 GUI
actions and ordinary shell/file tools for repository work. The protected source
project remains immutable even when development PLC access is authorized.

## Development-environment safety gate

Before every GX Works2 UI run, obtain a current operator confirmation that every
PLC reachable from this engineering host is a development/test target and that
no communication route can reach production equipment. A confirmation from an
older run is context only and does not satisfy a new run.

- Work only on `fixtures/projects/schlenker-working/schlenker.gxw`.
- Never open or edit `D:\GX Works\schlenker.gxw` through Computer Use.
- Online, Monitor, Read from PLC, and connected diagnostics are permitted only
  when the current task requires them and the visible target identity matches a
  development PLC covered by the current operator confirmation.
- Write to PLC, Transfer, Remote Operation, PLC run/stop/reset, connection
  changes, and device-memory writes require explicit current-run authorization
  naming both the development target and the exact operation. General
  development-environment access is not authorization for these actions.
- If a target is ambiguous, differs from the approved development target, or
  could route to production, stop without approving the action and report the
  visible state.
- For an offline task, opening a project never means Read from PLC and creating
  a deliverable never means Write to PLC or downloading to hardware.

## Computer Use workflow

- Use the installed `computer-use` skill and its official wrapper.
- Select GX Works2 only from objects returned by Computer Use and require one
  unique target window before acting.
- Observe the target window before each action and refresh immediately after
  it. Never reuse coordinates, screenshot IDs, or accessibility indexes after
  state changes.
- Keep unrelated and sensitive applications closed while Computer Use runs.
- Do not use terminal applications or Windows security dialogs through Computer
  Use.

## Windows 10 hybrid capture workflow

The official Computer Use screenshot call is not compatible with Windows 10
build 19045 on this machine. Use `scripts/capture-gxworks2-window.ps1` only as a
read-only visual observation channel while accessibility and keyboard remain the
control channel.

- The same current-run development-environment confirmation is required before
  launching GX Works2 or taking a hybrid capture.
- The helper may capture only the exact installed `GD2.exe`, only while that
  process owns the foreground, and only to `runs/`.
- The helper must refuse the protected original path and any `.gxw` project
  outside `fixtures/projects/`.
- Do not add input, activation, clicking, typing, clipboard, or Windows-key
  behavior to the capture helper.
- Do not treat an external PNG as an official Computer Use screenshot ID or as
  authorization for a blind coordinate action.
- Prefer GX Works2 Guided Editing, menus, and documented keyboard shortcuts.
- If an essential control is graphical-only, stop and record a human checkpoint
  or a separately reviewed integration requirement.

## Test evidence

- Record the original source hash before and after a run.
- Save screenshots, material action summaries, compiler output, and the final
  report under `runs/<run-id>/`.
- For hybrid runs, save the capture manifest alongside every PNG and record that
  the image was external evidence rather than an official Computer Use capture.
- A passing logic test requires the disposable project to compile without
  errors and the original source metadata/hash to remain unchanged.

## Mandatory change-request log

Every user request that causes a repository file to be created, modified,
renamed, or deleted must produce one append-only entry in
`logs/change-log/YYYY-MM-DD.md` before the task is reported complete.

- Use `scripts/write-change-log.ps1`; do not hand-edit an existing entry.
- Record an ISO-8601 local timestamp with timezone, UTC timestamp, request
  summary, result, status, branch, base commit, actor, and affected files.
- Paraphrase the request. Never copy credentials, secrets, production
  connection details, or other sensitive values into the log.
- Use status `partial` or `blocked` when requested work is not fully complete and
  state the reason in `-Notes`.
- Documentation-only, configuration, script, and log-policy changes are still
  changes and must be recorded.
- Read-only investigation does not require an entry unless it is part of a
  request that also changes repository files.
- The tracked pre-commit hook requires a newly added timestamp entry whenever a
  commit stages files outside `logs/change-log/`.
