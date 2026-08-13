# HMI UI Refinement Findings

Date: 2026-08-13  
Repository: `C:\www\Schlenker`  
Target: TIA Portal V19 / WinCC Unified / MTP1500 Unified Comfort  
Assessment mode: offline and read-only

## Purpose

This document records the UI refinement findings obtained from the existing
Schlenker specifications, inventories, audit reports, and captured images. It
is an assessment and planning input only. It does not authorize a Production
write, a TIA project modification, a PLC change, or communication with
hardware.

The current HMI already has a strong visual foundation. The next refinement
should prioritize correct operational meaning and a consistent global frame
before further cosmetic work.

## Evidence reviewed

- `specs/production/requirements.md`
- `specs/production/design.md`
- `specs/production/tasks.md`
- `REV12/Documentation/REV12_3_HMI_UI_Layout_Refinement.md`
- `REV12/HMI/REV12_HMI_Pages.md`
- `outputs/REV25_HMI_Global_Production_Master_Template_and_PLC_Audit/REV25_EXECUTION_REPORT.md`
- `outputs/REV25_HMI_Global_Production_Master_Template_and_PLC_Audit/REV25_PRODUCTION_BINDING_AUDIT.md`
- `outputs/REV25-current/completion-ledger-final.tsv`
- `outputs/REV25-existing-open-2026-08-13/production-audit/production-object-binding-audit.tsv`
- Existing screenshots and reference images under `img-log/`

No `.ap19`, `.zap19`, `.gxw`, or other protected project internals were
parsed, patched, or modified during this assessment.

## Current state

- The REV25 ledger covers 25 HMI screens.
- Twenty-four screens follow the validated global frame or have no outstanding
  frame issue in the final ledger.
- `production` remains a frozen master with 24 recorded frame issues.
- The Production binding audit found 33 referenced HMI tags; all 33 exist and
  resolve to PLC tag and connection mappings.
- M103 feedback/speed remains explicitly `MISSING`.
- The global profile control has no verified login/session navigation action.
- Produced Today and product pressure still lack confirmed sources suitable
  for operational display.
- Previous REV12 evidence records an HMI compile with zero errors and zero
  warnings, but compile success does not prove that every visual state has the
  correct operational meaning.

## Priority 0: correct unsafe or misleading visual semantics

### Alarm indicators

The current Production object audit shows the `Vacuum alarm` and `Critical
alarm` lamps returning green when their respective alarm tags are true. This
conflicts with the documented visual convention and can make an active alarm
look healthy.

Required refinement:

- Active alarm or trip: red.
- Warning or attention: amber.
- Confirmed healthy or active process feedback: green.
- Inactive or unavailable: grey.
- Bad quality or disconnected: a distinct state that cannot be mistaken for a
  valid OFF or numeric zero.
- Status meaning must be reinforced by text or symbol; colour alone is
  insufficient.

This should be treated as the highest-priority Production correction. The
change must be limited to proven visual semantics and must not alter alarm or
PLC logic.

## Priority 1: approve and apply one canonical global frame

The current Production screen conflicts with the frame already validated on
the other screens. The main deviations include navigation items in the bottom
bar, inconsistent vertical positions, and a displaced user/profile icon.

Recommended master decision:

- Adopt the established canonical right-side rail already validated on the
  other screens.
- Preserve Production central content, bindings, events, command behavior, and
  object identities.
- Keep all main destinations in the right-side rail.
- Use the established 133 x 43 navigation button size and uniform spacing.
- Standardize the header, status row, user, alarm, date/time, and footer
  geometry.
- Keep the footer for global status or deliberately approved shortcuts; do not
  use it as overflow navigation.

No global propagation should occur until the corrected Home and Production
pilot screens have been reviewed at 1366 x 768 and 100% zoom.

## Production refinements

### Speed KPI

The approved requirement calls for a circular or radial speedometer. The
current screen uses a linear track and calculates a BPH-looking value from
`Speed_Actual_Pct` using a fixed multiplier. That conversion is not presently
configured as an approved engineering relationship.

Recommended approach:

- Use a radial gauge composed of supported native Unified objects.
- Display percentage when the confirmed source is `Speed_Actual_Pct`.
- Display BPH only when the conversion or direct BPH source is proven.
- Keep `Speed_Setpoint_BPH` visually separate from actual percentage feedback.
- Show `N/A` for invalid or bad-quality data.

### Efficiency KPI

The efficiency value already uses confirmed run and total time sources with an
`N/A` fallback when total time is zero. The visual ring should follow the
calculated value and should not show a static demonstration percentage.

Recommended approach:

- Dynamic 0-100% ring or arc.
- `N/A` when the denominator is zero or the source quality is invalid.
- No continuous decorative animation or normal-state blinking.

### Unavailable values

The following must remain explicit rather than being filled with plausible
values:

- Produced Today: `MISSING` until a genuine daily/resettable counter and reset
  basis are confirmed.
- Product pressure: `MISSING` until source, physical meaning, unit, and scaling
  are confirmed.
- M103 feedback, speed, ready, fault, mode, and communication states: show only
  individually proven signals; otherwise use `N/A` or `NOT CONFIGURED`.

No M102 signal, command state, static sample, or unrelated counter may be used
as a substitute.

### Command versus feedback

Several valve indicators use command tags. A command is not proof of physical
actuation.

Recommended presentation:

- Label a command explicitly as `command` when no physical feedback exists.
- Use `open feedback`, `closed feedback`, or `running feedback` only for proven
  feedback sources.
- Show conflicting feedback only when the required signals are available.
- Keep direct commands out of the Production mimic; equipment touch may open a
  read-only diagnostic context but must not energize hardware.

### Process mimic

