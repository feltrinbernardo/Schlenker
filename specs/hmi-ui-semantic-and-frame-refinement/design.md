# Design — HMI Semantic and Global-Frame Refinement

Date: 2026-08-13  
Status: draft for review  
Target: TIA Portal V19 / WinCC Unified / MTP1500 Unified Comfort

## 1. Design intent

The design preserves the existing Schlenker visual language and process
behavior while making the HMI more consistent and operationally unambiguous.
The established non-Production frame is the proposed geometry baseline;
Production supplies the approved central visual composition and confirmed
process bindings.

The design is implemented as a conservative property-level refinement, not a
screen rebuild and not a behavioral redesign.

## 2. Source hierarchy

The implementation shall use the following precedence:

1. Safety and transaction rules in the repository `AGENTS.md` files.
2. This specification's transaction and semantic-safety requirements.
3. Existing `specs/production/` process, binding, behavior, access, and visual
   requirements.
4. The established canonical frame proven by the latest read-only audit.
5. Approved Production and Home reference captures.

If sources conflict outside the explicitly resolved frame and alarm-colour
decisions, implementation stops for review.

## 3. Transaction architecture

```text
read-only inventory
        |
        v
canonical source + exact baseline
        |
        v
validation -> diff plan -> hash-bound human approval
        |                       |
        +-----------------------+
                    |
                    v
fresh .zap19 checkpoint
                    |
                    v
one-screen Production apply
                    |
                    v
TIA V19 HMI compile (0 errors / 0 warnings)
                    |
                    v
post-inventory + reconciliation + page isolation
                    |
              pass / abort
                    |
                    v
                  save
```

The apply journal is append-only. Save is the final operation and is permitted
only after all checks pass.

## 4. Canonical screen regions

The exact coordinates shall come from the latest validated canonical-frame
inventory and be bound into the approval hashes. Conceptually, the screen is
divided into:

| Region | Purpose | Rule |
|---|---|---|
| Header | Machine title and page identity | Common geometry on every main page |
| Status row | Machine state, readiness, alarm, time, user | No process commands |
| Central content | Page-specific process UI | Preserved per-screen behavior |
| KPI column | Production summary | No invented values |
| Right navigation | Primary destinations | Fixed order and 133 x 43 buttons |
| Footer | Approved global status/shortcuts | No overflow primary navigation |

The design does not hard-code new coordinates in this document because the
transaction must bind to a fresh authoritative inventory. The currently
expected navigation coordinates in the REV25 evidence are planning inputs,
not an approval substitute.

## 5. Global-frame component model

### 5.1 Header

- Dark navy background.
- Siemens Sans or the verified Unified equivalent.
- Left-aligned machine title.
- Right-aligned page title and site identifier.
- Identical object names and geometry where the current project already uses a
  common naming convention.

### 5.2 Status row

Recommended left-to-right composition:

1. Machine status label.
2. Read-only symbolic machine-state field.
3. System-ready lamp and label.
4. Alarm icon/summary.
5. Date/time.
6. User/profile.

The profile graphic remains display-only unless a separately approved,
verified Unified login/session action is supplied. No credentials or custom
security behavior are introduced by this spec.

### 5.3 Navigation rail

- One background panel.
- One 133 x 43 native button per approved destination.
- One non-interactive, transparent SVG icon per button where already proven.
- Text remains visible; icons supplement rather than replace labels.
- The current-page button is selected and disabled only if that matches
  existing behavior.
- Existing `Tapped` scripts and destination names are copied byte-for-byte from
  the baseline unless separately approved.

### 5.4 Footer

The footer is a separate common component. Any bottom shortcut retained by the
approved design must have a documented reason and must not duplicate or
displace a primary rail destination without approval.

## 6. Semantic state model

| State | Colour | Supporting cue | Permitted source |
|---|---|---|---|
| Healthy/active feedback | Green | State text or active symbol | Confirmed feedback |
| Inactive/disabled | Grey | OFF/inactive/disabled text where needed | Confirmed value |
| Warning | Amber | Warning icon/text | Confirmed warning |
| Alarm/fault/trip | Red | Alarm/fault icon or text | Confirmed alarm/fault |
| Bad quality/disconnected | Distinct neutral/diagnostic treatment | `NO DATA` or diagnostic symbol | Proven quality mechanism |
| Missing/not configured | Neutral grey | `N/A` or `NOT CONFIGURED` | Evidence-backed absence |

For Boolean alarm tags, true maps to the alarm/fault state. The existing green
true-state scripts on `REV12_Exact_Status_Lamp_P6` and
`REV12_Exact_Status_Lamp_S5` are visual defects to be corrected without
changing their tag references.

## 7. Production component design

### 7.1 Process mimic

