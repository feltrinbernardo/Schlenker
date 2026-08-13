# Implementation Plan — External WinCC Unified HMI Development System

This plan is deliberately incremental. A checked task means its implementation,
tests, evidence and repository change-log entry are complete. It does not imply
authorization for a later TIA write gate.

## Phase 0 — Protect the project and establish the baseline

- [ ] 0.1 Record implementation prerequisites and safety boundaries
  - Document the approved `.ap19` path as operator-supplied local configuration.
  - Confirm TIA Portal V19 PublicAPI availability and compatible .NET tooling.
  - State that the tool has no online, transfer or download commands.
  - Define clean-project, exact-project, exact-HMI and exact-screen preconditions.
  - _Requirements: 1.1–1.7, 9.1–9.3, 13.6_

- [ ] 0.2 Preserve a fresh read-only baseline
  - Run the existing or adapted inspector without saving the TIA project.
  - Export the current HMI screen list and dimensions.
  - Export the complete current Production object inventory.
  - Record tags, PLC symbolic references, connections, events, dynamizations,
    authorizations and graphics exposed by V19 Openness.
  - Mark unexposed fields `API_NOT_EXPOSED`.
  - _Requirements: 8.1–8.4, 14.1–14.4_

- [ ] 0.3 Freeze baseline evidence
  - Store tool version, timestamps, project/HMI identity and input hashes.
  - Generate machine-readable and human-readable baseline reports.
  - Verify that no TIA save, checkpoint or repository-unrelated change occurred.
  - _Requirements: 1, 12.1–12.5_

## Phase 1 — Scaffold the external source system

- [ ] 1.1 Create the required `hmi-ui-src/` and `generated/` directories
  - Add every source folder and named file defined by the approved structure.
  - Add placeholder screen documents with explicit status, never fabricated tags.
  - Keep generated output separate from authored source.
  - _Requirements: 2.1, 12.5, 13.5_

- [ ] 1.2 Write `hmi-ui-src/README.md` and local command documentation
  - Explain edit, validate, preview, capture, diff and approval workflows.
  - Explain that browser values are simulated and TIA remains authoritative.
  - Document offline prerequisites and limitations.
  - _Requirements: 6.4–6.8, 13.1–13.7_

- [ ] 1.3 Create `project.json`
  - Record project ID, platform, device class, 1366 × 768 target, language,
    Production master and design-system version.
  - Exclude credentials and machine-specific project paths.
  - _Requirements: 1.7, 2.2, 12.2_

## Phase 2 — Define schemas and the canonical model

- [ ] 2.1 Implement source JSON Schemas
  - Create project, component and screen schemas.
  - Require stable IDs, TIA names/types, geometry, z-order and classification.
  - Model text, styles, graphics, rules, bindings, directions, dynamizations,
    events, authorizations, notes and managed fields.
  - Reject unknown required semantics instead of silently discarding them.
  - _Requirements: 2.2–2.8, 3.1–3.7_

- [ ] 2.2 Implement validation-report schema
  - Create `screen-validation.schema.json` for errors, warnings, information,
    source locations, affected IDs and suggested actions.
  - Create fixtures that prove valid and invalid reports.
  - _Requirements: 7.4–7.6_

- [ ] 2.3 Implement the canonical IR loader and component expander
  - Resolve component versions and typed parameters.
  - Resolve design tokens and binding references.
  - Namespace component children deterministically.
  - Serialize canonical JSON with stable property and object ordering.
  - _Requirements: 2, 4.3, 4.8, 12.2_

- [ ] 2.4 Add canonical-model correctness tests
  - Test deterministic normalization and hashing.
  - Test duplicate external IDs and duplicate TIA names in all object orders.
  - Test unknown type, token, component and binding rejection.
  - _Properties: P1, P3_

## Phase 3 — Extract and lock the Production design system

- [ ] 3.1 Convert the Production inventory into an external baseline model
  - Map each accessible native object and preserve exact current evidence.
  - Assign stable external IDs without changing native TIA names.
  - Create `screens/production.json` as an as-is baseline, not a correction.
  - Record unsupported attributes and TIA-only objects.
  - _Requirements: 8.2–8.6, 14.2–14.5_

