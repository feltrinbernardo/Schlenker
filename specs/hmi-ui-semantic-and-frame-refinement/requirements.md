# Requirements — HMI Semantic and Global-Frame Refinement

Date: 2026-08-13  
Target: TIA Portal V19 / WinCC Unified / MTP1500 Unified Comfort  
Resolution: 1366 x 768 at scale factor 1.0

## 1. Purpose

This specification defines a controlled refinement of the Schlenker WinCC
Unified HMI. It addresses unsafe or ambiguous visual semantics, the common
header/navigation/footer frame, Production KPIs, missing-data presentation,
and cross-screen consistency while preserving the existing PLC and HMI
behavior.

The governing assessment is
`docs/HMI_UI_REFINEMENT_FINDINGS.md`. The existing
`specs/production/` specification remains authoritative for Production process
content, bindings, interactions, access control, and acceptance requirements
unless this specification explicitly introduces a more restrictive visual or
transaction rule.

## 2. Scope

### R-001 — Pilot scope

The first implementation pilot shall cover exactly:

1. `production`
2. `home`

The initial TIA transaction authorized by this specification shall target
exactly the named `production` screen. Home and all other screens require
separate screen-scoped transactions and approvals.

### R-002 — Subsequent scope

After the Home and Production pilot receives visual approval, the approved
global frame may be proposed for the remaining HMI screens one screen at a
time. Multi-screen mutation and wildcard targeting are prohibited.

### R-003 — Out of scope

This specification does not authorize changes to:

- PLC logic or PLC data structures.
- Safety logic, devices, networks, permissives, or interlocks.
- Alarm definitions, classes, philosophy, or acknowledgement logic.
- HMI or PLC tags and tag addresses.
- Recipes, scripts, security, users, roles, or authorizations.
- Command events, command direction, or operating sequences.
- Hardware configuration or communication settings.
- Online access, monitoring, transfer, download, run, stop, or reset.

Any required change in these areas shall stop the UI transaction and require a
separate specification and explicit authority.

## 3. Authorization and transaction gates

### R-010 — Recorded Production approval

The operator explicitly approved writing the named WinCC Unified screen
`production` on 2026-08-13 for the refinements governed by this specification.

This is an approval of intent and target. It is not by itself an executable
write envelope. Before any Production mutation, the transaction shall also
contain:

- The exact project and HMI identity.
- A fresh read-only Production inventory and source hash.
- A fresh `.zap19` checkpoint created through supported TIA Portal V19
  Openness APIs.
- The checkpoint hash and verified archive location.
- The exact approved screen-source hash.
- The exact diff-plan hash.
- The exact capability-registry or executor hash.
- A human approval record matching those hashes.
- An explicit Production acknowledgement in the transaction envelope.

If any bound hash changes, the write approval is stale and shall be renewed.

### R-011 — One-screen isolation

Every mutation transaction shall name exactly one HMI screen. Before save, the
transaction shall prove that all non-target screen signatures are unchanged.

### R-012 — Supported API boundary

TIA access shall occur only through supported TIA Portal V19 Openness APIs.
No `.ap19`, `.zap19`, project database, archive, or binary internal content may
be parsed, patched, unzipped, or rewritten directly.

### R-013 — Default read-only

All discovery, inventory, diff, and evidence collection shall be read-only.
Mutation shall remain disabled until the exact approved transaction passes all
preconditions.

### R-014 — No hardware communication

No part of this specification requires or authorizes PLC/HMI online access,
monitoring, diagnostics, transfer, download, device-memory writes, or remote
operation.

### R-015 — Compile, reconcile, and save gate

After applying the approved Production changes offline:

- HMI compile shall complete with zero errors and zero warnings.
- Managed properties shall match the approved source.
- Bindings, events, scripts, authorizations, and unmanaged properties shall
  match the pre-change evidence unless explicitly included in a separately
  authorized scope.
- All non-target screen signatures shall remain unchanged.
- Save shall occur only after compile and reconciliation pass.

Any failure shall abort before save and preserve the checkpoint and transaction
evidence.

## 4. Global-frame requirements

### R-020 — Canonical frame decision

The established frame already validated on the non-Production screens shall be
the proposed global-frame baseline. Production central content and behavior
shall be preserved while its common frame is reconciled to that baseline.

