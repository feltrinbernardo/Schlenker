# Requirements Document — External WinCC Unified HMI Development System

## 1. Introduction

This specification defines an offline, repository-based system for designing,
reviewing, validating and generating native Siemens WinCC Unified screens outside
TIA Portal. Engineers will edit structured JSON and review a pixel-accurate browser
preview in VS Code. TIA Portal V19 remains the authoritative compiler and final
runtime renderer.

The target project is **Schlenker Monoblock Real Juice**, using an MTP1500 Unified
Comfort panel at **1366 × 768**. The system will be implemented with local source
code and Siemens TIA Portal Openness APIs; it will not require T-IA Connect or any
other paid third-party runtime.

This document uses Kiro-style user stories and testable acceptance criteria. The
keywords **SHALL**, **SHALL NOT**, **SHOULD** and **MAY** are normative.

## 2. Scope and governing constraints

The system includes:

- read-only TIA screen, object, asset and binding inspection;
- a version-controlled external HMI source model;
- a reusable design system and common-frame components;
- a local 1366 × 768 browser preview;
- static validation and evidence generation;
- a page-scoped TIA Openness exporter, comparator and importer/updater;
- compilation, reconciliation and completion-ledger reporting.

The system does not authorize changes to PLC logic, hardware configuration,
safety logic, network configuration, alarms, tags, scripts, security,
interlocks or working command behavior. It does not authorize connecting to or
downloading to PLC or HMI hardware.

## 3. Glossary

- **External model**: the reviewed JSON representation of screens, components,
  assets and bindings.
- **Authoritative project**: the named TIA Portal V19 project supplied by the
  operator.
- **Production**: the existing Production HMI screen and approved visual master.
- **Common frame**: header, status area, user/session display, alarm indication,
  right navigation, footer and communication status shared by screens.
- **Native object**: a WinCC Unified screen item created through supported TIA
  Portal Openness APIs.
- **Binding classification**: `CONFIRMED`, `MISSING`, `AMBIGUOUS` or
  `NOT_CONFIGURED`.
- **Checkpoint**: a fresh `.zap19` archive created through supported TIA
  mechanisms before a write transaction.
- **Page transaction**: one explicitly named screen, one checkpoint, one apply,
  one compile and one result record.
- **Preview data**: synthetic Boolean or numeric values used only to inspect the
  appearance of the browser preview.

## 4. Requirements

### Requirement 1 — Offline and non-destructive operation

**User story:** As the project owner, I want the toolchain isolated from field
hardware and unsupported project manipulation so that HMI development cannot
affect the running machine.

#### Acceptance criteria

1. THE SYSTEM SHALL operate without internet access after its repository and
   approved local dependencies are present.
2. THE SYSTEM SHALL NOT initiate PLC or HMI discovery, online access, monitoring,
   transfer, download, run/stop or device-memory operations.
3. THE SYSTEM SHALL NOT read, patch, unzip, rewrite or otherwise manipulate the
   internal binary contents of an `.ap19` file.
4. WHEN TIA project access is required, THE SYSTEM SHALL use only supported TIA
   Portal V19 Openness APIs.
5. THE SYSTEM SHALL refuse a write mode if the selected project, HMI software or
   target screen cannot be identified unambiguously.
6. THE SYSTEM SHALL default every command that can access TIA Portal to read-only
   mode unless the operator supplies an explicit write subcommand and screen name.
7. THE SYSTEM SHALL redact credentials and SHALL NOT store passwords, tokens or
   secrets in source files, logs or evidence.

### Requirement 2 — External source-of-truth model

**User story:** As an HMI engineer, I want every screen represented as structured,
version-controlled data so that I can review and change it in VS Code.

#### Acceptance criteria

1. THE SYSTEM SHALL maintain the source folders and files defined in the design
   document under `hmi-ui-src/` and generated evidence under `generated/`.
2. EACH screen document SHALL declare its name, target resolution and ordered
   collection of objects.
