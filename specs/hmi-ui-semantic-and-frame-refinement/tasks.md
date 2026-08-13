# Tasks — HMI Semantic and Global-Frame Refinement

Date: 2026-08-13  
Status: draft; specification started, implementation not started

No task checkbox may be marked complete without the evidence named by that
task. The recorded Production approval does not bypass checkpoint, hash,
compile, reconciliation, or page-isolation gates.

## Phase 0 — Specification and authority

- [x] T0.1 Record the UI refinement findings in
  `docs/HMI_UI_REFINEMENT_FINDINGS.md`.
- [x] T0.2 Create `requirements.md`, `design.md`, and `tasks.md` for this
  refinement.
- [x] T0.3 Record the operator's explicit intent approval for a future write to
  the named `production` screen.
- [ ] T0.4 Review and approve the exact normative specification version/hash.
- [ ] T0.5 Confirm the exact TIA project and HMI identity for the Production
  transaction.
- [ ] T0.6 Record the exact allowed Production object/property scope.
- [ ] T0.7 Record explicit exclusions for bindings, events, scripts,
  authorizations, alarms, PLC, Safety, interlocks, networks, and hardware.

## Phase 1 — Fresh read-only evidence

- [ ] T1.1 Export a fresh read-only inventory of the exact `production` screen
  through supported TIA Portal V19 Openness APIs.
- [ ] T1.2 Record the project, HMI, screen, source, and inventory hashes.
- [ ] T1.3 Inventory all Production objects, native types, geometry, styles,
  text, graphics, visibility, enabled state, dynamizations, and events exposed
  by the supported API.
- [ ] T1.4 Reconfirm all referenced HMI tags and PLC mappings.
- [ ] T1.5 Reconfirm the 33 currently resolved Production tag references.
- [ ] T1.6 Reconfirm `MISSING`, `AMBIGUOUS`, and `NOT CONFIGURED` items.
- [ ] T1.7 Reconfirm that Produced Today has no approved daily/resettable source
  before preserving its unavailable state.
- [ ] T1.8 Reconfirm product-pressure source status.
- [ ] T1.9 Reconfirm M103 feedback, ready, fault, speed, mode, and communication
  signals individually.
- [ ] T1.10 Record whether supported data-quality evidence is available for each
  target state/value.
- [ ] T1.11 Capture fresh read-only signatures for every non-target screen.

## Phase 2 — Canonical-frame decision

- [ ] T2.1 Export the latest validated non-Production common-frame geometry.
- [ ] T2.2 Compare it field-by-field with Production.
- [ ] T2.3 Identify duplicate, overlapping, footer-overflow, and displaced
  common-frame objects.
- [ ] T2.4 Produce the proposed canonical frame object/property list.
- [ ] T2.5 Verify all existing navigation target scripts and selected-page
  behavior.
- [ ] T2.6 Confirm icon overlays remain non-interactive.
- [ ] T2.7 Review and approve the canonical-frame hash.
- [ ] T2.8 Freeze the approved common-frame source as immutable transaction
  evidence.

## Phase 3 — Semantic-state design

- [ ] T3.1 Inventory every Production Boolean indicator and classify it as
  healthy feedback, active state, command, warning, alarm/fault, or unknown.
- [ ] T3.2 Correct the proposed `Alarm_Low_Vacuum` true-state mapping from green
  to red.
- [ ] T3.3 Correct the proposed `Alarm_Critical` true-state mapping from green
  to red.
- [ ] T3.4 Define and approve the false-state appearance for both alarm lamps.
- [ ] T3.5 Verify that no other warning/alarm Boolean maps true to green.
- [ ] T3.6 Identify command-only labels that currently imply feedback.
- [ ] T3.7 Propose wording corrections without changing tag references.
- [ ] T3.8 Define the bad-quality/disconnected pattern only where supported
  quality evidence exists.
- [ ] T3.9 Verify that normal active/healthy states do not blink.

## Phase 4 — Production visual design

- [ ] T4.1 Align the process mimic to the approved grid without changing process
  topology.
