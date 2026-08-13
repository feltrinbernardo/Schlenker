# Design Document — External WinCC Unified HMI Development System

## 1. Design goals

The system is a local, source-controlled HMI development pipeline with two hard
boundaries:

1. browser preview and static validation never require TIA Portal; and
2. only a deliberately invoked, page-scoped C# adapter may modify an open TIA
   Portal V19 project through Openness.

The external model is authoritative for approved, managed screen properties.
TIA Portal remains authoritative for compilation, supported object semantics and
final runtime rendering. Existing TIA fields outside the managed field set are
preserved.

## 2. Architecture

```text
                          OFFLINE / READ-ONLY BY DEFAULT

  TIA Portal V19 project
           │
           │ Openness inspection
           ▼
  ┌──────────────────┐      normalize      ┌────────────────────────┐
  │ C# TIA adapter   │ ──────────────────► │ TIA inventory JSON     │
  └──────────────────┘                     └────────────┬───────────┘
                                                       │ compare
  ┌──────────────────┐    resolve/expand    ┌───────────▼───────────┐
  │ hmi-ui-src JSON  │ ───────────────────► │ Canonical screen IR   │
  └────────┬─────────┘                      └──────┬─────────┬──────┘
           │                                       │         │
           │ schema + policy                       │         │
           ▼                                       ▼         ▼
  ┌──────────────────┐                    ┌─────────────┐ ┌──────────┐
  │ Static validator │                    │ HTML/CSS/JS │ │ Diff and │
  │ + SVG validator  │                    │ preview     │ │ reports  │
  └────────┬─────────┘                    └──────┬──────┘ └──────────┘
           │                                    │
           └─────────────► approval package ◄───┘
                                  │
                            HUMAN APPROVAL
                                  │
                                  ▼
                      ┌─────────────────────────┐
                      │ Page transaction runner │
                      │ checkpoint → apply      │
                      │ → compile → reconcile   │
                      │ → conditional save      │
                      └─────────────────────────┘
```

### 2.1 Components

| Component | Responsibility | Technology |
|---|---|---|
| Source model | Human-authored screens, components, tokens and bindings | JSON + JSON Schema |
| Model CLI | Load, normalize, validate, audit and report | .NET 8 console application |
| Preview | Render canonical model at exact target pixels | Local HTML/CSS/JavaScript |
| Capture runner | Produce deterministic PNG evidence | Locally installed Chromium/Edge via Playwright when available |
| TIA adapter | Inspect, compare, checkpoint, apply, compile and reconcile | C# targeting the TIA V19 PublicAPI-compatible .NET runtime |
| Transaction runner | Enforce approval, scope, journaling and state transitions | C# CLI |
| Tests | Schema, geometry, rendering and adapter contract tests | .NET tests + browser tests |

The initial implementation SHOULD use .NET for both shared model logic and the
TIA adapter, avoiding a Node.js runtime dependency on this engineering host. The
preview itself remains plain browser JavaScript. If Playwright cannot be installed
from approved offline media, deterministic capture is a documented optional
capability until an approved local browser driver is supplied; preview and static
validation remain functional.

### 2.2 Trust boundaries

- `hmi-ui-src/` is hand-authored and reviewed.
- `generated/` is tool output and must not be edited as source.
- The preview has no TIA or device communication capability.
- The TIA adapter has no PLC/HMI online or download commands.
- Write code lives in a separate command path and assembly namespace from inspect
  commands so a read-only invocation cannot fall through into mutation.
- No third-party REST/MCP server is required. A future MCP layer may call the same
  CLI only after it demonstrates identical gates; it is not part of release 1.

## 3. Repository layout

```text
hmi-ui-src/
  README.md
  project.json
  design-system/
    colors.json
    typography.json
    spacing.json
    geometry.json
  components/
    common-header.json
    common-status.json
    right-navigation.json
    common-footer.json
    command-button.json
    status-indicator.json
    numeric-display.json
    process-device.json
  screens/
    production.json
    home.json
    operate.json
    cip.json
    manual.json
    alarms.json
    diagnostics.json
    efficiency.json
    setup.json
  assets/
    svg/
    asset-manifest.json
  bindings/
    hmi-tags.json
    screen-bindings.json
    alarms.json
  preview/
    index.html
    styles.css
    renderer.js
  validation/
    screen.schema.json
    component.schema.json
    project.schema.json
    screen-validation.schema.json
    expected-bindings.json

tools/
  hmi-ui-cli/
  tia-v19-openness/
    ExternalHmi/

generated/
  tia-openness/
    inventories/
    diffs/
    transactions/<transaction-id>/
  screen-reports/
  screenshots/
  compile-evidence/
  completion-ledger.json
  completion-ledger.tsv
```

