# Tasks — Native HMI Production Page Implementation

## Phase 0 — Protect Current Project

- [ ] T0.1 Create a backup/checkpoint of the current REV13 TIA project.
- [ ] T0.2 Record current PLC compile status.
- [ ] T0.3 Record current HMI compile status.
- [ ] T0.4 Export/inventory current HMI screens, tags, scripts, faceplates and alarms.
- [ ] T0.5 Capture screenshots of Home, Production, Safety and existing global navigation.
- [ ] T0.6 Record the active project path, project revision and target MTP1500 resolution.
- [ ] T0.7 Create an open-point register before implementation.
- [ ] T0.8 Confirm that the implementation scope is Home and Production only until visual approval.
- [ ] T0.9 Record REV14.1 as the controlling preparation/implementation gate.

---

## Phase 1 — Audit Existing Objects

- [ ] T1.1 Identify current global top-header objects.
- [ ] T1.2 Identify all navigation buttons and target screens.
- [ ] T1.3 Identify existing M102 Product Pump tags.
- [ ] T1.4 Identify existing M103 Vacuum Pump tags.
- [ ] T1.5 Identify TLS100 tags.
- [ ] T1.6 Identify vacuum source tag and current engineering unit.
- [ ] T1.7 Identify actual speed and speed-setpoint tags.
- [ ] T1.8 Identify efficiency calculation/tag.
- [ ] T1.9 Identify produced-today counter tag.
- [ ] T1.10 Identify valve and process-sensor tags used by Production.
- [ ] T1.11 Assign every binding exactly one REV14.1 readiness status: `CONFIRMED`, `MISSING`, `AMBIGUOUS` or `NOT REQUIRED`.
- [ ] T1.12 Confirm `Speed_Feedback_Pct` and `Speed_Feedback_Valid` as the actual-speed display interface.
- [ ] T1.13 Confirm that `Speed_Setpoint_BPH` is not compared directly with percent feedback.
- [ ] T1.14 Confirm `Vacuum_Actual_mbar` scaling and sign convention.
- [ ] T1.15 Reconcile `SPC100` versus `SPL100` with the electrical drawing and I/O list.
- [ ] T1.16 Confirm an authoritative Produced Today counter and daily reset basis, or classify the KPI `MISSING`/`AMBIGUOUS` with evidence.
- [ ] T1.17 Confirm a product-pressure signal, physical meaning, engineering unit and scaling, or classify the value `MISSING`/`AMBIGUOUS` with evidence.
- [ ] T1.18 Identify M103 running feedback, fault/trip, ready/permissive, speed, mode and communication signals individually; classify every unavailable state `MISSING` and show it as `N/A`/`Not Configured` in the pilot.
- [ ] T1.19 Produce the complete tag-binding matrix with type, access, unit, scaling, quality and fallback state.
- [ ] T1.20 Search tags, cross-references, DBs, HMI tag tables, scripts, alarms, faceplates, screen events and existing graphics before classifying an item unresolved.
- [ ] T1.21 Record normal-state semantics, warning/fault semantics, quality/communication source, read/write direction and evidence for every Production visual object.
- [ ] T1.22 Record `Bound`, `Derived` or `Not Configured` separately from readiness; allow `Derived` only from `CONFIRMED` sources and a documented formula/reset basis.

**Exit criterion:** no new Production object is created until the binding inventory is complete.

`MISSING`/`AMBIGUOUS` acceptance items may remain open only if the HMI clearly displays `N/A`/`Not Configured` and the release is explicitly classified as partial. Produced Today cannot pass final acceptance without a real daily/resettable counter.

---

## Phase 1A — Validate Controlled REV14 Assets

