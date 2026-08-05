# Task Brief: Schlenker Ladder Documentation and Human-Review Copy

## Role

Act as an industrial automation engineer documenting an existing Mitsubishi
PLC project in GX Works2. Improve maintainability by adding accurate comments
to the existing ladder program without changing its executable behaviour.

## Target

- Profile: `profiles/schlenker-gxworks2.yaml`
- Editable offline fixture:
  `fixtures/projects/schlenker-working/schlenker.gxw`
- Protected original: `D:\GX Works\schlenker.gxw`
- Software: GX Works2 1.560J
- Project type: Structured Project
- Controller: MELSEC-F, FX3G
- Program: `POU > Program > MAIN`
- Language: Ladder
- Observed program size: 614 steps

## Objective

Review the complete existing `MAIN` ladder program and add clear English
documentation for human maintenance and review. Preserve the program's logic,
instructions, devices, parameters, task configuration, execution order, and
runtime behaviour exactly.

Create comments at the appropriate GX Works2 level:

- device comments for known functions of `X`, `Y`, `M`, `T`, `C`, `D`, and
  other used devices;
- statements for the purpose of a ladder block or functional section; and
- notes where they clarify the purpose of an output instruction.

Use concise engineering language. Document observable intent, interlocks,
latched states, timer purposes, reset paths, modes, alarms, and sequence
transitions when these are supported by the program evidence.

## Presentation target

The finished ladder should be readable in the same maintenance-oriented style
as a printed annotated ladder drawing: each device address remains visible and
has a short functional description displayed next to or above the associated
contact, coil, timer, counter, or instruction.

- Enable comment display in the GX Works2 ladder view and verify the entered
  comments are actually visible in that view.
- Prefer short equipment/function labels such as `Bottle count`, `No bottles`,
  `Auto run`, `Hopper off`, or `Filler operate` only when those meanings are
  supported by Schlenker project evidence. These are style examples, not facts
  to copy into the project.
- Add a statement at the start of each logical rung group explaining its
  purpose in plain language.
- For timers and counters, describe the controlled function and preserve the
  actual preset and time base shown by the project. Do not infer engineering
  units that GX Works2 or the program does not establish.
- For output instructions, use a concise device comment and add a note when the
  energising condition or action requires clarification.
- Keep terminology and capitalisation consistent across the full program.
- Do not obscure addresses or instructions. Coloured highlighting is optional
  review markup only and is not required inside the GX Works2 project.
- Capture representative full-rung pages with comments displayed. If GX Works2
  can safely produce an offline print or export without invoking connection
  functions, also create an annotated ladder PDF for human review. Otherwise,
  provide page screenshots and record the export limitation.

## Accuracy rules

1. Inspect all of `MAIN`, existing project comments, cross-references,
   parameters, program settings, and relevant device memory before describing
   overall machine behaviour.
2. Do not invent equipment names, I/O meanings, operating sequences, safety
   functions, units, or timing intent.
3. When a function is not proven, use a neutral description based on the
   instruction's observable role, or mark it `REVIEW REQUIRED` with the reason.
4. Use one consistent description for a device everywhere it appears. Check
   every device with multiple writers or uses before assigning a comment.
5. Clearly distinguish standard control logic from any safety-related
   interface. Never claim that ordinary PLC logic is a verified safety
   function.
6. Add comments only. Do not add, delete, reorder, replace, or modify ladder
   instructions, operands, constants, devices, parameters, labels, tasks,
   connection settings, or device-memory values.

## External functional reference

The supplied Siemens TIA Portal REV10 text package is preserved at
`fixtures/references/tia-import-rev10/source/`. Read
`fixtures/references/tia-import-rev10/INTEGRATION.md` before using it.

Treat every subsystem name, signal, address, state, timing value, hardware
model, network, and behavior from that package as an unverified candidate for
the Mitsubishi project. The package targets an S7-1512C-1 PN in SCL and uses
the project spelling `Schenker`; the active project targets an FX3G in Ladder
and uses `Schlenker`. Do not directly translate or import the SCL.

A TIA-derived term may be entered as a GX Works2 comment only after correlation
with the actual ladder/cross-reference evidence and an authoritative machine
source or explicit human confirmation. Record the correlation and its status in
`comment-inventory.md`. Otherwise use a neutral ladder-derived description or
`REVIEW REQUIRED`.

