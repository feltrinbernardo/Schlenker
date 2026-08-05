# Windows Desktop Test Setup Log

## Session

- Date: 2026-08-03
- Timezone: Australia/Sydney
- Repository: `C:\www\Schlenker`
- Branch: `codex/initial-agent-scaffold`
- Pulled scaffold commit reviewed: `472c2b1fe2c20cafb402090809f6e702d396eb7a`

## Pulled-commit review confirmation

All 11 tracked files from the pulled root commit were read before this setup:

- `.gitignore`
- `README.md`
- `agent/system-prompt.md`
- `agent/task-template.md`
- `agent/tools.md`
- `evals/cases/001-cylinder-cycle.md`
- `fixtures/projects/.gitkeep`
- `fixtures/requirements/.gitkeep`
- `profiles/demo-fx5u.yaml`
- `profiles/demo-iq-r.yaml`
- `runs/.gitkeep`

## Environment findings

- Codex CLI `0.146.0` is installed and current.
- The OpenAI Windows Computer Use runtime and native pipe were already present.
- The bundled `computer-use@openai-bundled` plugin was available but not
  installed, which is why no Windows desktop-control skill was exposed to the
  original thread.
- Git `2.55.0.windows.3` is installed and registered in the machine `PATH`; the
  active Codex process predates that PATH update and requires a restart.
- GX Works2 `1.560J` and its `GD2.exe` executable are installed.

## Settings applied

1. Installed and enabled `computer-use@openai-bundled` version `26.727.51351`
   with the Codex plugin CLI.
2. Initialized the plugin through its official wrapper and read the required
   Computer Use guidance and confirmations policy.
3. Verified the runtime can enumerate GX Works2 with application identifier
   `{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}\MELSOFT\GPPW2\GD2.EXE`.
4. Added only that exact GX Works2 identifier to
   `[computer_use.windows].always_allowed_app_ids` in the global Codex config.
5. Added `.codex/config.toml` with the baseline model, high reasoning effort,
   workspace-write sandbox, on-request approvals, and the Computer Use plugin.
6. Added repository-wide `AGENTS.md` instructions that require a current
   disconnection confirmation for every GUI run and forbid all online,
   monitoring, transfer, and PLC-write actions.
7. Added `scripts/test-desktop-environment.ps1` for repeatable environment and
   source/copy integrity checks.
8. Updated `README.md` with the Windows desktop-test procedure.
9. Added `evals/cases/000-desktop-smoke.md` as the repeatable Computer Use
   launch/observe/open/close safety test that must pass before logic work.
10. Updated `agent/tools.md` to bind the generic desktop contract to the
    official plugin and exact GX Works2 application identifier.

## Validation completed

- Official Computer Use runtime initialization: PASS
- GX Works2 app enumeration through Computer Use: PASS
- Global Codex configuration parse: PASS
- Project Codex configuration strict parse: PASS
- Computer Use plugin installed and enabled: PASS
- GX Works2 allow-list entry: PASS
- Git installed and registered for new processes: PASS
- GX Works2 installed: PASS
- Disposable project present: PASS
- Original/disposable SHA-256 equality: PASS
- Repository patch whitespace check: PASS
- Current operator confirmed that the PLC and all production equipment were
  disconnected for this test run: PASS
- Computer Use launched and uniquely targeted GX Works2: PASS
- GX Works2 opened only the disposable project in read-only mode: PASS
- Accessibility state identified FX3G/FX3GC and 614 program steps: PASS
- GX Works2 closed normally: PASS
- Original project hash, length, and modification timestamp after the run: PASS

Verified source/copy SHA-256:
`B16C05F4A555E9E9628BE35AAAA889A21D047AC239DA1D5D9390FC8704A29BD1`.

## Desktop smoke-test record

- Operator confirmation: the PLC and all production equipment were
  disconnected; the run was limited to development tests.
- GX Works2 was launched through Computer Use and one GX Works2 target window
  was selected.
- The GX Works2 default folder and recent-file registry values were changed to
  the disposable working-copy location before the final run.
- The final run opened
  `C:\www\Schlenker\fixtures\projects\schlenker-working\schlenker.gxw` in
  read-only mode.
- Accessibility evidence showed CPU `FX3G/FX3GC`, program `MAIN`, and
  `614 Step`.
- No Online, Monitor, Transfer, Read/Write PLC, remote-operation, connection,
  or project-edit command was invoked.
- GX Works2 was closed normally and Computer Use subsequently reported it as
  not running.
- Full evidence is in
  `runs/20260803-140335-schlenker-desktop-smoke/`.

During the first file-dialog attempt, GX Works2 still used its old recent-file
value and opened `D:\GX Works\schlenker.gxw` read-only. The unexpected original
path was detected in accessibility state; only the language notice was
dismissed, GX Works2 was closed immediately, and no editing or communication
action occurred. The original file's hash, length, and timestamp were unchanged.
The safe-path configuration script and verifier were then added before the
successful final run.

## Known compatibility limitation