3. EACH object SHALL declare a stable object ID/name, native WinCC Unified type,
   X/Y position, width, height and z-order.
4. EACH object SHALL be able to declare text and language, typography, alignment,
   foreground, background, border, SVG/graphic reference, visibility rule,
   enable rule, HMI tag binding, PLC symbolic reference, direction,
   dynamization, event handlers, required authorization, engineering notes and
   binding classification.
5. THE SYSTEM SHALL validate all external source documents against versioned JSON
   Schemas before preview or import.
6. THE SYSTEM SHALL reject duplicate object IDs within a screen and duplicate tag
   definitions within the tag catalogue. Multiple read-only objects MAY reference
   the same confirmed tag, but the audit SHALL report that reuse.
7. THE SYSTEM SHALL preserve stable IDs across export/import cycles and SHALL NOT
   derive identity solely from screen coordinates or visible text.
8. THE SYSTEM SHALL represent an unknown value explicitly and SHALL NOT replace it
   with an inferred tag, address, event or authorization.

### Requirement 3 — Binding truth and classification

**User story:** As a controls engineer, I want every signal traceable to the
existing TIA project so that generated screens never invent machine behavior.

#### Acceptance criteria

1. EACH binding SHALL be classified as `CONFIRMED`, `MISSING`, `AMBIGUOUS` or
   `NOT_CONFIGURED`.
2. A binding SHALL be `CONFIRMED` only when the HMI tag exists and its PLC symbolic
   reference, connection, data type and intended direction are supported by
   exported project evidence.
3. WHEN no suitable tag exists, THE SYSTEM SHALL display `N/A`, `MISSING` or
   `NOT CONFIGURED` and SHALL prevent generation of a fabricated live binding.
4. WHEN multiple plausible signals exist, THE SYSTEM SHALL display `AMBIGUOUS`,
   list the candidates in the report and require a human decision.
5. THE SYSTEM SHALL compare proposed events, commands, authorizations and
   dynamizations with the existing object before permitting a behavioral change.
6. THE SYSTEM SHALL preserve existing bindings and events unless an approved
   external definition explicitly requests the same field to change.
7. THE SYSTEM SHALL produce a consolidated list of every `MISSING`, `AMBIGUOUS`
   and `NOT_CONFIGURED` item.

### Requirement 4 — Production-derived design system

**User story:** As a UI engineer, I want a single reusable common frame based on
the approved Production screen so that every page remains visually consistent.

#### Acceptance criteria

1. BEFORE defining common components, THE SYSTEM SHALL perform a current read-only
   export of the Production screen.
2. THE SYSTEM SHALL extract and define once the approved header, machine status,
   time/date, current Unified user/session display, alarm indication, right-side
   navigation, footer and communication status.
3. SCREEN documents SHALL reference common-component definitions instead of
   independently duplicating those definitions.
4. ONLY the central page-content region MAY vary between standard screens.
5. THE SYSTEM SHALL preserve Siemens Sans typography and the approved blue/white
   palette unless a future approved design-system revision explicitly changes it.
6. THE SYSTEM SHALL compare every common-component instance with the locked
   Production master and report any geometry, style, text, ordering or binding
   divergence.
7. THE SYSTEM SHALL NOT correct or write the Production screen until a human has
   approved a complete inventory and difference report.
8. WHEN the Production master changes through an approved transaction, THE SYSTEM
   SHALL create a new design-system version and SHALL NOT silently mutate the
   meaning of an existing version.

### Requirement 5 — Controlled graphic assets

**User story:** As a UI engineer, I want reusable, validated SVG assets so that
graphics remain scalable and traceable in both preview and WinCC Unified.

#### Acceptance criteria

1. THE SYSTEM SHALL support controlled SVG assets for pumps, valves, sensors,
   bottles and navigation icons.
2. EACH SVG SHALL use a `viewBox`, have a transparent background and contain no
   embedded process labels or process values.
