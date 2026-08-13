# Design — Native WinCC Unified Production HMI

## 1. Architecture

The HMI shall use a layered architecture:

1. **Global Frame**
   - Header
   - Right navigation
   - Bottom status bar

2. **Page Content**
   - Production mimic
   - Commands
   - KPIs
   - Status cards

3. **Reusable Components**
   - Pump faceplate
   - Valve faceplate
   - Sensor faceplate
   - KPI gauge faceplate
   - Navigation button component
   - Device popup

4. **PLC Binding Layer**
   - Existing HMI tags
   - Existing PLC tags
   - New HMI-only display/conversion tags only when strictly required

5. **Controlled Asset Layer**
   - Source: `C:\www\Schlenker\.modification_logic\svg`
   - Validated SVG manifest
   - Imported WinCC Unified project graphics
   - State-specific graphic lists or tested native overlays

The asset layer is presentation-only. It shall never become an alternate command path.

### 1.1 REV14.1 Preparation Gate

The implementation pipeline has two explicit states:

1. **Preparation:** inspect the existing TIA V19 project, validate assets, classify bindings, complete the access matrix and generate evidence.
2. **Pilot implementation:** build Home and Production only after the preparation deliverables pass review.

No visual object may bind to a `MISSING`, `AMBIGUOUS` or `NOT REQUIRED` item. Missing/ambiguous values use `N/A` or `Not Configured` and remain in the open-item register. No PLC logic is added during this visual-refinement phase without separate approval.

---

## 2. Screen Grid

Use a relative layout model.

### Global Regions
- Header: `X 0–100%, Y 0–9%`
- Main content: `X 0–81%, Y 9–94%`
- Right navigation: `X 81–100%, Y 9–94%`
- Bottom status: `X 0–100%, Y 94–100%`

### Production Regions
- Status/command zone: `X 2–20%, Y 11–83%`
- Main mimic: `X 22–79%, Y 11–59%`
- Lower process cards: `X 22–79%, Y 61–83%`
- Right KPI zone: `X 81–98%, Y 11–49%`
- Right navigation: `X 81–98%, Y 51–93%`

These values are the implementation grid. Final visual approval still depends on matching the approved reference image.

### Layout Constraints

- Target canvas: `1366 × 768` for MTP1500 Unified Comfort.
- Objects shall remain inside their assigned region with a minimum 8 px internal gutter and 10 px inter-card gutter unless the approved reference requires more.
- Labels and values shall use independent bounding boxes; no label may extend underneath another value or control.
- Navigation, header and bottom bar are fixed regions and shall not be covered by page-specific content.
- Nested objects intentionally forming one component shall be grouped or documented so the overlap verifier can distinguish intentional containment from a defect.
- No object may be clipped at 100% zoom.

---

## 3. Global Header Design

### Native Objects
- Rectangle background
- Text objects
- Status icons / SVG
- User/login display
- Date/time display
- System-ready indicator

### Dynamic Properties
- System-ready indicator:
  - Green if ready
  - Amber if warning
  - Red if critical alarm
- User name bound to active login/session
- Page title dynamically set per page where appropriate

---

## 4. Right Navigation Design

Use one reusable navigation-button component.

### Button Parameters
- `Label`
- `IconSvg`
- `TargetScreen`
- `IsCurrentPage`
- `UserLevelRequired`
- `Enabled`

### Visual States
- Normal
- Hover
- Pressed
- Selected
- Disabled

### Required Icons
- Home → house
- Safety → shield/safety
- Production → production/machine
- Manual → hand/manual
- CIP → cleaning
- Run Out → run-out/emptying
- Settings → gear
- Diagnostics → diagnostic/tool
- Alarms → warning/alarm

### Controlled Navigation Assets

| Destination | Asset |
|---|---|
| Home | `nav_home.svg` |
| Safety | `nav_safety.svg` |
| Production | `nav_production.svg` |
| Manual | `nav_manual.svg` |
| CIP | `nav_cip.svg` after XML correction |
| Run Out | `nav_runout.svg` |
| Settings | `nav_settings.svg` |
| Diagnostics | `nav_diagnostics.svg` |
| Alarms | `nav_alarms.svg` |