- [ ] 3.2 Identify common-frame candidates and conflicts
  - Classify header, status, session, alarm, navigation and footer objects.
  - Report duplicates, overlaps, inconsistent dimensions and positions.
  - Separate current facts from proposed corrections.
  - _Requirements: 4.1–4.7, 14.3, 14.6_

- [ ] 3.3 Create design-system tokens from approved Production evidence
  - Populate colors, typography, spacing and geometry files.
  - Preserve Siemens Sans and approved blue/white palette.
  - Record literal exceptions for human review.
  - _Requirements: 4.4–4.6_

- [ ] 3.4 Create reusable common component definitions
  - Implement common header, status, right navigation and footer.
  - Define typed per-screen parameters only where variation is permitted.
  - Create supporting command, indicator, numeric and process-device components.
  - _Requirements: 4.2–4.4_

- [ ] 3.5 Obtain approval and lock common-frame version 1.0.0
  - Present the as-is inventory, candidate selection and every proposed difference.
  - Record the approved Production inventory and component hashes.
  - Do not perform a TIA write.
  - _Requirements: 4.7–4.8, 11.3–11.4, 14.6–14.8_

- [ ] 3.6 Test common-component equivalence
  - Expand components for each placeholder screen.
  - Prove protected common fields match the locked master except parameters.
  - _Property: P4_

## Phase 4 — Build the controlled SVG pipeline

- [ ] 4.1 Inventory approved SVG source assets
  - Copy or reference only controlled pump, valve, sensor, bottle and navigation
    assets authorized for the external source tree.
  - Assign stable logical asset names.
  - _Requirements: 5.1, 5.4_

- [ ] 4.2 Implement SVG structural and safety validation
  - Require an SVG root and valid `viewBox`.
  - Require transparent background.
  - Reject embedded process labels/values and unsupported external resources.
  - Reject missing or malformed asset files.
  - _Requirements: 5.2–5.3, 5.6_

- [ ] 4.3 Generate `asset-manifest.json`
  - Record logical name, relative path, media type, `viewBox`, SHA-256 and result.
  - Verify hash drift blocks dependent screens and approvals.
  - _Requirements: 5.4–5.5; Property P5_

## Phase 5 — Implement static validation

- [ ] 5.1 Implement identity, bounds and clipping checks
  - Validate positive dimensions and integer pixel geometry.
  - Reject any object extending outside 1366 × 768.
  - Detect duplicate IDs and TIA names after component expansion.
  - _Requirements: 7.1, 7.7; Properties P2, P3_

- [ ] 5.2 Implement overlap analysis
  - Detect positive-area rectangle intersections.
  - Support reviewed ID-pair allow-list entries with reasons.
  - Validate hidden as well as visible objects.
  - _Requirements: 7.1, 7.3_

- [ ] 5.3 Implement design-system consistency checks
  - Detect common-frame misalignment and hash divergence.
  - Detect button dimension, typography and palette deviations.
  - Distinguish approved token exceptions from accidental literals.
  - _Requirements: 4.6, 7.2_

- [ ] 5.4 Implement binding and behavior policy checks
  - Validate tag existence and evidence for `CONFIRMED` bindings.
  - Require explicit fallbacks for missing/unconfigured signals.
  - Report ambiguous candidates without selecting one.
  - Reject unauthorized binding, event, script or authorization changes.
  - _Requirements: 3.1–3.7, 7.1_

- [ ] 5.5 Produce per-screen reports
  - Emit schema-valid JSON and readable Markdown.
  - Aggregate unresolved bindings by classification.
  - Use non-zero exit codes for blocking findings.
  - _Requirements: 3.7, 7.4–7.6, 13.3–13.4_

## Phase 6 — Build the exact browser preview

- [ ] 6.1 Implement the local preview shell
  - Create a fixed 1366 × 768 stage with no CDN or network dependencies.
  - Add screen selection, review mode and engineering/debug mode.
  - Display the preview/runtime-authority disclaimer.
  - _Requirements: 6.1, 6.6–6.8_

