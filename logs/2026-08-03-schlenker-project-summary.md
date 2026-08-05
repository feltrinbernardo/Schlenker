# Schlenker GX Works2 Project Summary

## Executive result

The Schlenker project opens successfully in GX Works2 1.560J on this Windows 10
development machine. The test used a uniquely named disposable proof copy and
kept the program read-only. No PLC, production equipment, Online mode, Monitor,
Transfer, Read/Write PLC, remote operation, or connection-setting action was
used.

The project is ready for continued offline inspection and carefully controlled
development on disposable copies. It is not yet certified as build-clean: the
normal `Build` command was disabled while `MAIN` was read-only, and `Rebuild All`
returned without an error dialog but did not expose a compiler-results view.

## Verified project identity

- Product: MELSOFT GX Works2 1.560J (`GD2.exe` 1.560.0.1)
- Project format: GX Works2 Structured Project
- Controller family: MELSEC-F
- CPU shown by GX Works2: FX3G/FX3GC; the stored profile identifies FX3G
- Program language: Ladder
- Main program: `POU > Program > MAIN`
- Program size shown by GX Works2: 614 steps
- Additional visible project areas: Parameter, Special Module (Intelligent
  Function Module), Global Device Comment, Program Setting, Local Device
  Comment, and `Device Memory > MAIN`

## Visible ladder overview

The first visible section of `MAIN` contains discrete control logic using
internal relays (`M` devices), inputs (`X`), outputs (`Y`), and timers (`T`).
Examples visible in the captured page include logic around `M200`, `M201`,
`M205`, `M10`, `X000`, `Y000`, `Y001`, `M11`, `Y012`, `Y013`, `Y014`, and timers
`T1` and `T13`. This is only the first on-screen portion of a 614-step program;
it is not a complete functional interpretation of the machine sequence.

## File integrity and test isolation

- Protected original: `D:\GX Works\schlenker.gxw`
- Normal working copy:
  `C:\www\Schlenker\fixtures\projects\schlenker-working\schlenker.gxw`
- Test proof copy:
  `C:\www\Schlenker\fixtures\projects\schlenker-hybrid-proof\20260803-145051-schlenker-hybrid-review\schlenker-proof.gxw`
- SHA-256 before and after the run for all three files:
  `B16C05F4A555E9E9628BE35AAAA889A21D047AC239DA1D5D9390FC8704A29BD1`
- Length before and after: 1,323,520 bytes
- Last-write time before and after: `2026-08-03T03:19:21.3460000Z`

The proof project, working copy, and protected original were byte-identical
after GX Works2 closed. The GX Works2 default/recent path was then restored to
the normal working copy.

## Desktop-workflow result

The Windows 10 hybrid workflow is viable for opening, observing, and recording
GX Works2 state:

1. Official Computer Use uniquely selected GX Works2 and supplied accessibility
   state plus keyboard input.
2. A read-only, exact-process, foreground-only helper saved visual evidence.
3. The helper had no input capability and wrote only below `runs/`.
4. No external screenshot was used for coordinate input.

During the live run, GX Works2 shortened the long proof path in its title. The
capture guard initially refused that title. It was updated to accept an
abbreviated `.gxw` title only when its suffix matches GX Works2's exact current
recent-file registry value, that value is an existing file below
`fixtures/projects/`, and no protected-source path is present. The helper's
unsafe-title self-test passes after this change.

## Remaining work before logic changes

- Obtain a definitive baseline compiler result with captured error and warning
  counts. The read-only view prevented the normal Build path in this run.
- Prove and document keyboard-only Guided Editing entry and cancellation on a
  disposable copy if automated ladder editing is required.
- Review all 614 steps, symbols/comments, parameters, special-module settings,
  and device memory before claiming a complete functional understanding.
- Keep every future GUI run behind a fresh operator confirmation that the PLC
  and production equipment are disconnected.

## Evidence

- Run report: `runs/20260803-145051-schlenker-hybrid-review/report.md`
- Project-open image:
  `runs/20260803-145051-schlenker-hybrid-review/screenshots/03-project-open/gxworks2-20260803-145702825-window-1-hwnd-461214.png`
- Post-Rebuild image:
  `runs/20260803-145051-schlenker-hybrid-review/screenshots/04-rebuild-all/gxworks2-20260803-145804370-window-1-hwnd-461214.png`

