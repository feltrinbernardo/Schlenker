# Electrical Drawing Change Request — REV11 Working

## Status

Draft engineering change instruction. The current Schenker monoblock
electrical drawing and editable source were not supplied or found, so no issued
schematic sheet has been modified. Do not assign terminal numbers, wire
numbers, device identifiers, PLC channels, fuse ratings, relay ratings, or
conductor sizes from this document alone.

Implementation owner: the operator's AutoCAD Electrical programmer. This file
is the functional handoff; the programmer shall implement the changes in the
controlled drawing source and return the final identifiers and I/O assignments
for the TIA project.

## 1. Machine lights — confirmed 24 VDC

- Add the machine-light load to a protected 24 VDC auxiliary circuit after the
  electrical designer confirms the available power budget and load current.
- Provide one independent 24 VDC PLC digital output to energize a dedicated
  machine-lights relay coil.
- PLC command alias: `Aux_MachineLightsRelayCmd`.
- HMI Main-page commands: `HMI_Main_LightsOnPB` and
  `HMI_Main_LightsOffPB`.
- Assign the final PLC digital output, relay, contact and load circuit in the
  electrical drawing. The PLC output drives only the 24 VDC relay coil; the
  relay contact powers the protected 24 VDC lighting load.
- Show branch protection, relay-coil suppression where applicable, terminals,
  0 VDC return, wire numbers, cable/conductor size, and all physical light
  identifiers.
- Until a separate electrical feedback is confirmed, the HMI `Lights ON`
  indication represents the PLC command state, not proof of illumination.

## 2. Air filter

- Provide a second independent 24 VDC PLC digital output to energize a
  dedicated air-filter relay coil.
- PLC command alias: `Aux_AirFilterRelayCmd`.
- HMI Main-page commands: `HMI_Main_AirFilterOnPB` and
  `HMI_Main_AirFilterOffPB`.
- Existing proof alias: `b_HoodPressureOK`, described by the supplied package
  as a 24 VDC hood/filter pressure input.
- Add a separate 24 VDC maintenance input for the filter-change indication:
  `DI_AirFilterChangeRequired24V`. When energized, it produces the HMI warning
  `Air filter needs replacement`; it does not stop the filter by itself.
- Add the final PLC output, 24 VDC relay coil, relay contact, protection,
  terminals and filter load wiring. Confirm the filter load voltage and current
  before selecting the relay contact rating and downstream protection.
- Do not assume the air-filter motor/load itself is 24 VDC; only the existing
  pressure-proof signal and the new relay coil command are confirmed as 24 VDC.
- The PLC raises `Aux_AirFilterFault` if pressure proof is absent after the
  parameterized `Par_AirFilterProofDelay` while the filter command is active.

## 3. Seven physical controls below the HMI

The latest operator photograph confirms the following left-to-right physical
order. Show each device and cross-reference its PLC or Pilz destination:

1. `AUX. VOLTAGE` illuminated pushbutton - working alias
   `Panel_AuxiliaryResetPB`; confirm whether its contact resets/enables the
   auxiliary or safety circuit and identify its lamp circuit.
2. Keyed `MAN / AUT` selector - working logical alias
   `Panel_ModeSelectorAuto`; confirm contact arrangement, polarity and priority
   relative to the HMI Auto/Manual commands.
3. Illuminated `OPEN / CLOSE DOORS` pushbutton -
   `DoorAccess_RequestPanelPB`, with indication
   `DoorAccess_RequestPanelBlueLamp`. The standard PLC treats this as a request
   to begin controlled opening; it does not power-close a guard.
4. Blue `RESET` pushbutton - `Panel_AlarmResetPB`.
5. Green `START` pushbutton - `Panel_StartMachinePB`.
6. Red illuminated `STOP` pushbutton - `Panel_StopMachinePB`; identify its lamp
   circuit separately from the stop input.
7. Emergency stop - part of the four-device series circuit returning 24 VDC to
   the Pilz safety PLC; not a standard PLC safety function.

The panel E-stop is provisionally treated as the previously counted front
E-stop. Confirm whether it is the same device or an additional fifth E-stop.

## 4. Door-request stations

- Panel, front and rear physical request pushbuttons initiate the same standard
  PLC access request.
- Each station has a blue indication lamp.
- Common working indication command: `DoorAccess_RequestBlueLampCmd`.
- Assign three physical input channels and three physical lamp output channels.
- The blue lamps are non-safety indications and must not be used as guard-unlock
  proof.

## 5. Emergency-stop circuit

- Four operator-confirmed devices: front/panel, rear, infeed-door area and jog
  pendant.
- Contacts are operator-confirmed as a series circuit whose operation removes
  the 24 VDC return to the Pilz safety PLC.
- Standard PLC/HMI diagnostic alias: `Safety_EStopChain24VHealthy`, derived from
  the validated Pilz system.
- HMI uses one aggregate alarm: `Emergency stop pressed`; individual device
  identification is not required.
- The responsible safety engineer must validate the circuit architecture,
  contact arrangement, reset behavior, diagnostic coverage and Pilz channel.

## Required information before drawing issue

- Latest electrical drawing PDF and editable source, with title-block revision.
- PLC and remote-I/O module types plus available input/output channels.
- Two available 24 VDC digital-output channels and their permissible coil load.
- Two 24 VDC relay coil types, contact ratings and coil-suppression method.
- Machine-light quantity, total 24 VDC current and inrush.
- Air-filter motor/load voltage, current, starter/control method and protection.
- Existing 24 VDC distribution, power-supply capacity and protection scheme.
- Device, terminal, cable and wire-numbering conventions.
- Confirmation that the panel E-stop is the front E-stop rather than a fifth
  device.
- MAN/AUT selector contact diagram, normal state, input channel and control
  priority relative to the HMI Auto/Manual commands.

## Verification before release

- Electrical design review and load calculation completed.
- PLC output and input channels cross-checked against the TIA hardware project.
- Pilz/safety modifications independently reviewed and validated.
- Point-to-point continuity and polarity tests completed with power controlled
  under the approved commissioning procedure.
- HMI command, command indication, pressure proof and alarms tested offline/FAT.
- Updated drawing revision approved before any PLC/HMI download or field change.

## AutoCAD Electrical handback deliverables

- Updated editable AutoCAD Electrical project and issued PDF.
- Revision-clouded sheets and updated title-block revision/history.
- PLC I/O list identifying the two independent 24 VDC output channels.
- Final relay identifiers for the machine lights and air filter.
- Relay coil voltage, coil current, suppression and contact ratings.
- Fuse/protection, terminal, cable and wire numbers.
- Final equipment identifiers for the lights, air filter and physical controls.
- Cross-reference from `Aux_MachineLightsRelayCmd` and
  `Aux_AirFilterRelayCmd` to the issued drawing locations.
- Confirmation of the air-filter load voltage/current and the existing
  `b_HoodPressureOK` input channel.
- Final PLC input channel, terminal and wire number for
  `DI_AirFilterChangeRequired24V`.