- [ ] T4.2 Standardize pipe stroke and arrow style using supported V19 objects.
- [ ] T4.3 Verify equipment code placement and legibility.
- [ ] T4.4 Preserve all mimic graphics as non-commanding.
- [ ] T4.5 Design a V19-compatible radial speed gauge driven by
  `Speed_Actual_Pct` and labelled as percent.
- [ ] T4.6 Preserve `Speed_Setpoint_BPH` as a separate BPH value.
- [ ] T4.7 Remove the unapproved percent-to-BPH actual conversion from the
  proposed design.
- [ ] T4.8 Design a dynamic efficiency ring using the confirmed run/total-time
  formula.
- [ ] T4.9 Verify `N/A` behavior for zero/invalid efficiency denominator.
- [ ] T4.10 Preserve Produced Today as `N/A`/`NOT CONFIGURED` unless a genuine
  counter is confirmed.
- [ ] T4.11 Preserve product pressure as `NOT CONFIGURED` unless its complete
  interface is confirmed.
- [ ] T4.12 Preserve each unavailable M103 state explicitly.
- [ ] T4.13 Render and review every changed SVG at its intended HMI size.

## Phase 5 — Offline source and validation

- [ ] T5.1 Build the proposed Production source from the fresh baseline.
- [ ] T5.2 Limit managed fields to the approved visual properties.
- [ ] T5.3 Preserve all bindings, events, scripts, authorizations, and unmanaged
  properties outside the approved diff.
- [ ] T5.4 Run schema and object-identity validation.
- [ ] T5.5 Run screen-boundary validation at 1366 x 768.
- [ ] T5.6 Run interactive hit-area overlap validation.
- [ ] T5.7 Run foreground text/value overlap and clipping validation.
- [ ] T5.8 Run asset integrity and hash validation.
- [ ] T5.9 Run binding existence and readiness validation.
- [ ] T5.10 Run the alarm-semantic validation.
- [ ] T5.11 Run missing-data/no-fabrication validation.
- [ ] T5.12 Produce a deterministic offline preview and capture for review.
- [ ] T5.13 Record preview limitations; do not treat it as TIA runtime proof.

## Phase 6 — Diff and exact approval envelope

- [ ] T6.1 Generate a conservative diff against the fresh Production baseline.
- [ ] T6.2 Reject unexpected create, delete, behavior, binding, event, script,
  security, alarm, or authorization changes.
- [ ] T6.3 Review every proposed object/property change.
- [ ] T6.4 Record the exact screen-source hash.
- [ ] T6.5 Record the exact diff-plan hash.
- [ ] T6.6 Record the exact capability/executor/configuration hashes.
- [ ] T6.7 Create a fresh `.zap19` checkpoint through supported V19 Openness.
- [ ] T6.8 Verify and record the checkpoint hash.
- [ ] T6.9 Create the immutable transaction directory and manifest.
- [ ] T6.10 Bind the existing Production intent approval to the exact hashes, or
  obtain renewed explicit Production approval if binding requires a new human
  confirmation.
- [ ] T6.11 Record the required explicit Production acknowledgement in the
  transaction envelope.
- [ ] T6.12 Confirm all V19 mutation capabilities required by the exact diff are
  enabled by approved runtime-proof evidence; otherwise stop.

## Phase 7 — Production apply

- [ ] T7.1 Revalidate the project, HMI, and exact `production` target identity.
- [ ] T7.2 Revalidate all approval and checkpoint hashes immediately before
  apply.
- [ ] T7.3 Confirm hardware communication remains disabled.
- [ ] T7.4 Apply only the approved Production operations through supported TIA
  Portal V19 Openness APIs.
- [ ] T7.5 Append every operation and result to the transaction journal.
- [ ] T7.6 Abort immediately on an unsupported property, unexpected object,
  hash drift, or behavior difference.
- [ ] T7.7 Do not save at the end of apply; proceed first to compile and
  reconciliation.

## Phase 8 — Compile and reconciliation

- [ ] T8.1 Compile the HMI through the supported TIA Portal V19 API.
- [ ] T8.2 Require exactly zero compile errors.
- [ ] T8.3 Require exactly zero compile warnings.
- [ ] T8.4 Export the post-apply Production inventory.
- [ ] T8.5 Reconcile every managed value against the approved source.
- [ ] T8.6 Verify all preserved bindings, events, scripts, authorizations, and
  unmanaged properties.