The frame selection shall be reviewed and hash-approved before mutation.

### R-021 — Header and status row

Every main screen shall use the same approved geometry for:

- Machine/project title.
- Current page title.
- Machine status label and value.
- System-ready indication.
- Alarm indication.
- Date/time display.
- User/profile display.

Foreground objects shall not overlap, clip, or extend outside the screen.

### R-022 — Right-side navigation

All approved primary navigation destinations shall appear in the fixed
right-side rail in the same order on every main screen.

- Button size: 133 x 43 pixels unless a reviewed V19 rendering test requires a
  documented adjustment.
- Button and icon coordinates shall be integer values.
- Icon overlays shall not intercept touch events.
- The current page shall have a distinct selected state.
- Existing event behavior and target screen names shall be preserved.
- Navigation shall not overflow into the footer.

### R-023 — Footer

The footer shall be reserved for approved global status and approved shortcuts.
It shall not serve as overflow storage for primary navigation objects.

### R-024 — Touch targets

Interactive hit areas shall not overlap. Text and icons shall remain readable
without reducing a primary touch target below its approved dimensions.

## 5. Semantic state requirements

### R-030 — State palette

The following semantic mapping is mandatory:

- Green: confirmed healthy or active feedback.
- Grey: inactive, disabled, or unavailable.
- Amber: warning or attention required.
- Red: active alarm, trip, fault, or stop condition.
- Bad quality/disconnected: distinct diagnostic treatment that is not confused
  with valid OFF, zero, or healthy state.

Colour shall be reinforced by text, symbol, or state label wherever the object
communicates an operational condition.

### R-031 — Alarm truth semantics

When `Alarm_Low_Vacuum` or `Alarm_Critical` is true, its visual state shall not
be green. It shall follow the red alarm mapping in R-030. False shall use the
approved inactive/normal representation.

This requirement changes visual semantics only. It shall not alter the alarm
tags, alarm logic, acknowledgement, reset behavior, or PLC code.

### R-032 — Command versus feedback

A command or output tag shall not be presented as physical feedback. Labels
shall identify command state when only a command is known. `Running`, `Open`,
`Closed`, `Ready`, or similar feedback wording requires a proven feedback
source.

### R-033 — Data quality

Data quality shall be treated independently from the numeric or Boolean value.
A bad-quality zero or false value shall not appear as a valid zero or valid OFF.

No new quality source or quality semantics may be invented. If quality cannot
be read through a proven supported V19 mechanism, the limitation shall remain
documented and shall not be visually fabricated.

### R-034 — No normal-state blinking

Normal active, healthy, and inactive states shall not blink. Animation is
permitted only when it communicates a proven process condition and passes the
runtime-performance acceptance gate.

## 6. Production content requirements

### R-040 — Preserve confirmed bindings and events

The 33 currently resolved Production HMI tag references, existing command
events, navigation targets, and read/write directions shall be preserved unless
a separately approved diff explicitly changes one of them.

### R-041 — Missing and ambiguous data

Unavailable information shall use `N/A`, `MISSING`, `AMBIGUOUS`, or
`NOT CONFIGURED`, with evidence. No tag, value, scaling, device identity,
conversion, feedback, or quality state may be invented.

### R-042 — Produced Today

Produced Today shall not display a live-looking count until a genuine daily or
resettable counter and its reset basis are confirmed. `Bottle_Count_In_Machine`
shall not be used as a substitute.

Until confirmed, the card shall display `N/A` or `NOT CONFIGURED` with neutral
styling.

### R-043 — Product pressure

Product pressure shall remain `NOT CONFIGURED` until its source, physical
meaning, engineering unit, scaling, and quality treatment are confirmed.

### R-044 — M103

M103 running feedback, ready/permissive, fault/trip, actual speed, mode, and
communication state shall be handled independently. Each unproven state shall
remain `MISSING` or `NOT CONFIGURED`. No M102 signal or unrelated command shall
be substituted.

### R-045 — Speed KPI

The Production speed KPI shall use a circular or radial visual compatible with
WinCC Unified V19.

- `Speed_Actual_Pct` may be displayed as percent.
- `Speed_Setpoint_BPH` shall remain a separate BPH setpoint.
- A BPH actual value shall not be derived from percent without an approved and
  evidenced conversion.