- [ ] 6.2 Implement native-object preview renderers
  - Render the initial type registry from canonical IR.
  - Apply absolute geometry, z-order, text, fonts, alignment, styles and graphics.
  - Fail visibly for unsupported types rather than omitting them.
  - _Requirements: 6.1–6.2_

- [ ] 6.3 Implement debug overlays and validation links
  - Display external ID, TIA name, bounds, coordinates and z-order.
  - Color-code binding classifications and link findings to objects.
  - _Requirements: 6.3, 7.6_

- [ ] 6.4 Implement constrained simulation
  - Simulate only declared Boolean and numeric visual inputs.
  - Show persistent and per-value `SIMULATED` labelling.
  - Prove simulation state cannot enter source bindings or TIA output.
  - _Requirements: 6.4–6.5_

- [ ] 6.5 Implement deterministic screenshot capture
  - Detect an approved locally available browser/capture engine.
  - Capture exactly 1366 × 768 at scale 1.0 with browser chrome excluded.
  - Store capture metadata and source hash.
  - If offline capture tooling is unavailable, document the dependency and keep
    preview approval blocked rather than generating non-deterministic evidence.
  - _Requirements: 6.6, 11.3, 12.2_

- [ ] 6.6 Add renderer and screenshot tests
  - Use golden fixtures for representative object types and common components.
  - Test text clipping and missing asset visibility.
  - _Requirements: 6, 7.1_

## Phase 7 — Build the read-only TIA exporter and comparator

- [ ] 7.1 Refactor existing V19 inspection code behind a stable adapter
  - Isolate read-only PublicAPI access from write namespaces/commands.
  - Verify the installed V19 assemblies and supported object/property registry.
  - Add an offline environment-check command.
  - _Requirements: 1.4–1.6, 8.1–8.4, 13.6_

- [ ] 7.2 Implement project and screen inventory exports
  - Require exact project, HMI software and screen resolution.
  - Normalize screens, objects, graphics, tags, bindings, events and dynamics.
  - Preserve API errors and `API_NOT_EXPOSED` fields.
  - _Requirements: 8.1–8.4_

- [ ] 7.3 Implement conservative object matching
  - Match approved mapping first, then exact TIA name plus compatible type.
  - Report heuristic candidates but never use them for automatic writes.
  - _Requirements: 8.5–8.6, 9.4–9.5_

- [ ] 7.4 Implement field-level diff and apply-plan generation
  - Classify `CREATE`, `UPDATE`, `REUSE`, `UNCHANGED`, `REJECT` and `TIA_ONLY`.
  - Separate visual from binding/behavior differences.
  - Generate the external-ID-to-TIA-name mapping report.
  - _Requirements: 8.5–8.7, 12.3–12.4_

- [ ] 7.5 Add fixture-based exporter/comparator tests
  - Test duplicate names, incompatible types, API-unavailable fields and drift.
  - Prove an unchanged reconciled fixture produces no create/update plan.
  - _Property: P8_

## Phase 8 — Implement approval and ledger gates

- [ ] 8.1 Implement append-only ledger events and derived views
  - Enforce the defined page statuses and transitions.
  - Generate JSON and TSV current views from immutable transition events.
  - Record source revision, evidence, actor and reason.
  - _Requirements: 11.1–11.2, 12.5_

- [ ] 8.2 Implement approval packages
  - Hash canonical IR, components, tokens, bindings, assets, reports,
    screenshot, pre-inventory and diff.
  - Record approver identity, timestamp and exact scope.
  - Verify any changed hash invalidates approval.
  - _Requirements: 11.3–11.4; Property P6_

- [ ] 8.3 Test ledger and approval policies
  - Generate valid and invalid state sequences.
  - Prove import cannot occur without matching `PREVIEW_APPROVED` evidence.
  - Prove one blocked page does not mutate another page's state.
  - _Requirements: 11.5–11.6; Property P10_

## Phase 9 — Build the page-scoped importer/updater