- Preserve M102, M103, TLS100, EV210, EV212, EV217, EV213, and EV247 identity.
- Preserve the confirmed product/vacuum topology.
- Standardize pipe stroke and arrow treatment using native lines or validated
  assets.
- Keep equipment graphics non-commanding.
- If a touch interaction already exists, preserve its diagnostic-only behavior
  exactly.
- Place engineering codes close to their equipment without covering graphics
  or flow paths.

### 7.2 Status lists

Each row consists of:

- A fixed-width semantic label.
- A fixed-size state container.
- A non-interactive state lamp or semantic value.

Rows align to a common vertical grid. Command-only signals are labelled as
commands. Feedback wording is reserved for confirmed feedback.

### 7.3 Speed gauge

The preferred V19-compatible implementation uses native shapes:

- Static circular or semicircular track.
- Dynamic arc/segments or a tested state graphic driven by
  `Speed_Actual_Pct`.
- Central percentage text bound to the confirmed actual-percent source.
- Separate BPH setpoint field bound to `Speed_Setpoint_BPH`.
- `N/A` text when the actual value is invalid under a proven validity rule.

If dynamic arcs are not safely writable through the proven V19 API, use a
small finite set of validated state graphics or retain a linear gauge and
record a design deviation. Do not fabricate a BPH conversion.

### 7.4 Efficiency ring

The displayed value and ring share one formula:

```text
if TotalSeconds > 0:
    EfficiencyPct = clamp(RunSeconds / TotalSeconds * 100, 0, 100)
else:
    N/A
```

Both source tags must remain confirmed. A single reviewed script or equivalent
native dynamization shall avoid disagreement between ring and text.

### 7.5 Produced Today, product pressure, and M103

These components use an explicit unavailable pattern until their sources are
confirmed:

- Neutral card or label.
- `N/A` or `NOT CONFIGURED` text.
- No green lamp, live-looking number, or animated state.
- Engineering note and evidence classification retained outside the runtime
  presentation.

## 8. Cross-screen patterns

### Home

Use the same global frame. Keep Machine Overview and approved auxiliary
commands. Reserved future actuator areas should be visually subordinate and
may be removed only through an approved Home-specific diff.

### Alarms

Treat the existing `HmiAlarmControl` as an opaque preserved native control.
Arrange reset/acknowledgement controls outside its bounding box and preserve
their existing authorization and events.

### Settings and diagnostics

Use consistent table rows with engineering code, function, location, and
value/state. Read-only fields receive a distinct non-editable visual treatment.
No diagnostic component provides forcing or manual output control.

### Manual, CIP, and Recipe

Use the common frame without changing their command or security architecture.
Later screen-specific specs may refine sequence progress and grouping after the
pilot approval.

## 9. V19 implementation constraints

- Prefer native Unified objects already present in the project.
- Mutate only properties proven by the local V19 capability evidence.
- Use integer geometry.
- Use self-contained, hash-controlled SVG assets.
- Do not depend on CSS, HTML layout, external resources, or browser rendering.
- Do not infer native z-order when it is not exposed reliably.
- Avoid deleting TIA-only objects automatically.
- Preserve unmanaged properties and unknown metadata.
- Treat `HmiAlarmControl` internals as read-only unless separately proven and
  approved.

## 10. Validation design

### Static checks

- Schema and identity.
- Screen bounds.
- Interactive hit-area overlap.
- Foreground text/value overlap.
- Text clipping.
- Asset integrity and hashes.
- Binding existence and classification.
- Event and authorization preservation.
- Semantic rule: active alarm shall not map to green.
- Missing-data rule: unavailable signals shall not show live-looking values.

### TIA checks

- Exact project/HMI/screen identity.
- Pre/post object inventory.
- HMI compile: zero errors and zero warnings.
- Managed-property reconciliation.
- Unmanaged-property preservation.
- Non-target screen signature equality.
- Final 1366 x 768 capture and visual comparison.

## 11. Rollback and failure handling

The fresh `.zap19` checkpoint is the recovery boundary. A failed transaction
shall not save. Evidence shall record the failure, unchanged saved-project
state, and the exact stage that stopped. Restoration or reopening from the
checkpoint is a separate human-controlled action and is not automatic.

## 12. Open design decisions

The following decisions must be closed before the corresponding change enters
the Production diff:

1. Exact canonical frame hash and coordinate set.
2. Exact alarm false-state appearance.
3. Proven V19 technique for the radial speed gauge.
4. Proven V19 technique for the dynamic efficiency ring.
5. Whether data-quality metadata is accessible for each target object/tag.
6. Whether the user/profile control remains display-only or receives a
   separately authorized verified session action.
7. Exact Production object list and managed properties in the first write.