- [ ] T1A.1 Inventory every SVG in `C:\www\Schlenker\.modification_logic\svg`.
- [ ] T1A.2 Parse every SVG as XML.
- [ ] T1A.3 Correct the duplicate `fill` attribute in `nav_cip.svg`.
- [ ] T1A.4 Verify viewBox, transparent background and intended aspect ratio.
- [ ] T1A.5 Verify no scripts, external links, web dependencies, event handlers or unsupported fonts.
- [ ] T1A.6 Verify no duplicate IDs.
- [ ] T1A.7 Render every SVG at the intended HMI size and inspect clipping/stroke consistency.
- [ ] T1A.8 Record filename, purpose, viewBox, dimensions and SHA-256 in the asset manifest.
- [ ] T1A.9 Approve a WinCC Unified dynamic-state strategy for SVG graphics.
- [ ] T1A.10 Record semantic function, asset version, validation result and intended HMI use for every final SVG.
- [ ] T1A.11 Reject every asset that fails XML, security, dependency, viewBox, duplicate-ID or render validation.

**Exit criterion:** all controlled assets pass validation; no malformed or unapproved asset is imported.

---

## Phase 1B — Resolve Access Matrix and Session Reversion

- [ ] T1B.1 Map the existing WinCC Unified user groups and authorizations to Operator, Maintenance and Engineer.
- [ ] T1B.2 Preserve password-free routine Operator access for Production, CIP and normal reset/start/stop already permitted by the project.
- [ ] T1B.3 Protect Maintenance diagnostics, setup, bypasses, manual actuators and I/O/service actions behind Maintenance login.
- [ ] T1B.4 Protect advanced configuration and engineering/service parameters behind Engineer login.
- [ ] T1B.5 Confirm HMI privilege cannot bypass PLC or Safety interlocks.
- [ ] T1B.6 Implement the supported inactivity timeout that returns the active session to Operator/default access.
- [ ] T1B.7 Record the final access matrix and timeout behavior as preparation evidence.

**Exit criterion:** the access matrix and session-reversion behavior are implemented and documented before pilot-page construction.

---

## Phase 2 — Build Global UI Components

- [ ] T2.1 Create/reuse global header template.
- [ ] T2.2 Create right-side navigation button component.
- [ ] T2.3 Add approved icons: Home, Safety, Production, Manual, CIP, Run Out, Settings, Diagnostics, Alarms.
- [ ] T2.4 Add selected-page visual state.
- [ ] T2.5 Validate every navigation destination.
- [ ] T2.6 Create/reuse bottom status bar.
- [ ] T2.7 Inventory existing Operate, Function, Recipe and Efficiency destinations and preserve them unless removal is approved.
- [ ] T2.8 Resolve controlled icons for every retained destination.
- [ ] T2.9 Verify the identical navigation order on Home and Production before propagation.
- [ ] T2.10 Create and validate matching SVG icons for Operate, Function, Recipe and Efficiency when those pages exist.

**Exit criterion:** global frame is visually consistent and all navigation links work.

---

## Phase 3 — Create Reusable Native HMI Faceplates

- [ ] T3.1 Pump faceplate.
- [ ] T3.2 Valve faceplate.
- [ ] T3.3 Digital sensor faceplate.
- [ ] T3.4 Analog sensor faceplate.
- [ ] T3.5 Device-detail popup.
- [ ] T3.6 KPI speedometer component.
- [ ] T3.7 KPI efficiency ring component.
- [ ] T3.8 Produced-today KPI with bottle SVG.
- [ ] T3.9 Demonstrate OFF/ON/WARNING/FAULT/BAD-QUALITY states on one pump.
- [ ] T3.10 Demonstrate OPEN/CLOSED/TRANSITION/FAULT/BAD-QUALITY states on one valve.
- [ ] T3.11 Demonstrate FALSE/TRUE/BAD-QUALITY states on one digital sensor.
- [ ] T3.12 Verify that read-only mimic faceplates expose no actuator command action.

**Exit criterion:** each component has documented parameters and test bindings.

---

## Phase 4 — SVG / Graphics Library