3. EACH SVG SHALL pass structural and safety validation before use.
4. THE ASSET MANIFEST SHALL record the relative path, logical name, media type,
   dimensions or `viewBox`, SHA-256 hash and validation result.
5. WHEN an asset is absent or its current hash differs from the approved manifest,
   validation SHALL fail for screens that reference it.
6. Text intended for localization or live data SHALL remain a native WinCC Unified
   text or I/O object rather than SVG text.

### Requirement 6 — Exact local preview

**User story:** As a reviewer, I want to view the same screen definition at the
panel's exact resolution in a browser so that visual issues can be found before
opening TIA Portal.

#### Acceptance criteria

1. THE PREVIEW SHALL render a 1366 × 768 stage using absolute pixel geometry from
   the same JSON consumed by the TIA generator.
2. THE PREVIEW SHALL implement object z-order, visibility, enabled state, text,
   fonts, alignment, colors, borders and graphic references supported by the
   external model.
3. THE PREVIEW SHALL provide an engineering/debug mode that overlays object names,
   bounds, coordinates, z-order and binding classification.
4. THE PREVIEW MAY simulate Boolean and numeric values for visual review only.
5. EVERY simulated value SHALL be visibly labelled `SIMULATED` and SHALL never be
   written into production bindings or TIA tag definitions.
6. THE PREVIEW SHALL support deterministic capture at exactly 1366 × 768 without
   browser chrome in the captured image.
7. THE PREVIEW SHALL warn that browser rendering is an approximation and that TIA
   Portal compilation/runtime remains authoritative.
8. THE PREVIEW SHALL work from a local server or generated bundle without internet
   access, remote fonts, CDN resources or cloud services.

### Requirement 7 — Static validation

**User story:** As a reviewer, I want automated geometry and consistency checks so
that common layout defects are blocked before a TIA transaction.

#### Acceptance criteria

1. VALIDATION SHALL detect object overlap, clipping, coordinates outside the
   1366 × 768 bounds, duplicate IDs, missing assets and missing bindings.
2. VALIDATION SHALL detect misaligned common components, inconsistent command or
   navigation button dimensions, and incorrect font or palette use.
3. OVERLAP rules SHALL support explicit allow-list entries for intentional
   containment or layering and SHALL fail unapproved intersections.
4. VALIDATION SHALL distinguish errors, warnings and informational findings.
5. A screen SHALL NOT become eligible for human preview approval while static
   validation contains any error.
6. VALIDATION SHALL produce both a machine-readable report and a human-readable
   report scoped to the named screen.
7. THE SYSTEM SHALL validate all coordinates, sizes and z-orders using deterministic
   integer pixel rules.

### Requirement 8 — Read-only TIA inventory and comparison

**User story:** As a TIA engineer, I want a complete read-only export and diff so
that every proposed change is understood before applying it.

#### Acceptance criteria

1. THE EXPORTER SHALL list every existing HMI screen and its target dimensions.
2. FOR a named screen, THE EXPORTER SHALL inventory object names, native types,
   geometry, z-order, supported styles, text, graphics, bindings,
   dynamizations, events and authorizations that the V19 API exposes.
3. THE EXPORTER SHALL inventory referenced HMI tags, PLC symbolic references and
   graphic assets without modifying the project.
4. WHEN an attribute is not exposed by the V19 Openness API, THE EXPORTER SHALL
   record it as `API_NOT_EXPOSED` rather than guessing.
5. THE COMPARATOR SHALL classify external objects as `CREATE`, `UPDATE`, `REUSE`,
   `UNCHANGED`, `REJECT` or `TIA_ONLY`.
6. THE COMPARATOR SHALL produce field-level differences and identify any proposed
   behavioral or binding change separately from visual changes.
7. THE PRODUCTION pilot SHALL produce a complete difference report and SHALL stop
   before any write until the operator records explicit approval.