- [ ] T8.7 Verify no unexpected object creation or deletion.
- [ ] T8.8 Export post-apply signatures for every screen.
- [ ] T8.9 Verify every non-target screen signature is unchanged.
- [ ] T8.10 Verify zero unresolved symbolic references.
- [ ] T8.11 Save only if T8.1 through T8.10 pass.
- [ ] T8.12 Record the final saved-project evidence without opening an online or
  hardware path.

## Phase 9 — Production visual acceptance

- [ ] T9.1 Capture Production at 1366 x 768 and 100% zoom using the approved
  offline/runtime evidence workflow.
- [ ] T9.2 Compare the frame with the approved canonical frame.
- [ ] T9.3 Verify all primary navigation objects remain in the right rail.
- [ ] T9.4 Verify no navigation object occupies the footer.
- [ ] T9.5 Verify header, date/time, alarm, and user/profile alignment.
- [ ] T9.6 Verify no clipping, unintended overlap, or off-screen objects.
- [ ] T9.7 Verify true alarm states display red, not green.
- [ ] T9.8 Verify inactive/healthy/warning/bad-quality states are distinct.
- [ ] T9.9 Verify speed actual and speed setpoint use correct and separate units.
- [ ] T9.10 Verify efficiency text and ring agree.
- [ ] T9.11 Verify Produced Today, product pressure, and missing M103 states do
  not display fabricated live values.
- [ ] T9.12 Obtain explicit Production visual acceptance for the exact capture
  and hashes.

## Phase 10 — Home pilot

- [ ] T10.1 Generate a fresh read-only Home baseline after Production approval.
- [ ] T10.2 Define an exact Home-only diff using the approved global frame.
- [ ] T10.3 Obtain separate Home screen approval and checkpoint/hash evidence.
- [ ] T10.4 Apply, compile, reconcile, and save Home under a separate
  one-screen transaction.
- [ ] T10.5 Capture Home at 1366 x 768 and 100% zoom.
- [ ] T10.6 Obtain Home visual acceptance.

## Phase 11 — Controlled rollout

- [ ] T11.1 Confirm both Production and Home pilot approvals are complete.
- [ ] T11.2 Prioritize remaining screens based on frame drift and operational
  risk.
- [ ] T11.3 Create one baseline, diff, approval, checkpoint, transaction, compile,
  reconciliation, and capture package per screen.
- [ ] T11.4 Never combine multiple screens into one mutation transaction.
- [ ] T11.5 Stop rollout on the first unexpected behavior or V19 capability
  conflict.

## Phase 12 — Completion evidence

- [ ] T12.1 Publish the approved specification hashes.
- [ ] T12.2 Publish the immutable Production transaction manifest.
- [ ] T12.3 Publish checkpoint, diff, apply, compile, reconciliation, isolation,
  and capture evidence.
- [ ] T12.4 Update the HMI screen completion ledger.
- [ ] T12.5 Record all material repository changes in the mandatory append-only
  change log.
- [ ] T12.6 Record unresolved items only as evidence-backed `MISSING` or
  `AMBIGUOUS` entries.
- [ ] T12.7 Confirm no hardware communication occurred.

## Current gate status

| Gate | Status | Evidence/next action |
|---|---|---|
| Findings recorded | PASS | `docs/HMI_UI_REFINEMENT_FINDINGS.md` |
| Specification created | PASS | This specification directory |
| Production intent approval | RECORDED | User approval on 2026-08-13 |
| Exact specification hash approval | PENDING | T0.4 |
| Fresh Production baseline | PENDING | Phase 1 |
| Canonical frame approval | PENDING | Phase 2 |
| Exact diff approval hashes | PENDING | Phase 6 |
| Fresh `.zap19` checkpoint | PENDING | T6.7-T6.8 |
| V19 mutation runtime proof | PENDING VERIFICATION | T6.12 |
| Production write | NOT STARTED | Phase 7 |
| Compile/reconciliation/save | NOT STARTED | Phase 8 |
| Production visual acceptance | NOT STARTED | Phase 9 |

