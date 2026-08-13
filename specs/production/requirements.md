# Requirements — Native HMI Production Page Refinement

## 1. Purpose

This specification defines the requirements for refining the existing REV13 Siemens WinCC Unified HMI using **native HMI objects, SVG graphics, dynamic properties, animations, and PLC tag bindings**.

The goal is to reproduce the **approved Production page visual reference** faithfully inside TIA Portal / WinCC Unified without changing working PLC logic, safety interlocks, alarms, recipes, navigation logic, or existing tag semantics.

The approved visual reference is binding. The Automation Agent must execute the design, not reinterpret it.

---

## 2. Scope

This spec covers:

- Production page
- Global top header
- Global right-side navigation
- Home-page KPI conventions that are reused on Production
- Native WinCC Unified visual objects
- SVG / graphic assets
- Dynamic visual properties
- Animations
- PLC/HMI tag bindings
- User interaction and navigation
- Diagnostics/detail interaction
- Access control
- Validation and regression testing

This spec does **not** authorize changes to:
- PLC process sequences
- Safety logic
- Alarm philosophy
- Existing interlocks
- Existing device addresses
- Existing machine-state definitions

### R-000 — Specification Authority and Controlled Inputs

The following inputs jointly define the Production implementation:

1. The approved Production reference image controls visual composition.
2. This Production specification controls behavior, bindings, validation and acceptance.
3. `C:\www\Schlenker\.modification_logic\REV14_HMI_Asset_Library_and_Binding_Spec.docx` controls the approved asset names and intended asset usage.
4. `C:\www\Schlenker\.modification_logic\svg` is the controlled source directory for SVG assets.
5. `C:\www\Schlenker\.modification_logic\REV14_1_Preparation_Resolution_and_Implementation_Gate.docx` controls preparation, readiness classification, implementation gating, permitted stop conditions and required evidence. Where it is more restrictive than an earlier REV14 instruction, REV14.1 takes precedence.

Where these sources disagree, the Automation Agent shall stop and record the conflict. It shall not silently reinterpret the requirement.

The first implementation release is limited to the Home and Production pages. Global propagation to other pages requires visual approval of both pilot pages.

### R-000A — Preparation Execution Directive

Before building either pilot page, the Automation Agent shall complete every preparation task that can be proven from the existing TIA Portal V19 PLC/HMI project. It shall search project tags, cross-references, DBs, HMI tag tables, scripts, alarms, faceplates, screen events and existing graphics without requesting information that is already discoverable in the project.

The agent shall not invent tags, values, device identities, scaling, quality semantics, reset behavior or control logic. A fact that cannot be proven shall be classified as `MISSING` or `AMBIGUOUS` with supporting evidence.

---

## 3. Global Visual Requirements

### R-001 — Global Header
The current approved top header shall be preserved across all main pages.

The header shall retain:
- Machine/project title
- Current page title
- System-ready status
- Login/user area
- Date/time area
- Existing status icons

No process control commands shall be placed in the global header.

### R-002 — Global Navigation
All main HMI pages shall use the same fixed vertical navigation column on the **right side**.

Minimum destinations:
- Home
- Safety
- Production / Operation
- Manual
- CIP
- Run Out
- Settings
- Diagnostics
- Alarms

Each navigation button shall include:
- Approved icon
- Text label
- Selected-page state
- Pressed/hover feedback where supported

The navigation order must be identical on every main page.

Existing valid destinations such as Operate, Function, Recipe and Efficiency shall not be deleted merely because they are absent from the REV14 minimum list. The final navigation inventory and order shall be approved before propagation.

### R-003 — Visual Language
The HMI shall preserve the approved:
- Blue / white palette
- Dark navy navigation/header
- Rounded cards
- Clean sans-serif typography
- Modern industrial dashboard style
- Green = healthy/active
- Grey = inactive
- Amber/orange = warning/attention
- Red = alarm/trip

Normal active digital signals shall **not blink**.

---

## 4. Production Page Requirements

### R-010 — Approved Master Layout
The Production page shall reproduce the approved final reference layout.

The page shall contain:
- Global top header
- Main product/filler/vacuum mimic
- Production commands
- Production KPIs
- Speed gauge
- Efficiency gauge
- Produced-today KPI with bottle icon
- Process status
- Mode/sequence status
- Right-side navigation
- Bottom system/status bar

A rough P&ID or plain engineering-debug screen is not acceptable.

### R-011 — Process Mimic
The central mimic shall graphically represent the active production process.

Minimum live equipment:
- M102 Product Pump
- M103 Vacuum Pump
- Product pressure
- Vacuum pressure
- Product valves
- Vacuum valve
- TLS100 Tank Level Sensor
- Product-presence sensor where available
- PLC/HMI connection status
- Product flow path
- Vacuum flow path