- [ ] T4.1 Create/import Home house SVG.
- [ ] T4.2 Create/import Safety icon SVG.
- [ ] T4.3 Create/import Production icon SVG.
- [ ] T4.4 Create/import Manual icon SVG.
- [ ] T4.5 Create/import CIP icon SVG.
- [ ] T4.6 Create/import Run Out icon SVG.
- [ ] T4.7 Create/import Settings icon SVG.
- [ ] T4.8 Create/import Diagnostics icon SVG.
- [ ] T4.9 Create/import Alarm icon SVG.
- [ ] T4.10 Create/import Product Pump SVG.
- [ ] T4.11 Create/import Vacuum Pump SVG.
- [ ] T4.12 Create/import Valve SVG set.
- [ ] T4.13 Create/import Sensor SVG set.
- [ ] T4.14 Create/import Bottle icon SVG.
- [ ] T4.15 Optimize all SVGs for WinCC Unified.
- [ ] T4.16 Import validated assets into a dedicated REV14 project-graphics folder.
- [ ] T4.17 Record imported project-graphic names in the asset manifest.
- [ ] T4.18 Confirm no HMI object depends on the external filesystem path at runtime.

---

## Phase 5 — Rebuild Production Page Front-End

- [ ] T5.1 Apply approved top header.
- [ ] T5.2 Apply right-side navigation.
- [ ] T5.3 Build left command/status card area.
- [ ] T5.4 Build central Product/Filler/Vacuum mimic.
- [ ] T5.5 Place M102 Product Pump.
- [ ] T5.6 Place product pressure readout.
- [ ] T5.7 Place product valves.
- [ ] T5.8 Place filler/process graphic.
- [ ] T5.9 Place TLS100.
- [ ] T5.10 Place vacuum path.
- [ ] T5.11 Place M103 Vacuum Pump.
- [ ] T5.12 Place vacuum readout in mbar.
- [ ] T5.13 Add lower process-status cards.
- [ ] T5.14 Add mode/sequence card.
- [ ] T5.15 Add alarm summary.
- [ ] T5.16 Add bottom status bar.
- [ ] T5.17 Show `Not Configured` for missing product pressure rather than another process value.
- [ ] T5.18 Label command-only valve states as command, not feedback.
- [ ] T5.19 Apply minimum 8 px internal and 10 px inter-card gutters.
- [ ] T5.20 Run object-boundary overlap and clipping audit at 100% zoom.
- [ ] T5.21 Ensure each equipment touch opens only detail/diagnostic context and never directly energizes hardware.

**Exit criterion:** layout visually matches approved master reference before advanced animation is added.

---

## Phase 6 — Add KPI Graphics

- [ ] T6.1 Bind circular speedometer to actual speed.
- [ ] T6.2 Bind speed setpoint/target.
- [ ] T6.3 Bind circular efficiency gauge.
- [ ] T6.4 Bind produced-today counter.
- [ ] T6.5 Add bottle SVG next to produced-today KPI.
- [ ] T6.6 Verify units and ranges.
- [ ] T6.7 Bind actual speed to `Speed_Feedback_Pct` and gate validity with `Speed_Feedback_Valid`.
- [ ] T6.8 Display BPH setpoint separately until an approved percent/BPH conversion exists.
- [ ] T6.9 Display efficiency as `N/A` when total classified time is zero.
- [ ] T6.10 Prohibit `Bottle_Count_In_Machine` as the Produced Today binding.
- [ ] T6.11 If no true daily counter is `CONFIRMED`, show `N/A`/`Not Configured` and do not add PLC counter/reset logic.

---

## Phase 7 — Dynamic Properties and Animations

