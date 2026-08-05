# TIA Import Package REV10 Integration Guide

## Provenance

- Supplied archive: `TIA_Import_Package_REV10.zip`
- Imported on: 2026-08-04 (Australia/Sydney)
- Archive SHA-256:
  `A19595057143CA2B42F78937801A7679D4C438CB4630D1F416ACEFEA00D28A5D`
- Archive project name: `Refurbished Monoblocco Schenker`
- Archive revision: `REV10`
- Archive target: Siemens S7-1512C-1 PN, SCL
- Schlenker repository target: Mitsubishi MELSEC-F FX3G, GX Works2 Ladder
- Preserved archive contents: `source/`

The supplied files are text sources and metadata, not a compiled TIA Portal
project. Their relationship to the Mitsubishi Schlenker project has not been
proven. The different spelling (`Schenker` versus `Schlenker`), controller,
language, device model, networks, and addresses must remain visible whenever
the reference is used.

## Operator-supplied SMC valve identification

On 2026-08-04, the operator supplied a photograph of SMC manifold `P1` and
explicitly identified the yellow equipment labels as the valve tags to use.
The ten visible positions are labelled, from left to right, `-125Y1` through
`-125Y10`. The evidence image SHA-256 is
`EF63E33D0242187187F8B8F24E9C0624FCF7BF71E462F85525E8035119D4032D`.

The physical labels are retained verbatim for drawings, HMI text, alarm text,
and maintenance documentation. TIA-safe symbolic aliases use the form
`SMC_125Y1` through `SMC_125Y10`; see `SMC_TAG_ALIASES.csv`.

This evidence confirms the equipment identifiers and physical order only. It
does not identify each valve's machine function, prove that physical position
1 is cyclic output bit 0, or validate the Anybus/EX260 process-image order.
Those relationships remain `REVIEW REQUIRED` until supported by the electrical
drawing, the gateway mapping, or a controlled offline/FAT verification.

### Manual HMI naming convention

The operator requested that every manual valve control show its machine
function together with the physical SMC equipment label. Use this navigation
and display pattern:

`Home > Machine > Manual > <Function> (<EquipmentLabel>)`

The first operator-confirmed assignment is:

`Home > Machine > Manual > Gate Open (-125Y1)`

Use `Gate Open (-125Y1)` on the control, alarm, diagnostic, and maintenance
views. The HMI command tag may use the TIA-safe form
`HMI_Manual_SMC_125Y1_OpenPB`. This confirmation identifies the valve function,
but it does not yet prove an Anybus/EX260 cyclic output bit.

### Mandatory return from manual control

Manual valve commands are momentary and must never latch. Manual override is
permitted only while all of these conditions are true:

- the machine is in Manual mode;
- the relevant HMI manual page is active;
- the logged-in user has manual-test permission; and
- the machine safety status permits the requested action.

If any condition becomes false, every affected valve must return to its normal
automatic command path in the same PLC scan. This includes leaving Manual mode,
navigating away from the page, logout or permission loss, and loss of the
safety permissive. The HMI must also reset all manual pushbutton command tags
to `FALSE` when the manual page closes.

The PLC enable condition for each manual override shall follow this pattern:

`TestModeActive := ManualMode AND MenuActive AND SafetyOK AND ManualTestEnable;`

When `TestModeActive` is false, the final outputs must come exclusively from
the automatic sequence (`AutoOpenCmd` and `AutoCloseCmd`). A FAT test must prove
that a valve manually energized on the HMI immediately returns to normal
automatic control when Manual mode or the manual page is exited.

## Integration classification

This package is an **external functional reference**. It is useful for forming
inspection questions and candidate maintenance terminology. It is not an
executable dependency, a conversion source, an authoritative Mitsubishi I/O
map, or proof of the existing FX3G program's intent.

| Source material | Permitted reuse | Required caution |
| --- | --- | --- |
| Machine state model | Checklist for identifying modes and transitions | Do not impose the Siemens state numbers on the FX3G program |
| Pump, accumulation, height, jog, vacuum/CIP blocks | Candidate subsystem vocabulary and review questions | Use a name in GX Works2 only after matching it to project or machine evidence |
| Manual valve override | Maintainability and reversion-to-auto design reference | It describes new behavior and is outside the comment-only task |
| Network diagnostics | Hardware inventory hypothesis | PROFINET, EtherCAT, Anybus, SMC, Pilz, IO-Link, G120C, and HMI claims are unverified for the FX3G project |
| OB1 call structure | Functional sequencing hypothesis | It is not the execution order of `MAIN` and must not be copied as such |
| Tag mapping CSV | Candidate signal descriptions | Siemens `%I`, `%IW`, TM Count, bus, and HMI addresses have no direct GX mapping |
| SCL implementation | Engineering rationale reference | Never paste, translate, import, compile, or execute it as GX Works2 logic without a separate approved conversion task |

An item marked `Confirmed` in the archive is confirmed only within that
external package. It is not confirmed for the Schlenker GX Works2 project.

## Candidate functional areas for offline correlation

The complete GX Works2 program may be inspected for evidence of these areas:

1. Power-up, homing, ready, automatic, standby, manual, CIP, emptying, and
   alarm modes.
2. Product tank level and pump control, including dry-run and drive interlocks.
3. Bottle-shortage, accumulation, and infeed-gate logic.
4. Filler/capper height adjustment and rotation blocking.
5. Forward-only maintenance jog behavior.
6. Vacuum and CIP sequencing around valves identified externally as EV210,
   EV211, and EV212.
7. Cap release identified externally as EV230.
8. Valve command mutual exclusion and return from manual test to automatic
   control.
9. Device/network diagnostics and alarm aggregation.

These are investigation prompts, not findings. Absence of a recognizable name
does not prove the function is absent, and similarity of logic does not prove
the two projects describe the same machine.

## Correlation workflow

For every candidate relationship, add a row to the run's comment inventory:

| GX device/rung | Candidate meaning | GX evidence | External reference | Human/machine-document evidence | Status |
| --- | --- | --- | --- | --- | --- |
| | | | | | `verified`, `rejected`, or `review required` |

A candidate may become a GX Works2 comment only when its meaning is supported
by the actual ladder/cross-reference evidence and an authoritative machine
source or explicit human confirmation. If that evidence is unavailable, retain
the neutral ladder-derived description or mark the item `REVIEW REQUIRED`.

Never infer Mitsubishi device addresses from the Siemens tag map. Never change
logic, parameters, communications, or hardware configuration while performing
reference correlation or comment entry.

## Explicitly excluded from this integration

- TIA Portal import or compilation
- Automated Siemens-to-Mitsubishi code translation
- Creation or modification of GX Works2 executable logic
- Adoption of Siemens addresses, hardware, networks, timing, state numbers, or
  call order as Schlenker facts
- PLC connection, transfer, write, remote operation, or commissioning
- Any claim that the package implements or validates machine safety

The package itself states that the Pilz system remains the authoritative safety
layer and that its process interlocks do not replace safety validation. That
statement is retained as source context, not as a verified fact about the
current machine.

## Guarded-door access request

The operator-confirmed guarded-access sequence is documented in
`DOOR_ACCESS_SEQUENCE.md`. `Request Open Door` is a safety-access request and
must not be confused with the pneumatic manual command
`Gate Open (-125Y1)`. The HMI/standard PLC may coordinate status and requests,
but the validated safety system remains authoritative for hazardous-energy
removal, guard unlocking, safety-circuit reset, and restart permission.