- Invalid/unavailable input shall display `N/A`.

### R-046 — Efficiency KPI

Efficiency shall use the confirmed run-time and total-time sources with the
documented formula. It shall display `N/A` when total time is zero or input is
invalid. The ring or arc shall follow the same calculated value as the text.

### R-047 — Process mimic

The Production mimic shall retain its approved equipment identity and process
topology while improving alignment, legibility, line consistency, and state
clarity. Touching mimic equipment shall not directly command hardware.

## 7. Cross-screen requirements

### R-050 — Home pilot

Home shall be assessed after the Production pilot against the exact approved
global frame. A Home write requires its own screen-scoped approval and
transaction evidence.

### R-051 — Alarms

The native alarm control shall be preserved. Acknowledgement/reset controls
shall not overlap it. Warning and stop/fault states shall be visually distinct.

### R-052 — Settings and diagnostics

Read-only fields shall be clearly differentiated from editable parameters.
Diagnostic screens shall not introduce output forcing. Bare numeric or Boolean
values shall be replaced by semantic text only where the meaning is proven.

### R-053 — Manual, CIP, and Recipe

UI refinements shall preserve all access control, mode gates, PLC interlocks,
Safety authority, command behavior, and recipe lifecycle rules.

## 8. TIA Portal V19 compatibility

### R-060 — Supported object model

The implementation shall prefer proven native V19 objects:

- `HmiText`
- `HmiButton`
- `HmiIOField`
- `HmiSymbolicIOField`
- `HmiRectangle`
- `HmiEllipse`
- `HmiLine`
- `HmiGraphicView`
- Existing native `HmiAlarmControl`, preserved according to its supported API
  surface

Unknown or unproven object/property mutations shall fail closed.

### R-061 — SVG boundary

SVG assets shall be controlled, well-formed, script-free, self-contained,
hash-recorded, and rendered at their intended size without clipping. Dynamic
SVG recolouring shall not be assumed unless proven in the target V19 runtime.

### R-062 — Unsupported presentation features

The implementation shall not depend on responsive layout, browser CSS effects,
external web resources, unsupported fonts, complex filters, or browser-only
behavior.

### R-063 — Deterministic geometry

All authored geometry shall be deterministic, integer-based, and validated at
1366 x 768 and 100% zoom. The browser/reference preview is not a substitute for
TIA compile and runtime rendering evidence.

## 9. Acceptance requirements

### R-070 — Static validation

The approved source shall pass schema, bounds, overlap, clipping, identity,
asset, binding, policy, and semantic-state validation with zero errors.

### R-071 — Production diff

The Production diff shall contain only approved frame and visual-semantic
changes. Behavioral differences and unexpected object deletion shall be
rejected.

### R-072 — Compile

The HMI shall compile in TIA Portal V19 with zero errors and zero warnings.

### R-073 — Reconciliation

Post-apply reconciliation shall report:

- All approved managed values match.
- All preserved bindings, events, scripts, and unmanaged properties match.
- No unexpected object creation or deletion.
- No non-target screen drift.
- No unresolved symbolic references.

### R-074 — Visual acceptance

Production shall be captured at 1366 x 768 and reviewed at 100% zoom. It shall
have no clipping, unintended overlap, off-screen controls, misleading alarm
colour, navigation overflow, or inconsistent common-frame geometry.

### R-075 — Pilot approval

Production and Home shall each receive explicit visual approval before the
global frame is proposed for another screen.

### R-076 — Audit evidence

The immutable transaction evidence shall include the checkpoint, approval,
hashes, pre/post inventories, diff plan, apply journal, compile result,
reconciliation, non-target screen signatures, captures, and final outcome.

## 10. Stop conditions

The transaction shall stop without save when:

- The exact target project, HMI, or screen is ambiguous.
- A required tag, meaning, unit, scaling, or quality rule is missing or
  ambiguous.
- The fresh baseline differs from the approved baseline.
- The checkpoint or approval hashes do not match.
- A requested property is unsupported or not runtime-proven in V19.
- A behavior, security, alarm, PLC, Safety, tag, or interlock change appears in
  the diff.
- Compile produces any error or warning.
- Reconciliation detects any unexpected difference.
- A non-target screen changes.