Each item must use the real existing PLC/HMI tag when available.

### R-012 — No Fabricated Live Values
The HMI shall not invent process values.

If a real PLC tag is unavailable:
- Show `N/A`, `Not Configured`, or equivalent
- Do not show simulated production values in production runtime

### R-013 — Device Identification
Displayed equipment shall show the approved engineering code.

Examples:
- `M102` — Product Pump
- `M103` — Vacuum Pump
- `TLS100` — Tank Level Sensor
- `E100` — Encoder
- `SPC100` — Sensor Presence Cylinder

The same code shall be consistent across:
- Cable label
- Electrical drawings
- I/O list
- PLC mapping
- HMI
- Maintenance documentation

### R-014 — Speed KPI
Machine speed shall use a **circular/radial speedometer**.

The gauge shall show:
- Actual speed
- Unit
- Setpoint/target where available
- Dynamic arc/needle bound to the real speed tag

A plain numeric card alone is not acceptable.

### R-015 — Efficiency KPI
Machine efficiency shall use a **circular/ring gauge** matching the approved Efficiency-page visual style.

It shall show:
- Efficiency %
- Dynamic ring/arc
- Real calculated value only

### R-016 — Production Quantity KPI
The Production page shall show:
- Bottles produced today
- Large live number
- Bottle icon
- Real production counter tag

### R-017 — Vacuum Engineering Unit
Vacuum shall be displayed in **mbar**.

The implementation shall verify actual PLC scaling before formatting.

Example:
- PLC source `-0.85 bar` must be converted to `-850 mbar`
- Do not relabel a bar value as mbar without conversion

For the current REV12/REV13 interface, `Vacuum_Actual_mbar` is already defined in mbar. No ×1000 HMI conversion shall be applied unless a later binding audit proves that the source definition changed.

### R-018 — Binding Readiness Gate

Every Production value or state shall have exactly one documented REV14.1 readiness status before page construction:

- **CONFIRMED:** signal identity, source, scaling and meaning are proven in the project. It may be bound.
- **MISSING:** the required signal does not exist or cannot be found. Record the blocker and do not bind or fabricate it.
- **AMBIGUOUS:** more than one plausible signal or device remains. Record the candidates and evidence; do not guess or bind it.
- **NOT REQUIRED:** the requested visual state is unsupported or unnecessary. Record the reason and do not bind it.

`Bound`, `Derived` and `Not Configured` may be recorded as implementation dispositions, but they do not replace the mandatory readiness status. A derived value is permitted only when all source signals and the formula/reset basis are `CONFIRMED`.

Current known binding decisions:

| Requirement | Confirmed source | Readiness / disposition |
|---|---|---|
| Actual drive speed | `Speed_Feedback_Pct` | CONFIRMED / Bound; preferred actual-speed source |
| Speed reference/output | `Speed_Actual_Pct` | CONFIRMED / Bound; shall not be presented as measured feedback |
| Production speed setpoint | `Speed_Setpoint_BPH` | CONFIRMED / Bound, but its BPH unit shall not share a percent gauge scale without an approved conversion |
| Vacuum actual | `Vacuum_Actual_mbar` | CONFIRMED subject to final project metadata check / bind directly in mbar if verified |
| Tank level | `Tank_Level_Pct` | CONFIRMED / Bound |
| Efficiency | `Efficiency_Run_Seconds / Efficiency_Total_Seconds × 100` | CONFIRMED sources / Derived; valid only when total seconds > 0 |
| Bottles inside machine | `Bottle_Count_In_Machine` | CONFIRMED diagnostic count; prohibited as a produced-today counter |
| Produced today | No confirmed tag | MISSING until the project search proves a daily/resettable counter |
| Product pressure | No confirmed tag | MISSING until the project search proves source, meaning, unit and scaling |
| M103 fault/ready/speed/communication | No complete confirmed interface | MISSING individually unless project evidence proves each state |

`MISSING` and `AMBIGUOUS` items shall remain in the open-point register and cannot be declared accepted or silently simulated.

### R-019 — Engineering-Code Reconciliation

The Production specification refers to `SPC100`, while existing project material has also used `SPL100` for product presence. The final device code shall be confirmed against the electrical drawing and I/O list before it is placed on the HMI. No automatic rename is permitted.

The project search shall include PLC/HMI tags, comments, cross-references, existing screen labels and any available electrical/I/O references. If `SPC100` is proven, use it. If only `SPL100` is proven, retain the PLC identity and record the naming discrepancy for review. If both exist and their functions cannot be distinguished, classify the identity as `AMBIGUOUS`.