Operate, Function, Recipe and Efficiency require either approved icons matching this library or an approved decision to map them to an existing controlled icon. Text-only placeholders are not acceptable for final release.

The final navigation inventory shall include every destination actually present in the project. New icons for Operate, Function, Recipe and Efficiency shall use simple vector geometry, consistent stroke/weight, a navy/blue base and transparent background, then pass the same XML, security, render and hash checks as the supplied library.

---

## 5. Production Mimic Design

### 5.1 Product Path
Use:
- Blue pipe SVG/polyline
- Animated or highlighted flow state
- Product pressure value
- Product presence status

### 5.2 M102 Product Pump
Use reusable pump faceplate.

Bindings:
- `RunCmd`
- `RunningFB`
- `Fault`
- `ManualMode`
- `AutoMode`
- `SpeedPct` or `FrequencyHz`
- `CommOK`

Visual:
- Grey stopped
- Green running
- Red fault
- Amber manual/attention if desired

Controlled graphic: `pump_product.svg`.

Current binding baseline:

- Run output: `DO_ProductPump_Run`
- Ready: `DI_ProductPump_Ready`
- Fault: `DI_ProductPump_Fault`
- Existing process run state may also be exposed as `Product_Pump_Run`; the binding audit shall identify the authoritative display signal.
- Speed: use only a confirmed product-pump feedback/reference tag and label it accurately.

### 5.3 Filler Process Block
Use:
- Modern machine/filler SVG or grouped native shapes
- TLS100 state/value
- Relevant process readouts
- Valve states around the process

Do not use a crude rectangular debug box as the primary visual.

### 5.4 M103 Vacuum Pump
Same implementation pattern as M102.

Bindings:
- Running
- Fault
- Speed/frequency
- CommOK

Controlled graphic: `pump_vacuum.svg`.

M103 states shall be discovered and recorded independently: running feedback, fault/trip, ready/enabled/permissive, actual speed/frequency/percentage, auto/manual mode and communication/quality. `DO_Vacuum_Pump` may represent a command/output state but shall not be mislabeled as running feedback. Any state not proven in the project is `MISSING` and is omitted or shown as `N/A`; it is never simulated. Command signals are reserved for authorized detail/manual control and are not exposed as direct actions on the Production mimic.

Only the command/output state `DO_Vacuum_Pump` is presently confirmed. Ready, fault, speed/frequency and communication signals remain open points and shall display `Not Configured` rather than copied M102 data or invented values.

### 5.5 Vacuum Display
Canonical HMI unit: `mbar`.

If PLC value is stored in bar:
- HMI display conversion = `bar × 1000`

Preferred:
- Perform conversion through a clearly documented HMI display expression or dedicated display tag
- Do not change PLC engineering units unless separately approved

### 5.6 TLS100
Display:
- Code `TLS100`
- Description `Tank Level Sensor`
- Value %
- Quality
- Alarm state if applicable

Controlled graphic: `sensor_analog.svg`.

The tank value shall bind to `Tank_Level_Pct`. Quality and alarm bindings remain subject to the binding inventory.

### 5.7 Product Presence Sensor

Controlled graphic: `sensor_digital.svg`.

The symbolic value is currently available through `Product_Present_At_Pump`. The visible engineering code remains blocked until `SPC100` versus `SPL100` is reconciled with the electrical documentation.

### 5.8 Process Valves

Controlled graphic: `valve.svg`.

Each valve instance shall retain its actual engineering code and bind to its existing command/feedback/fault signals. When only a command state exists, the HMI shall label that state as command, not feedback.

---

## 6. KPI Components

### 6.1 Speed Gauge
Component type:
- Native circular arc + text, or SVG gauge with native dynamic properties

Inputs:
- `ActualSpeed`
- `SetpointSpeed`
- `Min`
- `Max`
- `Unit`

Dynamic:
- Arc/needle position = normalized actual speed
- Numeric actual speed shown prominently

Current implementation rule:

- Actual percent feedback: `Speed_Feedback_Pct` when `Speed_Feedback_Valid` is true.
- `Speed_Actual_Pct` is the main-drive output/reference and shall not replace feedback silently.
- Setpoint `Speed_Setpoint_BPH` shall be displayed separately until a validated conversion makes it comparable to the percent scale.
- Invalid feedback shall show a distinct bad-quality/not-valid state, not zero.

### 6.2 Efficiency Gauge
Component:
- Circular progress/ring

Input:
- `EfficiencyPct`

Dynamic:
- Ring length/angle = `EfficiencyPct`
- Text = `%`

Approved interim calculation:

`EfficiencyPct = Efficiency_Run_Seconds × 100 / Efficiency_Total_Seconds`

If `Efficiency_Total_Seconds <= 0`, display `N/A`, not `0%`, because efficiency has not yet been established.

### 6.3 Produced Today
Component:
- Bottle SVG
- Large count
- Caption

Binding:
- Existing daily production counter

No qualifying daily production counter has been confirmed. `Bottle_Count_In_Machine` is a different diagnostic value and is prohibited as a substitute. The card remains `Not Configured` until a real counter and daily reset basis are approved.

The project search shall inspect PLC/HMI tags, DB logic, counter increments, cross-references and reset events. A lifetime total shall retain its actual description and shall not be relabeled as Produced Today. No new daily counter/reset logic is authorized in this phase.

Controlled graphic: `bottle.svg`.

### 6.4 Product Pressure

No product-pressure tag is presently confirmed. Keep the approved space in the mimic, show `Not Configured`, and record the requirement in the open-point register. Do not reuse tank level, vacuum or another analog signal.

The search shall cover PLC tags/DBs, analog-input scaling blocks, HMI tags, alarms and existing screens. A confirmed binding must document physical meaning, source, engineering unit and scaling; it shall not be labeled bar, mbar or psi until the project proves the unit.

---

## 7. Device Faceplates

Controlled assets provide only the visual layer. Each faceplate shall add native WinCC Unified parameters, quality handling, states and interaction. A fixed SVG fill is not itself a dynamic state implementation.

### 7.1 Valve Faceplate
Parameters:
- DeviceCode
- Description
- CommandOpen
- CommandClose
- OpenFB
- ClosedFB
- Fault
- ManualMode
- AutoMode
- CommOK

### 7.2 Digital Sensor Faceplate
Parameters:
- DeviceCode
- Description
- Location
- State
- Quality

Visual:
- Grey = FALSE
- Green = TRUE
- Diagnostic state if bad quality

### 7.3 Analog Sensor Faceplate
Parameters:
- DeviceCode
- Description
- Value
- Unit
- Min
- Max
- WarningLow
- WarningHigh
- Fault

---

## 8. Device Popup Design

Touching a device opens a popup.

Popup shall show:
- Device code
- Functional description
- Location
- Live value/state
- PLC tag
- HMI tag
- Mode
- Interlocks
- Fault state
- Navigation to manual/diagnostics if user is authorized

Manual control shall not be embedded in the mimic itself.

---

## 9. Dynamic Animation Strategy

Use animation selectively.

### Allowed
- Pump rotation/running indication
- Flow highlighting
- Valve open/closed movement
- Gauge movement
- Level/bar movement
- Selected navigation state

### Avoid
- Continuous decorative animation
- Flashing normal-status lamps
- Excessive motion
- Animation driven by guessed values

Runtime performance must remain stable.

---

## 10. Tag Binding Strategy

### Preferred Binding Order
1. Reuse existing HMI tag
2. Reuse existing PLC symbolic tag
3. Extend an existing interface DB
4. Create new tag only if no equivalent exists

### Naming
Follow existing project naming.

Device code remains visible separately from the symbolic PLC tag.

Example:
- Device code: `M102`
- PLC tag: `DB_Global.Out.ProductPumpRun`
- HMI tag: existing mapped equivalent

### Binding Matrix Columns

The implementation binding matrix shall contain at least:

- HMI object/component
- Visible engineering code
- Functional description
- HMI tag
- PLC symbolic path
- Data type
- Read/write access
- Engineering unit
- Scaling/conversion
- Validity or quality source
- Warning source
- Fault source
- Manual/Auto source
- Communication source
- Fallback display
- Verification status
- REV14.1 readiness status: `CONFIRMED`, `MISSING`, `AMBIGUOUS` or `NOT REQUIRED`
- Evidence/cross-reference note

The matrix is required for every Production visual object, including static-looking state indicators and popup fields. Its minimum authoritative fields are device code/functional name, PLC symbolic path, existing HMI path, data type, engineering unit, scaling/conversion, normal semantics, fault/warning semantics, quality/communication source, read/write direction, readiness status and evidence.

---

## 11. Quality and Fault Handling

Every dynamic object shall distinguish:
- Valid OFF
- Valid ON
- Warning
- Fault
- Bad communication/quality
- Not configured

Do not visually represent `0` as valid if the tag quality is bad.

---

## 12. Performance

Avoid:
- Excessive scripts
- High-frequency polling outside standard HMI mechanisms
- Large unoptimized SVG files
- Too many independent animation objects

Prefer:
- Native animations
- Reusable faceplates
- Shared styles
- Optimized SVG assets
- Tag-driven properties

---

## 13. Security

Visibility/enabled states shall be bound to user role.

Operator:
- Routine production/CIP

Maintenance:
- Bypass/setup/manual controls

Engineer:
- Advanced engineering

Protected actions must remain PLC-interlocked even when visible.

The user-administration design shall map Operator, Maintenance and Engineer to the approved routine/protected access matrix. The inactivity timeout returns to Operator/default access through a supported WinCC Unified mechanism; it shall not merely hide controls while leaving a privileged session active.

---

## 14. Final Visual Validation

The Automation Agent must provide:
- Full-screen Production screenshot
- Full-screen Home screenshot
- Full-screen Safety screenshot
- Comparison against approved reference
- Tag-binding report
- Faceplate/object inventory
- Compile evidence

Validation shall use the native HMI canvas at 100% zoom. The comparison report shall identify every deliberate deviation from the approved reference, with its technical reason and approval status.

## 15. Controlled SVG Import Strategy

Before importing graphics:

1. Parse every file as XML.
2. Validate `viewBox`, IDs, links, scripts, fonts and external resources.
3. Record SHA-256 and intended component usage.
4. Render each asset at its intended HMI size and inspect clipping and stroke consistency.
5. Correct `nav_cip.svg`, which currently contains a duplicate `fill` attribute.
6. Import the validated files into a dedicated REV14 project-graphics folder.
7. Reference the imported project graphic from reusable components; do not link runtime objects to the filesystem path.

The asset manifest shall record filename, semantic function, asset version, final SHA-256, validation result and intended HMI use. Assets that fail any validation check shall not be imported.

### Dynamic Color Strategy

Because imported SVG colors may not support direct runtime recoloring in every Unified object type, each component shall use one verified strategy:

- Graphic I/O list with OFF/ON/WARNING/FAULT/BAD-QUALITY variants;
- native colored state overlay behind or above a neutral SVG;
- faceplate state layers with controlled visibility;
- another tested WinCC Unified native mechanism.

The chosen strategy shall be demonstrated on one pump, one valve, one sensor and one navigation button before bulk creation.

## 16. Preparation Evidence and Stop Logic

The preparation package shall contain:

- Validated SVG library and asset manifest.
- Complete PLC/HMI binding matrix.
- Open-item register limited to evidence-backed `MISSING` and `AMBIGUOUS` items.
- Access matrix and inactivity-timeout implementation note.
- Home and Production pilot screenshots at native resolution and 100% zoom.
- HMI and PLC compile evidence.
- Before/after comparison and pilot approval record.

The agent requests human input only for indistinguishable signals, missing signals that require PLC changes, device-code conflicts requiring unavailable external documentation, or WinCC Unified capability conflicts with the approved visual design. Pilot approval is required before the global frame is propagated beyond Home and Production.