The requested `screen-validation.schema.json` validates validation-report output;
separate source schemas are included because a report schema alone cannot validate
screen authoring input.

## 4. External data model

### 4.1 Project document

`project.json` contains no credentials and records:

```json
{
  "$schema": "./validation/project.schema.json",
  "schemaVersion": "1.0.0",
  "projectId": "schlenker-monoblock-real-juice",
  "engineeringPlatform": "TIA Portal V19",
  "target": {
    "deviceClass": "MTP1500 Unified Comfort",
    "width": 1366,
    "height": 768
  },
  "languages": ["en-US"],
  "defaultLanguage": "en-US",
  "designSystemVersion": "1.0.0",
  "productionMasterScreen": "production"
}
```

The local TIA project path is supplied by command line or a gitignored local
settings file. It is not required in portable source JSON.

### 4.2 Screen document

A screen contains references to common components plus central content objects:

```json
{
  "$schema": "../validation/screen.schema.json",
  "schemaVersion": "1.0.0",
  "screen": {
    "id": "production",
    "tiaName": "production",
    "width": 1366,
    "height": 768,
    "masterRole": "PRODUCTION_MASTER",
    "commonComponents": [
      { "ref": "common-header", "version": "1.0.0" },
      { "ref": "common-status", "version": "1.0.0" },
      { "ref": "right-navigation", "version": "1.0.0" },
      { "ref": "common-footer", "version": "1.0.0" }
    ],
    "objects": []
  }
}
```

Components use the same object model as screens and may expose typed parameters.
Expansion produces one canonical intermediate representation (IR), ensuring the
preview, validator, diff engine and importer consume identical resolved objects.

### 4.3 Screen object

The normalized object shape is:

```json
{
  "id": "prod.speed.actual",
  "tiaName": "REV25_ProductionSpeedActual",
  "type": "HmiIOField",
  "geometry": { "x": 512, "y": 220, "width": 120, "height": 48 },
  "zOrder": 30,
  "text": {
    "values": { "en-US": "N/A" },
    "fontToken": "numeric.primary",
    "horizontalAlignment": "CENTER",
    "verticalAlignment": "CENTER"
  },
  "style": {
    "foregroundToken": "text.primary",
    "backgroundToken": "surface.card",
    "borderToken": "border.standard"
  },
  "graphic": null,
  "visibility": { "kind": "CONSTANT", "value": true },
  "enabled": { "kind": "CONSTANT", "value": false },
  "binding": {
    "hmiTag": null,
    "plcSymbol": null,
    "direction": "READ",
    "classification": "MISSING",
    "evidence": []
  },
  "dynamizations": [],
  "events": [],
  "requiredAuthorization": null,
  "engineeringNotes": "No confirmed source signal; display fallback only.",
  "management": {
    "managedFields": ["geometry", "zOrder", "text", "style"],
    "preserveUnmanagedTiaFields": true
  }
}
```

`id` is the immutable external identity. `tiaName` is the native object name used
for matching. An initial adopted object stores both values in the mapping report.
Release 1 does not write hidden metadata into TIA objects unless a supported,
non-runtime-affecting property is proven available.

### 4.4 Native object type registry

The model uses a registry that maps normalized types to supported V19 Openness
types and allowed properties. Unknown types fail closed.

Initial registry candidates:

- `HmiText`
- `HmiButton`
- `HmiIOField`
- `HmiSymbolicIOField`
- `HmiRectangle`
- `HmiEllipse`
- `HmiLine`
- `HmiGraphicView`

The exact Siemens API type names and writable property set must be verified by
adapter contract tests against the installed V19 PublicAPI before enabling each
type for write. An inventory may still preserve unsupported objects as `TIA_ONLY`
or `API_NOT_EXPOSED`.

### 4.5 Binding and event model

Bindings are defined centrally and referenced by ID from screen objects. Each
record contains:

- existing HMI tag name;
- existing PLC symbolic reference;
- existing HMI connection name;
- data type and direction (`READ`, `WRITE`, `READ_WRITE`);
- display format/scaling only when confirmed;
- classification and evidence references;
- last verified inventory hash.

Event definitions are explicit ordered operations. Release 1 supports only event
shapes proven in the inventory and adapter tests. Unknown scripts or action types
are preserved and reported, never regenerated from an approximation.

## 5. Design system and common components

### 5.1 Tokens