- [ ] T7.1 Pump running/stopped/fault visual states.
- [ ] T7.2 Valve open/closed/fault visual states.
- [ ] T7.3 Digital sensor grey/green states.
- [ ] T7.4 Analog warning/fault states.
- [ ] T7.5 Product-flow active animation.
- [ ] T7.6 Vacuum-flow active animation.
- [ ] T7.7 Speed gauge animation.
- [ ] T7.8 Efficiency ring animation.
- [ ] T7.9 Navigation selected-state animation.
- [ ] T7.10 Bad-quality/disconnected visual states.
- [ ] T7.11 Confirm the SVG/state technique works in MTP1500 Unified Runtime, not only in the editor.
- [ ] T7.12 Verify animation update rates do not create excessive polling or scripts.

**Rule:** no normal-status blinking.

---

## Phase 8 — Vacuum Unit Conversion

- [ ] T8.1 Confirm PLC vacuum engineering unit.
- [ ] T8.2 If source is bar, implement documented ×1000 conversion for HMI display.
- [ ] T8.3 Display unit as `mbar`.
- [ ] T8.4 Validate sign convention.
- [ ] T8.5 Validate alarm threshold display against PLC logic.

Current baseline expectation: `Vacuum_Actual_mbar` is already mbar; therefore T8.2 should normally record "conversion not required" rather than apply ×1000.

---

## Phase 9 — Interactive Device Navigation

- [ ] T9.1 Touch M102 → open M102 detail.
- [ ] T9.2 Touch M103 → open M103 detail.
- [ ] T9.3 Touch valve → open valve detail.
- [ ] T9.4 Touch TLS100 → open sensor detail.
- [ ] T9.5 Detail popup shows code, description, state, location and bindings.
- [ ] T9.6 Protected manual actions require Maintenance/Engineer access.
- [ ] T9.7 Confirm mimic touch never directly energizes outputs.
- [ ] T9.8 Confirm each popup reports command versus feedback accurately.
- [ ] T9.9 Confirm unavailable bindings are shown as Not Configured.

---

## Phase 10 — Home and Global Page Alignment

- [ ] T10.1 Restore machine graphic on Home.
- [ ] T10.2 Restore produced-today KPI with bottle icon on Home.
- [ ] T10.3 Apply same top header to Home.
- [ ] T10.4 Apply same right navigation to Home.
- [ ] T10.5 Apply same global frame to Safety.
- [ ] T10.6 Apply same global frame to Manual.
- [ ] T10.7 Apply same global frame to CIP.
- [ ] T10.8 Apply same global frame to Run Out.
- [ ] T10.9 Apply same global frame to Settings.
- [ ] T10.10 Apply same global frame to Diagnostics.
- [ ] T10.11 Apply same global frame to Alarms.

**Pilot rule:** perform T10.1 through T10.4 first. T10.5 through T10.11 remain blocked until Home and Production receive visual approval.

---

## Phase 11 — Security

- [ ] T11.1 Operator access matrix.
- [ ] T11.2 Maintenance access matrix.
- [ ] T11.3 Engineer access matrix.
- [ ] T11.4 Auto-logout/session timeout.
- [ ] T11.5 Verify protected buttons cannot execute without correct role.
- [ ] T11.6 Confirm PLC still enforces all interlocks regardless of HMI role.
- [ ] T11.7 Confirm Operator routine Production/CIP/start/stop/reset remains password-free where already permitted.
- [ ] T11.8 Confirm Maintenance login protects diagnostics, setup, bypass, manual actuator and I/O/service actions.
- [ ] T11.9 Confirm Engineer login protects advanced configuration and engineering/service parameters.
- [ ] T11.10 Implement and verify inactivity return to Operator/default access using supported WinCC Unified user administration.

**Phase 11 is the regression verification of the Phase 1B security implementation; it is not permission to defer the preparation gate.**

---

## Phase 12 — Compile and Regression Test