---

## 5. Native WinCC Unified Object Requirements

### R-020 — Native Objects First
Use native WinCC Unified objects wherever they provide the required function reliably.

Recommended native object categories:
- Text
- I/O fields
- Buttons
- Graphic I/O fields
- Ellipses / arcs
- Rectangles / rounded cards
- Lines / polylines
- Trend controls
- Alarm controls
- User administration controls
- Screen windows / popups
- Faceplates where reusable

### R-021 — SVG Usage
Use SVG for:
- Pump symbols
- Valve symbols
- Sensor symbols
- Bottle icon
- Home icon
- Safety icon
- Navigation icons
- Process graphics that require higher visual fidelity

SVG requirements:
- Vector, scalable
- Transparent background
- Minimal complexity
- Consistent stroke width
- Consistent visual style
- No embedded scripts
- No external web dependencies

Before import, every SVG shall pass an asset gate:

- Well-formed XML with exactly one value for each attribute.
- Valid `viewBox` and transparent background.
- No scripts, event handlers, external links, embedded web resources or unsupported fonts.
- No duplicate IDs.
- Controlled stroke width and palette.
- Tested at the intended HMI size without clipping.
- Asset filename and SHA-256 recorded in the asset manifest.

`nav_cip.svg` currently fails the well-formed XML requirement because one path contains a duplicate `fill` attribute. It shall be corrected and revalidated before import.

SVG color alone shall not be assumed dynamically recolorable in WinCC Unified. Dynamic-state implementation shall use a tested Graphic I/O list, state-specific graphics, native overlays or another verified Unified mechanism.

### R-022 — Reusable Faceplates
Reusable process equipment should be implemented as faceplates/components when practical.

Recommended:
- Pump faceplate
- Valve faceplate
- Digital sensor faceplate
- Analog sensor faceplate
- Navigation button faceplate
- KPI gauge faceplate
- Device-detail popup faceplate

Each faceplate shall expose clearly defined parameters/tags.

No faceplate shall expose a command parameter on the read-only Production mimic. Commands remain in the existing PLC/HMI command architecture and authorized command areas.

---

## 6. Dynamic Property Requirements

### R-030 — Digital Status
For digital inputs and outputs:
- FALSE = grey
- TRUE = solid green
- Alarm/fault = red where applicable
- Warning = amber where applicable
- Bad quality/disconnected = distinct diagnostic state

### R-031 — Pump Animation
Pump visual shall support:
- Stopped
- Running
- Fault
- Manual mode
- Automatic mode
- Communication bad/unavailable

Recommended visual behavior:
- Running = green active state
- Fault = red
- Stopped = grey
- Optional subtle rotation/flow animation only if performance remains acceptable

### R-032 — Valve Animation
Valve visual shall support:
- Closed
- Open
- Transitioning if feedback exists
- Fault/conflict if feedback is invalid
- Manual/Auto state where required

### R-033 — Flow Animation
Product and vacuum paths may use animated flow.

Only animate when:
- The corresponding process condition is truly active
- The source tags are valid
- Animation does not reduce runtime readability/performance

### R-034 — Analog Values
Analog values shall show:
- Numeric value
- Engineering unit
- Quality state
- Optional bar/ring visual
- High/low warning state where applicable

Tag quality shall be evaluated independently from the numeric value. A numeric zero with bad quality must not appear as a valid zero.

---

## 7. Interaction Requirements

### R-040 — Device Touch Behavior
Touching a device on the Production mimic shall:
- Open its diagnostic/detail popup or navigate to the relevant detail/manual page
- Preselect the touched device where possible

Touching the mimic shall **not directly energize an actuator**.

### R-041 — Manual Control
Manual actuator control shall remain on authorized detail/manual screens.

Manual control must:
- Require the correct user level
- Respect PLC interlocks
- Respect Safety
- Respect machine mode
- Never bypass critical permissives

### R-042 — Back Navigation
Detail pages/popups may use a Back control.

Main Home access remains available through the right-side global navigation.

---

## 8. Access Control Requirements

### R-050 — Operator
No password required for routine:
- Production
- CIP
- Normal start/stop/reset
- Routine operator functions

### R-051 — Maintenance
Password required for:
- Bypasses
- Setup
- Manual actuator controls
- Advanced diagnostics actions
- I/O actions
- Maintenance settings

### R-052 — Engineer
Password required for:
- Engineering parameters
- Advanced configuration
- Protected service functions

### R-053 — Session Timeout
The HMI shall support automatic return to Operator level after inactivity.

The exact TIA user groups, authorizations and timeout shall be recorded in an access matrix before security-dependent objects are implemented. Generic role names in this specification are requirements, not proof that matching project groups already exist.