- `colors.json`: semantic palette tokens and approved hex/alpha values.
- `typography.json`: Siemens Sans families, sizes, weights and line heights.
- `spacing.json`: approved gaps, padding and touch-target spacing.
- `geometry.json`: target bounds, common regions and standard object dimensions.

JSON source objects reference tokens rather than copying raw values where a token
exists. Literal exceptions require an engineering note and validator allow-list.

### 5.2 Production master extraction

The first Production inventory is normalized but not automatically declared
correct. Candidate common objects are grouped by position, purpose, object type,
binding and event. Duplicates and conflicts are shown to a human. Only the approved
selection becomes common component version `1.0.0`.

The common-frame lock file records:

- component versions and canonical hashes;
- Production inventory and approval IDs;
- allowed per-screen parameters, such as current-page title and selected nav item;
- fields that must remain byte-for-byte equivalent after normalization.

### 5.3 Component expansion

Expansion namespaces child IDs, for example
`common-header/user-name`, while preserving the mapped `tiaName`. Parameter
substitution is typed and restricted to declared component inputs. Components
cannot inject arbitrary events, tags or scripts through string substitution.

## 6. Preview design

### 6.1 Rendering

`preview/index.html?screen=production&mode=engineering` loads a generated,
canonical JSON bundle. The renderer creates a fixed 1366 × 768 stage with:

```css
.hmi-stage {
  position: relative;
  width: 1366px;
  height: 768px;
  overflow: hidden;
}

.hmi-object {
  position: absolute;
  box-sizing: border-box;
}
```

The preview viewport does not responsively rearrange objects. The surrounding page
may scale the stage for convenient viewing, but evidence capture uses scale 1.0.

### 6.2 Simulation

Simulation state is held only in browser memory or a generated review fixture.
Every changed live-looking field receives a visible `SIMULATED` badge, and the
stage has a persistent simulation banner. The renderer rejects simulation keys
that do not correspond to declared Boolean or numeric visual inputs.

### 6.3 Debug overlays

Engineering mode shows:

- stable ID and TIA object name;
- bounding rectangle and anchor coordinates;
- z-order;
- binding classification color;
- warnings linked to the validation report.

### 6.4 Renderer limitations

WinCC Unified browser/runtime internals, font metrics, native controls and event
semantics may not be reproduced exactly by a standalone preview. The renderer is
therefore a design-review approximation. Compile and post-import TIA evidence are
mandatory and remain authoritative.

## 7. Validation design

Validation runs in this order:

1. JSON parse and schema validation;
2. component reference and version resolution;
3. token resolution and normalized IR creation;
4. ID, name, type and z-order validation;
5. geometry bounds, clipping and overlap analysis;
6. common-frame hash and alignment checks;
7. typography, palette and standard-dimension checks;
8. asset path, structure and SHA-256 checks;
9. binding, event and authorization policy checks;
10. output of JSON and Markdown reports.

### 7.1 Geometry rules

For object `o`:

```text
0 ≤ x
0 ≤ y
width > 0
height > 0
x + width ≤ 1366
y + height ≤ 768
```

Two axis-aligned rectangles overlap when their intersection has positive width
and height. Touching edges are not overlap. Intentional parent/child layering must
match an allow-list containing both stable IDs and a reason. Objects marked hidden
are still validated because they may become visible at runtime.

Text clipping uses a conservative estimate in static validation and a browser
measurement during screenshot validation. Neither check silently resizes native
objects.

### 7.2 Severity policy

- **Error**: unsafe, ambiguous or structurally invalid; blocks approval/import.
- **Warning**: review required; may allow preview, but blocks final TIA validation
  because final acceptance requires zero warnings.
- **Info**: traceability or intentional reuse; does not block.

No command-line `--ignore-errors` option exists. Allow-list entries are reviewed
source records with scope, reason and approval reference.

## 8. TIA Openness adapter

### 8.1 Command surface

```text
hmi-ui inspect-project  --project <path> --out <directory>
hmi-ui inspect-screen   --project <path> --screen <exact-name> --out <directory>
hmi-ui validate         --screen <id>
hmi-ui preview          --screen <id>
hmi-ui capture          --screen <id> --mode review
hmi-ui diff             --screen <id> --inventory <file>
hmi-ui checkpoint       --project <path> --transaction <id>
hmi-ui apply-screen     --project <path> --screen <exact-name>
                        --approval <file> --transaction <id>
hmi-ui compile-hmi      --transaction <id>
hmi-ui reconcile        --transaction <id>
hmi-ui finalize         --transaction <id>
```