- [ ] T12.1 HMI full rebuild.
- [ ] T12.2 PLC full rebuild.
- [ ] T12.3 Confirm 0 HMI errors.
- [ ] T12.4 Confirm 0 PLC errors.
- [ ] T12.5 Confirm 0 unresolved references.
- [ ] T12.6 Confirm all navigation links.
- [ ] T12.7 Confirm all existing alarms.
- [ ] T12.8 Confirm Production commands.
- [ ] T12.9 Confirm Manual commands.
- [ ] T12.10 Confirm CIP.
- [ ] T12.11 Confirm Run Out.
- [ ] T12.12 Confirm Settings.
- [ ] T12.13 Confirm Diagnostics.
- [ ] T12.14 Confirm user permissions.
- [ ] T12.15 Confirm HMI tag quality/bad-quality behavior.
- [ ] T12.16 Confirm no duplicate semantic HMI tags were introduced.
- [ ] T12.17 Confirm no PLC source or state-machine logic changed during UI refinement.
- [ ] T12.18 Verify valid TRUE/running states are solid green and do not blink.
- [ ] T12.19 Verify bad-quality data is visually distinct from valid OFF and valid numeric zero.

---

## Phase 13 — Visual Acceptance

- [ ] T13.1 Capture full-screen Production screenshot.
- [ ] T13.2 Compare with approved master reference.
- [ ] T13.3 Verify process mimic proportions.
- [ ] T13.4 Verify Home house icon.
- [ ] T13.5 Verify Safety icon.
- [ ] T13.6 Verify complete right-side navigation.
- [ ] T13.7 Verify speedometer.
- [ ] T13.8 Verify efficiency ring.
- [ ] T13.9 Verify bottle icon + produced-today count.
- [ ] T13.10 Verify vacuum in mbar.
- [ ] T13.11 Verify no clipping/overlap.
- [ ] T13.12 Obtain visual approval before propagating further design changes.
- [ ] T13.13 Run automated object-boundary audit and manually review every reported collision.
- [ ] T13.14 Confirm no page content enters the fixed header, navigation or bottom-bar regions.
- [ ] T13.15 Record approved deviations and unresolved `MISSING`/`AMBIGUOUS` bindings.

---

## Phase 14 — Deliverables

- [ ] T14.1 Updated TIA Portal project.
- [ ] T14.2 Updated HMI project.
- [ ] T14.3 SVG/graphic asset library.
- [ ] T14.4 Faceplate inventory.
- [ ] T14.5 PLC/HMI tag binding matrix.
- [ ] T14.6 Vacuum conversion note.
- [ ] T14.7 User-access matrix.
- [ ] T14.8 HMI compile evidence.
- [ ] T14.9 PLC compile evidence.
- [ ] T14.10 Before/after screenshots.
- [ ] T14.11 Change log.
- [ ] T14.12 Open-point register.
- [ ] T14.13 Validated SVG asset manifest with SHA-256 hashes.
- [ ] T14.14 Component parameter and dynamic-state matrix.
- [ ] T14.15 Navigation destination and icon matrix.
- [ ] T14.16 Home/Production visual approval record.
- [ ] T14.17 Evidence-based open-item register containing only `MISSING` and `AMBIGUOUS` items.
- [ ] T14.18 Preparation-gate review showing all required deliverables complete before pilot construction.
- [ ] T14.19 Recorded stop-condition decision for any requested human input.

---

## Implementation Gate — Mandatory Sequence

1. Complete Phase 0, Phase 1, Phase 1A and Phase 1B.
2. Review the validated asset manifest, binding matrix and open-item register.
3. Build only Home and Production.
4. Compile HMI and PLC and capture evidence.
5. Inspect both pilot pages at native resolution and 100% zoom for overlap, clipping, off-screen controls, tag quality and navigation consistency.
6. Capture Home/Production screenshots and a before/after comparison.
7. Stop for pilot visual approval.
8. Do not execute T10.5–T10.11 or any wider propagation until approval is recorded.

Human input may be requested only if signals remain indistinguishable after cross-reference review, a required signal is absent and would require PLC logic, a device identity conflicts with unavailable external documentation, or the installed WinCC Unified version cannot implement the approved design without a design change.