- [ ] 9.1 Implement write-command safety envelope
  - Require exact identities, source revision, transaction ID and approval file.
  - Reject wildcard/list/all-page targets.
  - Add the distinct Production authorization check.
  - Ensure no device-online or transfer APIs are referenced by the assembly.
  - _Requirements: 1, 9.1, 9.3, 9.7_

- [ ] 9.2 Implement checkpoint creation and verification
  - Create a fresh `.zap19` through supported TIA functionality.
  - Record absolute path, timestamp and SHA-256 before apply.
  - Abort when checkpoint creation or verification fails.
  - _Requirements: 9.2, 9.11_

- [ ] 9.3 Implement precondition and drift checks
  - Reinspect the target page immediately before apply.
  - Compare project, page and object state with the approved inventory.
  - Abort on unapproved drift.
  - _Requirements: 9.1, 9.4–9.6_

- [ ] 9.4 Implement visual-only create/update/reuse operations
  - Support only object types/properties proven writable by V19 contract tests.
  - Set only approved managed visual fields.
  - Preserve unmanaged fields, bindings, events, scripts and authorizations.
  - Never delete TIA-only objects in release 1.
  - Journal each planned, completed or rejected operation.
  - _Requirements: 3.5–3.6, 9.4–9.8, 12.3; Property P7_

- [ ] 9.5 Implement fail-closed transaction handling
  - Stop immediately on API or object failure.
  - Mark only the target page blocked and retain partial journal evidence.
  - Never automatically proceed to another page or overwrite the checkpoint.
  - _Requirements: 9.8–9.11, 11.5_

## Phase 10 — Compile, reconcile and conditionally save

- [ ] 10.1 Implement HMI compilation for the active transaction
  - Invoke the supported HMI software compile path.
  - Parse and retain all diagnostics without hiding Siemens output.
  - Require exactly zero errors and zero warnings.
  - _Requirements: 9.10, 10.1–10.2_

- [ ] 10.2 Implement post-apply re-export and reconciliation
  - Compare managed fields, expected bindings and behavior with approved IR.
  - Produce final inventory, mapping and reconciliation reports.
  - Reject missing, extra or changed managed fields.
  - _Requirements: 10.3–10.5_

- [ ] 10.3 Implement conditional save/finalization
  - Save only after a clean compile and successful reconciliation.
  - Otherwise close/recover without save where the supported API permits and mark
    the page blocked with explicit recovery instructions.
  - Never restore a checkpoint automatically over the authoritative project.
  - _Requirements: 9.9–9.11, 10.2–10.6_

- [ ] 10.4 Test idempotence and page isolation on a disposable project
  - Apply a visual-only test page, compile, reconcile and regenerate its plan.
  - Require no second create/update operation.
  - Compare all accessible non-target page inventories before and after.
  - _Properties: P8, P9_

- [ ] 10.5 Rehearse blocked-transaction recovery on a disposable project
  - Force a compile warning/error and a mid-apply rejection.
  - Confirm no save/finalization and retain complete evidence.
  - Verify documented manual recovery from the fresh checkpoint.
  - _Requirements: 9.8–9.11, 10.1–10.5_

## Phase 11 — Complete the Production read-only pilot

- [ ] 11.1 Validate the as-is Production external model
  - Require schema, geometry, common-frame, asset and binding reports.
  - Resolve no missing or ambiguous binding by inference.
  - _Requirements: 3, 5, 7, 14.4–14.5_

- [ ] 11.2 Generate Production preview and debug evidence
  - Render at exactly 1366 × 768.
  - Produce review and engineering screenshots.
  - Clearly label any simulated review state.
  - _Requirements: 6, 14.5_

- [ ] 11.3 Generate the complete Production difference package
  - Compare current TIA inventory with the external baseline/proposal.
  - List every visual, binding and behavioral difference.
  - List all `MISSING`, `AMBIGUOUS` and `NOT_CONFIGURED` bindings.
  - Include common-frame candidates/conflicts and proposed resolutions.
  - _Requirements: 3.7, 8.5–8.7, 14.6_

- [ ] 11.4 Stop at the human approval gate
  - Record preview approval only if the reviewer accepts the exact source hashes.
  - Do not apply any Production correction in this phase.
  - _Requirements: 4.7, 11.3–11.4, 14.7_