Computer Use accessibility capture and keyboard input work with GX Works2 on
this machine. Screenshot capture fails with `0x80004002` at
`GraphicsCaptureSession.IsBorderRequired`. This machine runs Windows 10 build
19045, while that API is documented for build 20348 or newer. The smoke test
therefore passed using accessibility evidence, but coordinate-based desktop
work cannot use the official screenshot-ID path on this machine.

## Workflow decision: retain Windows 10

The operator decided that the Windows version will not be changed. The previous
workflow treated an operating-system upgrade or a future Computer Use fallback
as a prerequisite for visual GX Works2 work. That dependency has been removed.

The replacement workflow uses three deliberately separate channels:

1. Official Computer Use discovers GX Works2 and provides accessibility state
   and keyboard input.
2. GX Works2 Guided Editing and documented shortcuts provide deterministic
   ladder navigation and offline Build/Rebuild operations.
3. `scripts/capture-gxworks2-window.ps1` captures visual evidence without any
   input capability.

The capture helper is restricted to the exact installed `GD2.exe`, requires GX
Works2 to own the foreground, refuses the protected original-project path and
projects outside `fixtures/projects/`, and writes only beneath `runs/`. Its PNGs
are external evidence, not official Computer Use screenshot IDs, and therefore
must not be used to authorize blind coordinate clicks.

This change is preferable for the current machine because it preserves the
installed Windows/MELSOFT environment while addressing the missing visual
observation channel. It does not claim that every GX Works2 control is now
automatable. Eval 000b must prove Guided Editing, capture quality, and offline
build evidence on a second disposable copy. Any essential mouse-only control
remains a stop-and-review condition.

## Hybrid workflow implementation

- Added `scripts/capture-gxworks2-window.ps1`.
- Added a validate-only preflight and integrated it into
  `scripts/test-desktop-environment.ps1`.
- Added capture-helper self-tests for unsafe project titles and added
  `scripts/prepare-gxworks2-hybrid-proof.ps1` so the proof uses a unique second
  disposable copy.
- Extended `scripts/configure-gxworks2-safe-paths.ps1` to accept only a `.gxw`
  project located beneath `fixtures/projects/`.
- Added `evals/cases/000b-windows10-hybrid-proof.md`.
- Updated `AGENTS.md`, `agent/tools.md`, and `README.md` with the observation,
  input, path, and evidence boundaries.

## Hybrid preflight validation

The following checks were completed without launching GX Works2:

- PowerShell parser validation for all five repository scripts: PASS
- Capture-helper validate-only mode: PASS
- Capture-helper unsafe output-directory rejection: PASS
- Capture-helper protected/unsafe project-title rejection: PASS
- Capture-helper refusal while GX Works2 is closed, with no output created: PASS
- Proof-copy preparation disconnection gate: PASS
- GX Works2 safe-path rejection outside `fixtures/projects/`: PASS
- GX Works2 normal disposable default/recent path restored: PASS
- Desktop environment verifier: PASS, 13 checks
- Original/disposable SHA-256 equality after implementation: PASS

No Windows version, MELSOFT installation, PLC connection setting, or production
setting was changed during this implementation. GX Works2 remained closed. The
live capture quality and keyboard-guided proof remain pending because they
require a new current-run operator disconnection confirmation.

## Remaining local action

- Restart the Codex desktop app before a new session so it inherits the updated
  machine `PATH` and loads the installed Computer Use plugin from startup.
- Obtain a fresh operator disconnection confirmation and run Eval 000b on a
  second disposable proof copy.

## Live hybrid project-opening test

Run `20260803-145051-schlenker-hybrid-review` was performed after the operator
confirmed that the PLC and all production equipment were disconnected and the
scope was development testing only.

- Prepared a unique second disposable proof copy whose SHA-256 matched the
  protected original and normal working copy.
- Configured GX Works2 to use only that proof copy, launched it through official
  Computer Use, and opened it read-only.
- Verified in the GX Works2 interface: `FX3G/FX3GC`, ladder program `MAIN`, 614
  steps, and the Structured Project navigation areas.
- Captured readable visual evidence of the project tree and first ladder page
  using the observation-only helper.
- GX Works2 displayed a language mismatch between its selected language and the
  Windows language for non-Unicode programs; the notice was dismissed without
  changing either setting.
- `Build F4` was disabled in the read-only editor. `Rebuild All` returned without
  an error dialog, but no compile-result view or error/warning counts were
  exposed, so the baseline build is not recorded as certified.
- Closed GX Works2 normally, restored its default/recent path to the normal
  disposable working copy, and verified that the original, working copy, and
  proof copy retained the same SHA-256, length, and timestamp.
- No Online, Monitor, Transfer, Read/Write PLC, remote-operation, connection,
  project-save, or material editing action was used.

The capture helper initially refused the opened project because GX Works2
abbreviated the long proof-copy path in its window title. The guard was amended
to accept an abbreviated title only when its suffix matches the exact current
GX Works2 recent-file registry value, that value exists below
`fixtures/projects/`, and no protected-source path is present. Parser validation
and protected, external, and mismatched-truncated-title rejection tests pass.

The project summary is in
`logs/2026-08-03-schlenker-project-summary.md`; detailed evidence is under
`runs/20260803-145051-schlenker-hybrid-review/`.