`apply-screen` is not a convenience pipeline that bypasses gates. It verifies the
checkpoint, approval, input hashes and clean precondition before it can apply.
A higher-level `run-approved-transaction` command MAY orchestrate the sequence but
must retain each independent gate and stop condition.

### 8.2 Project identity

Before access, the adapter records:

- canonical `.ap19` path supplied by the operator;
- TIA Portal version/process identity;
- project name and project GUID/API identity when exposed;
- target HMI software name and device type;
- expected 1366 × 768 screen dimensions.

A mismatch aborts. The `.ap19` file itself is never parsed. If it is locked and
cannot be hashed, the checkpoint archive hash plus Openness-exposed identity is
used and the limitation is recorded.

### 8.3 Inventory normalization

Raw Siemens API values are converted into the canonical IR without loss where
possible. Each exported field records provenance:

```json
{
  "value": 512,
  "source": "TIA_OPENNESS_V19",
  "readStatus": "CONFIRMED"
}
```

Fields absent from the API are `API_NOT_EXPOSED`; exceptions are not converted to
empty strings or defaults.

### 8.4 Matching and diffing

Matching order is deliberately conservative:

1. exact previously approved external-ID-to-TIA-name mapping;
2. exact `tiaName` and compatible native type;
3. otherwise no automatic match.

Geometry/text similarity may be reported as a candidate but cannot authorize
reuse. Diff output separates:

- visual managed-field changes;
- binding/dynamization changes;
- event/authorization changes;
- unmanaged or API-unavailable fields;
- creates, TIA-only objects and rejected conflicts.

### 8.5 Apply strategy

For each approved operation:

1. resolve the object again in the live in-memory project;
2. compare its current field values with the approved precondition inventory;
3. reject drift;
4. reuse the object when compatible;
5. set only approved managed fields whose values differ;
6. create only explicitly approved missing objects;
7. never delete a TIA-only object in release 1;
8. append the result to the transaction journal.

Bindings, events, scripts and authorizations are preservation-only in the first
safe milestone. Enabling changes to those fields is a later task that requires
dedicated adapter support, tests and separate authorization.

## 9. Transaction and recovery model

### 9.1 State machine

```text
NOT_STARTED → IN_PROGRESS → PREVIEW_APPROVED → IMPORTED
                                              │
                                              ▼
                                          COMPILED → VALIDATED
                                              │
Any active state ───────── failure ───────────┴──► BLOCKED
```

The ledger is append-only at the transition-event level. The JSON/TSV current
view is regenerated from those events.

### 9.2 Approval package

The package contains hashes of:

- normalized screen IR;
- every referenced component and design-system file;
- every referenced asset;
- binding catalogue subset;
- validation reports;
- review screenshot;
- TIA precondition inventory and proposed diff.

It also contains the approver identity, timestamp and scope. Any hash change
invalidates approval.

### 9.3 Write protocol

1. Confirm exact project/HMI/screen identity and clean supported state.
2. Validate source with zero errors and resolve the approval package.
3. Create the fresh `.zap19` checkpoint through supported TIA functionality.
4. Hash and record the checkpoint before changing an object.
5. Re-inspect the named page and reject precondition drift.
6. Apply the approved page plan in memory, journaling every operation.
7. Compile the HMI.
8. If any error or warning occurs, mark the transaction blocked and close/recover
   without saving when supported; do not continue to another page.
9. Re-export and reconcile the page.
10. Save only after zero errors, zero warnings and successful reconciliation.
11. Write final evidence and ledger transition.

TIA Openness may not offer a database transaction with atomic rollback. Therefore
the adapter must never promise one. The fresh archive is the recovery boundary.
The first end-to-end write tests must use a disposable project copy. Recovery of
the authoritative project from a checkpoint is a separate, explicit operator
action and is never automated over an open project.

## 10. Reports and evidence

Each transaction directory contains:

```text
transaction.json
approval.json
checkpoint.json
pre-inventory.json
proposed-diff.json
apply-plan.json
transaction-journal.jsonl
compile.log
compile-result.json
post-inventory.json
reconciliation.json
object-mapping.tsv
unresolved-bindings.tsv
result.md
```

Preview evidence is stored by source hash under `generated/screenshots/`.
Read-only reports and write transaction evidence use separate directories so a
preview run cannot appear to be an imported result.

## 11. Error handling

- Schema or policy errors stop before TIA is opened or attached.
- An ambiguous project, HMI, screen, object, tag or asset fails closed.
- TIA API exceptions are preserved with context and sanitized stack information.
- A compile warning is a blocking result, not an informational success.
- A journaled partial apply becomes `BLOCKED`; the tool does not retry blindly.
- Each page is isolated. No failure handler queues or rebuilds another page.