- Standardize pipeline stroke width and arrow style.
- Align pumps, tank, sensors, and valves to a consistent grid.
- Increase the legibility of equipment codes and engineering units.
- Use state text or symbols in addition to colour.
- Avoid fragile layering and unnecessary object overlap.
- Use subtle animation only when it is driven by a valid process condition and
  proven acceptable for MTP1500 Unified runtime performance.

## Cross-screen refinements

### Home

- Preserve a simple machine overview and concise process commands.
- Reduce or remove large reserved-actuator placeholders unless their future use
  is an approved operator requirement.
- Keep command and feedback visually distinct.
- Display Produced Today only after its source is confirmed; otherwise use
  `N/A` or `NOT CONFIGURED`.

### Operate

- Apply the canonical user/header geometry.
- Separate Production commands from Safety and access status.
- Make requested command, PLC permissive, and confirmed state distinct.
- Do not imply that an HMI indication replaces PLC or Safety authority.

### Alarms

- Preserve the native `HmiAlarmControl`.
- Reserve a non-overlapping area for acknowledgement and reset controls.
- Present cause, state, timestamp, and operator action where supported by the
  existing alarm configuration.
- Keep warnings visually distinct from machine-stopping faults.
- Preserve the approved English and Italian alarm content.

### Settings

- Use consistent columns for engineering code, function, location, and value.
- Clearly differentiate read-only diagnostics from editable parameters.
- Display units, permitted ranges, and actual values where confirmed.
- Reserve a dedicated footer band for Settings Home, Previous, and Next.
- Do not introduce output forcing through diagnostic screens.

### Diagnostics

- Replace ambiguous bare `0` values with meaningful states such as `OK`,
  `FAULT`, `ON`, `OFF`, or `NO DATA`, according to confirmed semantics.
- Present quality independently from the process value.
- Group Network, Safety, and Process Inputs consistently.
- Keep drill-down views read-only unless separate authorization exists.

### Manual, CIP, and Recipe

- Improve grouping, spacing, sequence progress, and feedback visibility.
- Preserve all existing modes, permissions, interlocks, and command behavior.
- Manual control must remain dependent on the approved mode, active page,
  access level, PLC interlocks, and Safety conditions.
- CIP should distinguish current step, requested action, awaited condition, and
  fault.
- Recipe should clearly separate selection, comparison, load, save, and
  approval-dependent actions.

## TIA Portal V19 implementation boundary

Suitable implementation mechanisms include:

- `HmiText`, `HmiButton`, `HmiIOField`, and `HmiSymbolicIOField`.
- Native rectangles, ellipses, lines, and simple arcs.
- `HmiGraphicView` with validated, controlled SVG assets.
- Native `HmiAlarmControl`.
- Supported native dynamizations and small, reviewed Unified scripts.
- Fixed 1366 x 768 geometry with integer coordinates and 100% zoom review.

Avoid or prohibit:

- Responsive/web-only layout behavior.
- CSS effects, blur, complex shadows, or browser-specific filters.
- SVG scripts, external resources, embedded web dependencies, or unsupported
  fonts.
- Heavy animations or excessive polling.
- Fragile reliance on complex z-order composition.
- Invented tags, states, conversions, units, or quality semantics.
- Bulk page updates before pilot approval.
- Any PLC, Safety, security, alarm, interlock, or command-behavior change as an
  incidental UI refinement.

## Recommended implementation sequence

1. Confirm and document the alarm-state colour correction.
2. Approve the established canonical frame as the global frame.
3. Create an exact read-only baseline for Home and Production.
4. Resolve the Production frame without changing central bindings or behavior.
5. Refine speed, efficiency, missing-data fallbacks, and command/feedback
   wording.
6. Validate geometry, overlap, clipping, text, navigation, SVGs, bindings, and
   quality states offline.
7. Compile HMI and reconcile every managed object with zero errors and zero
   warnings.
8. Capture Home and Production at 1366 x 768 and 100% zoom.
9. Obtain explicit visual approval for both pilots.
10. Propagate the approved frame one screen at a time, with a new diff and
    validation package for each screen.

## Recommendation: create a dedicated specification

Creating a new requirements/design/tasks specification is valid and strongly
recommended. The existing Production specification mixes original build goals,
historical implementation, unresolved preparation tasks, and current REV25
frame decisions. A dedicated refinement spec would establish a clean baseline
and prevent already-completed work from being confused with the new scope.

Suggested spec directory:

`specs/hmi-ui-semantic-and-frame-refinement/`

Suggested files:

- `requirements.md`: normative outcomes, safety boundaries, supported screen
  scope, semantic state rules, global frame acceptance, binding rules, and
  validation criteria.
- `design.md`: canonical frame geometry, component patterns, alarm/status state
  model, gauge construction, quality-state treatment, per-screen migration
  approach, and V19 implementation constraints.
- `tasks.md`: phased inventory, approval, Home/Production pilot, compile,
  reconciliation, visual acceptance, and one-screen-at-a-time rollout.

The spec should reference the existing Production specification rather than
replace it. It should explicitly state precedence where a new refinement rule
supersedes a historical visual decision, while preserving all PLC, Safety,
alarm, security, interlock, tag, and command semantics unless separately
authorized.

## Authorization status

This document authorizes no implementation. In particular, it does not
authorize:

- A write to the Production screen.
- A TIA project save or compile operation.
- A PLC or HMI hardware connection, monitor, transfer, or download.
- A binding, script, event, security, alarm, tag, PLC, Safety, interlock, or
  command change.
- Global propagation of the frame.

Every future Production write requires its own explicit Production approval
and the repository's checkpoint, evidence, compile, reconciliation, and
human-approval gates.