### Requirement 9 — Page-scoped TIA Openness transaction

**User story:** As the project owner, I want tightly bounded imports so that a
failure on one page cannot rebuild or damage other pages.

#### Acceptance criteria

1. EVERY write command SHALL require the exact project identity, HMI software
   identity, screen name, approved source revision and approval evidence ID.
2. BEFORE modifying an in-memory TIA project, THE SYSTEM SHALL create a fresh
   `.zap19` checkpoint and record its path and SHA-256 hash.
3. A TRANSACTION SHALL modify only one explicitly named screen and SHALL refuse a
   wildcard, list or all-screens target.
4. THE IMPORTER SHALL reuse an existing TIA object when its stable ID/name and
   expected type match, and SHALL create only an object confirmed missing.
5. THE IMPORTER SHALL refuse duplicate object names and SHALL refuse an incompatible
   type change unless an explicit, reviewed migration is approved.
6. THE IMPORTER SHALL preserve fields not managed by the approved source revision.
7. THE IMPORTER SHALL refuse any Production write unless the command contains a
   distinct Production authorization referencing the approved difference report.
8. THE TRANSACTION SHALL maintain a journal of intended and completed operations
   so that a partial failure is detectable.
9. IF apply or compilation fails, THE SYSTEM SHALL stop that page, SHALL NOT run
   another page automatically and SHALL retain evidence needed for recovery.
10. THE SYSTEM SHALL NOT save the authoritative TIA project unless the HMI compile
    result is zero errors and zero warnings and post-apply reconciliation passes.
11. WHERE TIA Openness cannot provide atomic rollback, THE SYSTEM SHALL state this
    limitation, require a clean project precondition and use the checkpoint-backed
    recovery procedure defined in the design.

### Requirement 10 — Compile and post-import reconciliation

**User story:** As a commissioning engineer, I want TIA to verify every generated
page so that external validation never substitutes for the real compiler.

#### Acceptance criteria

1. AFTER applying a named page, THE SYSTEM SHALL compile the target HMI software
   through supported TIA Portal functionality.
2. A page SHALL be eligible for `COMPILED` only when compilation reports zero
   errors and zero warnings.
3. AFTER a clean compile, THE SYSTEM SHALL export the resulting page again and
   compare it with the approved external JSON.
4. A page SHALL be eligible for `VALIDATED` only when required managed fields
   reconcile, expected bindings remain intact and no unapproved behavioral change
   exists.
5. THE SYSTEM SHALL retain compile output, the post-import inventory, mapping report
   and final screenshot/evidence references under that page transaction.
6. THE SYSTEM SHALL NOT mark browser visual equivalence as proof of WinCC Unified
   runtime equivalence.

### Requirement 11 — Approval workflow and completion ledger

**User story:** As the project approver, I want explicit gates and per-page status
so that no automated step crosses a review boundary.

#### Acceptance criteria

1. EACH page SHALL have exactly one current status from `NOT_STARTED`,
   `IN_PROGRESS`, `PREVIEW_APPROVED`, `IMPORTED`, `COMPILED`, `VALIDATED` or
   `BLOCKED`.
2. THE SYSTEM SHALL record state transitions with timestamp, source revision,
   evidence references, operator/approver identifier and reason.
3. THE SYSTEM SHALL require human visual approval of a deterministic preview
   screenshot before enabling any TIA write for that source revision.
4. AN approval SHALL become invalid when the approved screen JSON, referenced
   component, design token, binding or asset hash changes.
5. IF one page fails, THE SYSTEM SHALL mark only that page `BLOCKED`; completed
   pages SHALL NOT be rebuilt automatically.
6. THE SYSTEM SHALL reject invalid status transitions, including direct movement
   from `IN_PROGRESS` to `IMPORTED` without `PREVIEW_APPROVED`.

### Requirement 12 — Evidence, auditability and repository governance

**User story:** As an auditor, I want reproducible evidence and traceability so
that I can determine exactly what source produced each TIA change.

