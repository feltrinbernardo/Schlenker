# HMI Layout Style Reference - REV11 Working

## Scope

The operator supplied eleven photographs of an HMI from a different machine.
They are accepted as examples of preferred screen organisation and visual
style only. Do not copy the example machine's PLC tags, equipment names,
recipes, parameters, setpoints, page numbers, permissives or operating logic.

The Schenker HMI must use the functions and bilingual text defined in the
REV11 working package and must be verified offline in TIA Portal V19/WinCC
Unified before any hardware download.

## Reusable visual structure

- Fixed right-side primary navigation rail.
- Persistent top status area for machine mode, active alarm summary, logged-in
  user and date/time.
- Page title and page identifier near the upper-right of the content area.
- Previous-page navigation at the upper-left.
- Blue/white base palette with a clear highlighted state for the active page.
- Section headers above grouped process values, commands and diagnostics.
- Large touch targets with text labels; icons support the labels but do not
  replace them.
- Consistent alignment and spacing across every subsystem page.

## Proposed Schenker primary navigation

1. Home
2. Operate
3. Function
4. Alarms
5. Recipe
6. Setup
7. Manual
8. Trends

Access-controlled engineering and maintenance pages may appear within Setup or
Manual. Safety functions must never depend on HMI access control alone.

## Screen-pattern guidance

### Home

- Show a simplified Schenker monoblock line/machine overview using the approved
  mechanical-layout reference.
- Show current machine state, throughput, critical process values and concise
  operator commands.
- Place the independent machine-light and air-filter ON/OFF controls here with
  command status, air-filter pressure proof, fault and filter-change warning.
- Show the aggregate `Emergency stop pressed` alarm without trying to identify
  an individual series-wired E-stop.

### Operate and Function

- Use process mimics for filler, capper, product pump, vacuum/CIP and
  accumulation functions.
- Display sensor and actuator states next to the represented device.
- Distinguish command, feedback and alarm states; a PLC command is not proof of
  physical motion or energisation.

### Manual

- Organise manual tests by subsystem, following the example's equipment-page
  approach.
- Display the real SMC identification beside every valve command, for example
  `Gate Open -125Y1-`.
- Manual commands require machine Manual mode, the relevant HMI page to remain
  active and the existing permissions/interlocks. Leaving Manual mode or the
  page returns the output to its normal automatic path in the same PLC scan.
- Do not reproduce example-machine actuator names or addresses.

### Alarms

- Provide active alarm, alarm history and acknowledgement views.
- Use plain-language cause and operator action in English and Italian.
- Keep warnings such as `Air Filter Needs Replacement` visually distinct from
  machine-stopping faults.

### Recipe

- Use record selection, record number/name, comparison status and deliberate
  load/download controls similar to the supplied layout.
- Require access level and confirmation for recipe transfer or parameter
  changes.
- Recipe values and limits must come only from the Schenker process definition.

### Setup and parameters

- Use aligned two-column parameter tables with engineering units, allowed
  ranges and access-controlled edit fields.
- Display actual value separately from setpoint.
- Record or expose parameter-change audit information where supported.

### Trends

- Use a large plotting area, selectable pens, legend, current value, time range
  and pan/zoom controls.
- Initial Schenker pens should include product level, product-pump command,
  relevant pressure signals and production speed where those tags are valid.

### External-signal diagnostics

- Use two opposing columns and direction arrows for signals exchanged with
  external systems.
- Label each signal as incoming or outgoing and show state, quality and timeout
  or communications fault where available.

## Colour and interaction rules

- Grey: unavailable, disabled or inactive.
- Blue: navigation and neutral operator action.
- Green: confirmed active/healthy feedback.
- Amber: warning or attention required.
- Red: active fault, alarm or stop condition.

Colour must always be supported by text, symbol or state label. Do not use
colour alone to convey status. Momentary commands must be visibly momentary;
latched commands must show both requested state and confirmed feedback when a
feedback exists.

## Physical MAN/AUT selector

The new HMI may display the physical keyed selector state. Whether touchscreen
Auto/Manual buttons are status-only, gated commands or unavailable must be
decided after the selector contact arrangement and operating authority are
confirmed. A mode selection must never initiate automatic restart.