The approved access matrix is:

| Role | Routine access | Protected access | Governing rule |
|---|---|---|---|
| Operator | Production, CIP, normal daily operation, and normal reset/start/stop already permitted by the project | No maintenance bypass, setup engineering or service actions | Routine operator work requires no password |
| Maintenance | Diagnostics, setup, bypasses, manual actuator functions and I/O/service actions | Available only after Maintenance login | PLC and Safety interlocks remain effective |
| Engineer | Advanced configuration and engineering/service parameters | Available only after Engineer login | HMI privilege never bypasses Safety |

Inactivity shall return the session to the Operator/default access level using supported WinCC Unified user administration.

---

## 9. Non-Destructive Integration Requirements

### R-060
Before modifying the project, the Automation Agent shall inventory:
- Screens
- Screen IDs
- HMI objects
- Tags
- Scripts
- Faceplates
- Alarm classes
- Navigation events
- Animations
- PLC bindings

### R-061
Do not:
- Bulk-replace HMI XML
- Create duplicate IDs
- Rename working PLC tags for visual convenience
- Delete working HMI functions
- Change PLC state logic as part of UI refinement

### R-062
Create a backup/checkpoint before implementation.

### R-063 — Pilot and Approval Gate

Implementation shall proceed in this order:

1. Complete the binding and asset inventories.
2. Correct and validate the controlled asset library.
3. Build reusable components using test bindings.
4. Apply the global frame and graphics to Home and Production only.
5. Compile and capture both pages at the native MTP1500 resolution.
6. Perform an overlap, clipping, alignment, navigation and tag-binding audit.
7. Obtain visual approval.
8. Only then propagate the global frame to other pages.

No mass page update is permitted before Step 7.

The preparation gate is passed only when all of the following exist and have been reviewed:

- Validated SVG asset library, including a corrected `nav_cip.svg`.
- Asset manifest containing filename, semantic function, version, SHA-256, validation result and intended HMI use.
- Complete PLC/HMI binding matrix.
- Open-item register containing only `MISSING` and `AMBIGUOUS` items with evidence.
- Completed access matrix and supported inactivity-timeout design.

After the gate passes, build only:

- **Home:** approved global header, right navigation, machine graphic and Produced Today KPI with bottle icon when `CONFIRMED`; otherwise display `N/A` or `Not Configured`.
- **Production:** approved Product/Filler/Vacuum mimic, M102, M103, valves, sensors, circular speedometer, efficiency ring, proven vacuum-mbar value and only confirmed KPIs.

Touching equipment may open diagnostic/detail context but shall never directly energize hardware.

### R-064 — Permitted Stop Conditions

The agent may stop and request human input only when at least one genuine blocker remains after project inspection:

1. Two or more signals remain indistinguishable after cross-reference inspection.
2. A required signal does not exist and resolving it requires PLC logic changes.
3. A device designation conflicts with unavailable external electrical/I/O documentation.
4. The installed WinCC Unified version cannot implement an approved visual requirement without changing the agreed design.

All other preparation work shall continue autonomously.

---

## 10. Acceptance Requirements

### R-070
HMI compile: 0 errors.

### R-071
PLC compile: 0 errors.

### R-072
0 unresolved symbolic references.

### R-073
No broken navigation links.

### R-074
No clipped or overlapping HMI objects.

### R-075
Production page visually matches the approved reference.

### R-076
Speed gauge is circular/radial.

### R-077
Efficiency gauge is circular/ring style.

### R-078
Vacuum is correctly displayed in mbar.

### R-079
Produced-today KPI includes the bottle icon and a live count only when a genuine daily/resettable counter is `CONFIRMED`; otherwise the pilot displays `N/A` or `Not Configured` and acceptance remains partial.

### R-080
All device states are bound to real tags or clearly show unavailable status.

### R-081
Every controlled SVG passes the asset gate and matches the recorded asset manifest.

### R-082
Every displayed live value appears in the tag-binding matrix with source, type, unit, access, quality handling and fallback state.

### R-083
Home and Production receive visual approval before any global-frame propagation.

### R-084
The Home and Production pilot pages shall be inspected at 100% zoom and shall contain no overlap, clipping, off-screen controls or inconsistent navigation sizing.

### R-085
Valid TRUE/running states shall be solid green and shall not blink. Warning, fault and bad-quality states shall remain visually distinct from valid OFF or numeric zero.

### R-086
Preparation evidence shall include the validated asset library, hashed asset manifest, complete binding matrix, evidence-based open-item register, Home and Production pilot screenshots, HMI and PLC compile evidence, before/after comparison and a recorded decision that global propagation remains blocked pending pilot approval.