#### Acceptance criteria

1. EACH tool execution SHALL have a unique run or transaction ID.
2. GENERATED evidence SHALL include tool version, source Git commit, project
   identity, screen, timestamps, input hashes and result status.
3. A write transaction log SHALL list every created, changed, reused, unchanged
   and rejected object with field-level reasons.
4. THE SYSTEM SHALL produce a mapping from external stable IDs to final TIA object
   names.
5. GENERATED evidence SHALL be immutable for the duration of a transaction and
   SHALL use new transaction directories rather than overwriting earlier evidence.
6. EVERY repository mutation made while implementing this specification SHALL be
   recorded through the repository's mandatory append-only change-log mechanism.
7. THE SYSTEM SHALL support deterministic validation in continuous integration
   without requiring TIA Portal; TIA integration tests MAY run only on an approved
   Windows engineering host with V19 installed.

### Requirement 13 — Reliability and usability

**User story:** As an engineer working in VS Code, I want predictable commands and
actionable diagnostics so that I can use the system without learning TIA's object
model for every edit.

#### Acceptance criteria

1. THE TOOLCHAIN SHALL expose documented commands for inspect, validate, preview,
   capture, diff, checkpoint, apply, compile, reconcile and report operations.
2. READ-ONLY commands SHALL be usable independently from write commands.
3. EVERY failure SHALL name the screen, object or source file when applicable and
   provide an actionable reason without suppressing the underlying TIA diagnostic.
4. THE SYSTEM SHALL return a non-zero process exit code for validation, policy,
   compile or reconciliation failures.
5. THE SYSTEM SHALL keep generated files separate from hand-authored source files.
6. THE SYSTEM SHALL document local prerequisites and provide an offline environment
   check before any TIA interaction.
7. THE SYSTEM SHOULD support VS Code tasks for common read-only workflows without
   making the extension or editor itself a runtime dependency.

### Requirement 14 — Production pilot baseline

**User story:** As the project owner, I want Production handled as a protected
pilot so that its common frame becomes a trustworthy master for later screens.

#### Acceptance criteria

1. THE FIRST implementation activity SHALL be a fresh read-only inspection of the
   current TIA project and Production screen.
2. THE pilot SHALL preserve the discovered list of HMI screens and a complete
   Production object inventory as baseline evidence.
3. THE pilot SHALL identify reusable common-frame candidates and all conflicts or
   duplicates before locking the master.
4. THE pilot SHALL inventory existing tags, bindings and graphics and SHALL record
   unresolved items without inventing replacements.
5. THE pilot SHALL create `production.json` from current evidence before proposing
   any corrections.
6. THE pilot SHALL report every proposed difference between TIA and the external
   model.
7. THE SYSTEM SHALL wait for explicit human approval before the first Production
   write transaction.
8. AFTER Production is validated, other screens SHALL consume its locked
   common-frame version.

## 5. Out of scope for the initial system

- Replacing the TIA Portal compiler or Unified runtime renderer.
- A general-purpose WYSIWYG clone of the entire WinCC Unified editor.
- Creation or modification of PLC tags, alarm definitions, user accounts,
  passwords, scripts, recipes, network connections or safety functions.
- Online testing or deployment to an MTP1500 panel.
- Automatic interpretation of undocumented `.ap19` internals.
- Automatic repair of ambiguous or missing bindings.
- Multi-page write batches.

## 6. Release acceptance

The initial system release is accepted only when:

1. the external folder structure, schemas and design tokens exist;
2. a fresh Production inventory can be exported read-only;
3. `production.json` renders locally at exactly 1366 × 768;
4. static validation and deterministic screenshot capture pass;
5. the diff report identifies every proposed change and unresolved binding;
6. the page-scoped importer passes tests against a disposable test project or
   controlled fixture;
7. the Production write path remains disabled pending separate explicit approval;
8. all repository changes and generated evidence are traceable.