## 12. Correctness properties

The following properties complement example-based tests.

### Property P1 — Canonical model determinism

For any valid source tree, normalizing it twice with the same tool version produces
byte-identical canonical JSON and hash.

Validates Requirements 2, 4, 6 and 12.

### Property P2 — Geometry safety

For every object accepted by validation, its rectangle is fully inside the target
bounds and has positive dimensions.

Validates Requirements 2 and 7.

### Property P3 — Duplicate identity rejection

For any screen where two expanded objects share an ID or TIA name, validation
fails independently of their order.

Validates Requirements 2, 7 and 9.

### Property P4 — Component equivalence

For every standard screen and locked common-component version, expansion produces
the same protected common-frame fields as the approved master, except declared
parameters.

Validates Requirement 4.

### Property P5 — Asset integrity

Changing any byte of a referenced SVG without updating an approved manifest causes
validation and approval verification to fail.

Validates Requirements 5 and 11.

### Property P6 — Approval invalidation

Changing any approved source, component, token, binding or asset hash prevents the
associated approval package from authorizing a transaction.

Validates Requirements 9, 11 and 12.

### Property P7 — Managed-field confinement

For any approved update plan, the set of fields sent to the adapter is a subset of
the object's approved `managedFields` diff.

Validates Requirements 3, 8 and 9.

### Property P8 — Idempotent reconciliation

Applying a plan to a disposable project, reconciling, then generating a second
plan from unchanged sources yields no `CREATE` or `UPDATE` operations.

Validates Requirements 8, 9 and 10.

### Property P9 — Page isolation

For a transaction targeting screen S, pre/post inventories of all accessible
non-S screens remain identical for managed inventory fields.

Validates Requirement 9.

### Property P10 — Ledger transition safety

For every event sequence, the ledger reducer accepts only defined transitions and
never reaches `IMPORTED`, `COMPILED` or `VALIDATED` without their prerequisite
evidence hashes.

Validates Requirement 11.

## 13. Testing strategy

### 13.1 Tests without TIA Portal

- schema fixtures for valid and invalid model variants;
- component expansion and token resolution;
- property-based geometry, duplication and approval-hash tests;
- SVG parsing and hash validation;
- overlap allow-list tests;
- golden-file renderer tests and deterministic screenshots;
- inventory/diff tests using captured, sanitized fixtures;
- transaction state-machine and policy tests.

### 13.2 Tests with TIA Portal V19

Run only offline on the approved engineering host:

- PublicAPI compatibility and property-registry discovery;
- read-only inspection against a disposable project copy;
- object create/update/reuse tests on a disposable HMI screen;
- deliberate duplicate, drift and compile-failure tests;
- page-isolation comparison;
- checkpoint creation and documented recovery rehearsal;
- one explicitly approved Production pilot transaction only after all prior gates.

No test connects to or downloads to hardware.

## 14. Key risks and mitigations

| Risk | Mitigation |
|---|---|
| V19 Openness does not expose every Unified property/event | Capability registry; `API_NOT_EXPOSED`; preserve rather than guess |
| Browser font/control rendering differs from Unified runtime | Siemens Sans local font checks; conservative preview; TIA remains authoritative |
| Compile changes project state before save | Disposable tests, checkpoint first, compile in memory where supported, documented close-without-save recovery |
| Existing object names are inconsistent | Explicit adoption mapping; exact matching only; no heuristic writes |
| Common frame contains existing duplicates/conflicts | Inventory and human selection before lock; no automatic correction |
| A tag appears plausible but is not semantically correct | Evidence-backed classification and human resolution; no invented binding |
| TIA project is already open or dirty | Verify process/project identity and precondition inventory; refuse unsafe state |
| Screenshot automation dependency is unavailable offline | Plain local preview remains available; install capture tooling only from approved offline media |
| Third-party licensing or service outage | No paid connector dependency; own local C# Openness adapter |

## 15. Initial implementation decision

The recommended first implementation transaction is intentionally read-only:

1. build the schemas, canonical model and validators;
2. adapt the existing V19 inspection code into the new normalized inventory format;
3. export the current Production screen;
4. generate `production.json`, unresolved-binding and common-frame candidate reports;
5. render and capture the Production preview;
6. produce a zero-write diff and approval package.

The first TIA write transaction is a separate milestone. It should update one
low-risk visual-only object on a disposable project copy, prove checkpoint,
compile, reconciliation and page isolation, then stop for review. Production is
not the first write target.
