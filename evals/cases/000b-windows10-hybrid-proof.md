# Eval 000b - Windows 10 Hybrid GX Works2 Proof

## Purpose

Prove that the Windows 10 workflow can combine GX Works2-only external captures
with official Computer Use accessibility and keyboard control before any logic
evaluation or project edit is attempted.

## Preconditions

- A person has confirmed for this specific run that no PLC or production
  equipment is connected.
- Eval 000 has passed.
- `scripts/test-desktop-environment.ps1` passes.
- A second disposable proof copy has been created with
  `scripts/prepare-gxworks2-hybrid-proof.ps1`.
- GX Works2 is closed before the run.
- Unrelated and sensitive applications are closed.

## Procedure

1. Record the original, normal working-copy, and proof-copy hashes and metadata.
2. Configure GX Works2 recent/default paths to the proof copy by passing its
   path to `scripts/configure-gxworks2-safe-paths.ps1 -ProjectPath`.
3. Launch GX Works2 with official Computer Use and require one returned target.
4. Open only the proof copy and confirm its path in accessibility state.
5. Keep GX Works2 in the foreground and run
   `scripts/capture-gxworks2-window.ps1` with an output directory under the
   current run.
6. Inspect the saved image and manifest before acting.
7. Use accessibility and keyboard only to enter Guided Editing, invoke one
   harmless element-entry flow, and cancel it without changing logic.
8. Capture and inspect the resulting GX Works2 state.
9. Run an offline Build with `F4`; never use Online Program Change.
10. Record the Output window text and capture the build result.
11. Close GX Works2 normally without saving a material logic change.
12. Verify all protected hashes and write the run report.

## Pass criteria

- The helper refuses validation output outside `runs/`.
- The helper captures only the exact `GD2.exe` process while GX Works2 owns the
  foreground.
- The helper refuses a title containing the protected original-project path or
  a `.gxw` path outside `fixtures/projects/`.
- Saved PNG files are readable and show the expected GX Works2 state.
- Accessibility and keyboard navigation reach and cancel the Guided Editing
  element-entry flow.
- Offline Build completes and its result is recorded.
- No external screenshot is used as authorization for a blind coordinate click.
- No Online, Monitor, Transfer, Read/Write PLC, remote-operation, or connection
  action is used.
- GX Works2 closes normally.
- Original and normal working-copy hashes and metadata remain unchanged.

## Escalation rule

If an essential control cannot be reached through accessibility or keyboard,
stop. Do not add coordinate input to the capture helper. Record the control and
decide separately whether a human checkpoint, isolated VM, or reviewed custom
adapter is appropriate.
