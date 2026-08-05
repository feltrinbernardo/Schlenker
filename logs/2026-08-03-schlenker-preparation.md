# Schlenker Preparation Log

## Session

- Date: 2026-08-03
- Timezone: Australia/Sydney
- Repository: `C:\www\Schlenker`
- Branch: `codex/initial-agent-scaffold`
- Remote: `https://github.com/feltrinbernardo/Schlenker.git`

## Work completed

1. Verified Git for Windows `2.55.0.windows.3` is installed. The active Codex
   host process had inherited an older `PATH`, so Git was invoked through
   `C:\Program Files\Git\cmd\git.exe`.
2. Fetched the remote repository, created the local tracking branch
   `codex/initial-agent-scaffold`, and ran a fast-forward-only pull.
3. Confirmed the branch is current at commit
   `472c2b1fe2c20cafb402090809f6e702d396eb7a`.
4. Reviewed all files in the initial industrial automation agent scaffold.
5. Located the supplied source project at `D:\GX Works\schlenker.gxw`.
6. Verified `.gxw` is registered to GX Works2 and that GX Works2 `1.560J`
   (`GD2.exe` version `1.560.0.1`) is installed.
7. Read the open window metadata without sending UI input. It showed the
   `MAIN` ladder program, a size of 614 steps, and status `Monitor Stopping
   (Read Only)`.
8. Added `profiles/schlenker-gxworks2.yaml` containing only verified facts and
   explicit placeholders for the unresolved PLC family and CPU.
9. Added `fixtures/requirements/schlenker-baseline.md` defining the safety gate,
   disposable-copy workflow, baseline build, evidence, and source-integrity
   checks.
10. Added `agent/run-report-template.md` for repeatable run evidence.
11. Updated `README.md` with the Schlenker GX Works2 baseline workflow.
12. Created the ignored local working-copy directory
    `fixtures/projects/schlenker-working/`.
13. Added `scripts/prepare-schlenker-fixture.ps1`, a guarded copy utility that
    requires explicit disconnection confirmation, refuses an open source or an
    existing destination, restricts the destination to the intended fixture
    directory, verifies hashes and original metadata, and produces a local
    fixture manifest.

## Safety and source integrity

- No input was sent to GX Works2.
- No PLC communication, monitoring command, write, or download was attempted.
- The original `D:\GX Works\schlenker.gxw` was not modified.
- A disposable copy has not yet been created because GX Works2 currently holds
  an exclusive lock on the source project.

## Required user action

Confirm GX Works2 is disconnected from all physical PLC communication paths and
that its current read-only session can be closed safely, then close GX Works2.
After it is closed, the baseline workflow can record the source hash, create the
disposable copy, inspect the project CPU, and perform an offline baseline build.

The same exclusive lock was confirmed on three consecutive goal turns. Work is
therefore paused at the safety gate rather than terminating GX Works2 or
asserting physical disconnection without operator confirmation.

The operator subsequently confirmed that the PLC is disconnected, nothing is
connected to production, and the current scope is development testing. This
confirmation was recorded in `profiles/schlenker-gxworks2.yaml`.

A normal `CloseMainWindow` request was sent only to the GX Works2 process whose
title contains `D:\GX Works\schlenker.gxw`. GX Works2 accepted the request but
did not exit within ten seconds. No accessible confirmation dialog appeared.
The process was not terminated or force-killed, and the source remains locked;
manual closure is still required.

The operator confirmed GX Works2 was closed. The source lock was gone, and
`scripts/prepare-schlenker-fixture.ps1 -ConfirmDisconnected` completed at
2026-08-03T13:19:55.9760187+10:00. It created the ignored disposable copy and
manifest under `fixtures/projects/schlenker-working/`.

The post-close source and working copy both have SHA-256
`B16C05F4A555E9E9628BE35AAAA889A21D047AC239DA1D5D9390FC8704A29BD1` and length
1,323,520 bytes. The source remained unchanged throughout the scripted copy.

GX Works2 changed the source while it was being closed manually, before the
preparation script began: the earlier read-only observation was 1,020,416 bytes
with modification time 2026-07-04 18:47:19 local, while the post-close baseline
is 1,323,520 bytes with modification time 2026-08-03 13:19:21.346 local. No
pre-close hash could be recorded because GX Works2 held an exclusive lock.

Read-only inspection of the disposable copy identified:

- controller family `MELSEC-F` and CPU `FX3G`;
- a GX Works2 Structured Project, evidenced by its internal Program File, Task,
  and POU records;
- ladder program `MAIN`, observed at 614 steps;
- saved connection configuration is present inside the project, but its use is
  forbidden by the offline profile and its physical details were not copied
  into tracked repository files.

## Validation performed

- Git working-tree and branch status inspected before changes.
- Repository integrity checked with `git fsck --full`.
- Patch whitespace checked with `git diff --check`.
- Original source path, file size, timestamp, file association, installed IDE,
  executable version, and open-window state inspected read-only.
- The fixture preparation script passed PowerShell parser validation and its
  disconnection-confirmation refusal path passed without touching the source.
- The post-copy integrity audit confirmed identical source, working-copy, and
  manifest SHA-256 values and identical source/copy lengths.
- A second preparation attempt passed the existing-destination refusal test and
  did not overwrite the disposable copy.
- `git check-ignore` confirmed both the working `.gxw` file and its local
  `fixture-manifest.json` are excluded from version control.
- A final `git diff --check` completed without patch whitespace errors.