## Phase 12 — Prove the write pipeline before Production

- [ ] 12.1 Select a disposable project copy and one low-risk visual test object
  - Ensure the target is offline and not the authoritative Production screen.
  - Prepare an exact visual-only diff with no binding or event changes.
  - Obtain the separate transaction approval.
  - _Requirements: 1, 9, 11_

- [ ] 12.2 Execute one checkpointed, page-scoped test transaction
  - Checkpoint, apply, compile, reconcile and conditionally save.
  - Require zero HMI errors and warnings.
  - Capture all transaction and page-isolation evidence.
  - _Requirements: 9, 10, 12_

- [ ] 12.3 Review the test result and lock importer release 1
  - Resolve all tool defects found during the disposable test.
  - Repeat only the failed test page as necessary.
  - Version the proven capability registry and importer.
  - _Requirements: 9.9, 11.5, 13_

## Phase 13 — Production write gate (separately authorized)

- [ ] 13.1 Obtain explicit Production transaction authorization
  - Reference the approved preview, source revision, complete diff and checkpoint
    plan.
  - Reconfirm that the requested change is visual-only and page-scoped.
  - _Requirements: 9.7, 11.3–11.4, 14.7_

- [ ] 13.2 Execute exactly one approved Production page transaction
  - Create the fresh checkpoint.
  - Recheck drift, apply only approved fields, compile and reconcile.
  - Save only with zero errors, zero warnings and successful reconciliation.
  - _Requirements: 9, 10_

- [ ] 13.3 Produce and review final Production evidence
  - Capture post-import inventory, compile output, mapping, screenshot references,
    unresolved binding list and final result.
  - Mark `VALIDATED` only when every acceptance gate passes.
  - _Requirements: 10.3–10.6, 11, 12_

## Phase 14 — Migrate remaining screens one at a time

- [ ] 14.1 Create baseline JSON for each planned screen
  - Process `home`, `operate`, `cip`, `manual`, `alarms`, `diagnostics`,
    `efficiency` and `setup` independently.
  - Reference the locked Production common-frame version.
  - Preserve current bindings/events and unresolved classifications.
  - _Requirements: 2–4, 14.8_

- [ ] 14.2 Run the complete page workflow independently
  - Inspect → design → static validation → preview approval → checkpoint → apply
    → compile → reconcile → evidence.
  - Never batch pages and never rerun already validated pages due to another
    page's failure.
  - _Requirements: 9.3, 9.9, 10, 11.5_

- [ ] 14.3 Maintain the completion ledger and consolidated reports
  - Update per-page state only through valid transitions.
  - Regenerate mapping and unresolved-binding reports.
  - Keep previous transaction evidence immutable.
  - _Requirements: 3.7, 11, 12_

## Phase 15 — Release and repository governance

- [ ] 15.1 Add VS Code tasks for safe local workflows
  - Provide environment check, validate, preview, capture, inspect and diff tasks.
  - Keep write commands out of default/build-on-save tasks.
  - _Requirements: 1.6, 13.1–13.2, 13.7_

- [ ] 15.2 Add offline continuous-integration validation
  - Run schema, model, SVG, geometry, policy and report tests without TIA.
  - Keep TIA integration tests in a separately invoked host-only suite.
  - _Requirements: 12.7, 13.4_

- [ ] 15.3 Complete release documentation
  - Document support matrix, known API/rendering limitations and recovery steps.
  - Document how to add a screen, component, token, SVG and confirmed binding.
  - Document how to interpret every report and ledger state.
  - _Requirements: 6.7, 9.11, 13.3, 13.6_

- [ ] 15.4 Record repository changes for every implementation request
  - Use `scripts/write-change-log.ps1` before reporting each mutating request
    complete.
  - Include affected files and use `partial`/`blocked` when appropriate.
  - _Requirement: 12.6_

## Initial stopping point

For the first implementation iteration, complete Phases 0 through 7 and the
read-only parts of Phase 11. Stop after producing the Production preview and
difference package. Do not execute Phases 9, 10, 12, 13 or any TIA write without a
new, explicit approval covering that transaction.