## Mandatory safety gate

Before every GX Works2 UI run, obtain a fresh operator confirmation that no PLC
or production equipment is connected. An earlier confirmation is insufficient.
If confirmation is not current, stop before launching or capturing GX Works2.

- Operate only on the editable offline fixture named above.
- Never open or edit the protected original through Computer Use.
- Never use Online, Monitor, Write to PLC, Read from PLC, Transfer, Remote
  Operation, connected diagnostics, or any saved connection destination.
- Do not change PLC connection configuration.
- If an online, monitor, transfer, or connection dialog appears, stop without
  interacting and report the visible state.
- Follow all requirements in `AGENTS.md`, including the Windows 10 hybrid
  capture workflow.

In this task, "load" means open the offline fixture in GX Works2. It never means
Read from PLC. "Deliver" means create a separate review file on disk. It never
means Write to PLC or download to hardware.

## Procedure

1. Create a unique `runs/<run-id>/` evidence directory and record the protected
   original's SHA-256 hash, size, and modification time.
2. After the fresh safety confirmation, open only
   `fixtures/projects/schlenker-working/schlenker.gxw` in GX Works2.
3. Verify the active path, GX Works2 version, CPU, project type, program, and
   language. Stop if any target differs from this brief.
4. Compile/rebuild the unchanged fixture and record the complete baseline
   result, including error and warning counts. Do not edit if the baseline
   result cannot be determined; report the blocker.
5. Inspect the full project and prepare a comment inventory mapping each used
   device and ladder section to its evidence-supported description. Record
   uncertainties as `REVIEW REQUIRED` rather than guessing.
   Use the TIA REV10 material only through the correlation rules above; never
   use its Siemens addresses as Mitsubishi device mappings.
6. Add only the reviewed device comments, statements, and notes to `MAIN` and
   the appropriate comment tables.
7. Compile/rebuild again. Resolve only errors caused by comment entry or project
   state; do not alter logic to obtain a passing build.
8. Compare the documented project against the baseline and verify that only
   comment-related project data changed.
9. Save and close GX Works2. Using ordinary file tools, copy the completed
   fixture to
   `runs/<run-id>/deliverables/schlenker-commented-human-review.gxw`. Do not
   reopen or transfer this deliverable.
10. Recalculate the protected original's hash and metadata and prove they are
    unchanged.

## Deliverables

- `runs/<run-id>/deliverables/schlenker-commented-human-review.gxw`
- `runs/<run-id>/comment-inventory.md`, listing devices/sections, added
  comments, supporting evidence, and all `REVIEW REQUIRED` items
- `runs/<run-id>/deliverables/schlenker-annotated-ladder.pdf` when a safe
  offline print/export is available; otherwise, an ordered set of annotated
  ladder page images under `runs/<run-id>/screenshots/annotated-ladder/`
- before/after screenshots and hybrid capture manifests
- baseline and final compiler evidence
- completed report based on `agent/run-report-template.md`
- original pre-run and post-run hash/metadata comparison

## Acceptance criteria

- A fresh disconnection confirmation was obtained for every GX Works2 UI run.
- Only the designated offline working fixture was opened and edited.
- The complete `MAIN` program was reviewed, not only the initially visible
  ladder page.
- Comments are consistent, concise, evidence-based, and useful to a maintenance
  programmer.
- Device addresses and their functional comments are simultaneously visible in
  the captured ladder view, with rung statements making each section's purpose
  understandable at a glance.
- The review evidence covers the complete documented program through an
  annotated ladder PDF or an ordered page-image set; a few representative
  screenshots alone do not count as complete review evidence.
- Uncertain meanings are explicitly identified for human review.
- No executable logic, operand, parameter, task, device-memory, or connection
  setting changed.
- The final offline build has no new errors compared with the recorded baseline.
- The protected original's SHA-256 hash and metadata remain unchanged.
- A separately named `.gxw` file and a human-readable comment inventory are
  available for human approval.

## Stop conditions

Stop and report without improvising if the project path or CPU is unexpected,
the baseline build result cannot be established, logic changes appear necessary,
a comment's meaning cannot be supported, a graphical-only action cannot be
performed safely, or any connection-related UI appears.
