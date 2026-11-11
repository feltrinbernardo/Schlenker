# SCHLENKER 36/10 — PLC Motor List

## 1. Current verified status

The project contains **six motorized functions**:

- four machine drives intended to use SINAMICS G120C;
- one vacuum-pump motor;
- two ventilation/hood fan assemblies.

Only five functions currently have confirmed electrical equipment codes,
`M100` through `M104`. The cap-distributor drive is required by the PLC logic
and engineering specification, but its final equipment code and cable entry
are missing.

The latest offline TIA Portal hardware inventory does not show a configured
SINAMICS G120C device. Consequently, the drive names and PLC interfaces below
are logical engineering references, not proof of commissioned hardware,
PROFINET telegrams, addresses or motor nameplate data.

## 2. Motor summary

| No. | Equipment code | Motorized function | Intended control | Cable reference | Current status |
|---:|---|---|---|---|---|
| 1 | M100 | Main machine | SINAMICS G120C VFD | C-032 | Motor data stated as known; drive hardware/channel still not configured |
| 2 | M101 | Bottle conveyor | SINAMICS G120C VFD | C-033 | Motor data and drive configuration pending |
| 3 | M102 | Product pump | SINAMICS G120C VFD | C-034 | Motor data and drive configuration pending |
| 4 | TBC | Cap distributor | SINAMICS G120C VFD | Missing / TBC | Equipment code, motor data, cable and drive configuration pending |
| 5 | M103 | Vacuum pump | Contactor or VFD — TBC | C-035 | Motor data and final starter architecture pending |
| 6 | M104A / M104B | Hood fan 1 / hood fan 2 | 230 VAC single-phase assembly; brushless motor electronics associated/integrated | C-036A / C-036B | Previous 400 VAC three-phase representation rejected; assembly protection, terminals and control interface TBC |

Planned G120C count: **4**. Configured G120C devices verified in the latest TIA
hardware inventory: **0**.

The offline PLC sources now explicitly pass `RunningFeedback := FALSE` and
`RunningFeedbackValid := FALSE` to all four drive managers. This removes the
previous command-as-feedback and circular-feedback behavior. Each drive remains
`NOT COMMISSIONED / DATA INVALID` until real G120C status-word and actual-speed
signals are mapped and validated.

## 3. PLC and HMI interface inventory

| Equipment | PLC command/reference | PLC feedback/reference | HMI reference | Interface completeness |
|---|---|---|---|---|
| M100 Main machine | `DB_Global.Out.MainRun`, `MainSpeedPct` | `MainDriveReady`, `MainDriveFault`, `MainSpeedActualPct`, `EncoderHealthy` | `DO_Main_Run`, `DI_MainDrive_Ready`, `DI_MainDrive_Fault`, `Speed_Actual_Pct`, `Speed_Feedback_Pct` | Symbolic interface present; telegram/channel/address TBC |
| M101 Bottle conveyor | `DB_Global.Out.ConveyorRun`, `ConveyorSpeedPct` | `ConveyorReady`, `ConveyorFault` | `DO_Conveyor_Run`, `DI_Conveyor_Ready`, `DI_Conveyor_Fault` | Symbolic interface present; actual speed/running feedback and telegram mapping TBC |
| M102 Product pump | `DB_Global.Out.ProductPumpRun`, `ProductPumpSpeedPct` | `ProductPumpReady`, `ProductPumpFault` | `DO_ProductPump_Run`, `DI_ProductPump_Ready`, `DI_ProductPump_Fault`, `Pump_Speed_Pct` | Symbolic interface present; actual speed/running feedback and telegram mapping TBC |
| Cap distributor, code TBC | `DB_Global.Out.CapDriveRun`, `CapDriveSpeedPct` | `CapDriveReady`, `CapDriveFault` | `DO_CapDrive_Run`, `DI_CapDrive_Ready`, `DI_CapDrive_Fault` | Symbolic interface present; equipment code, actual speed/running feedback and telegram mapping TBC |
| M103 Vacuum pump | `DB_Global.Out.VacuumPump` | No dedicated ready, running or fault feedback identified | `DO_Vacuum_Pump`, `Vacuum_Pump_Run` | Boolean command only; contactor/VFD and feedback architecture TBC |
| M104A / M104B Hood fans | probable common-enable association: `DB_Global.Out.AirFilterRelayCmd` | `AirFilterPressureOK`, `AirFilterFault`, `Hood1FilterClogged`, `Hood2FilterClogged` | `AirFilter_Relay`, `AirFilter_PressureOK`, `AirFilter_Fault`, `Hood1_FilterClogged`, `Hood2_FilterClogged` | `%Q4.2` is not approved as a direct motor connection; relay/interface and whether enable is common to both hoods remain TBC |

The PLC contains separate drive-manager structures for the main machine,
product pump, bottle conveyor and cap distributor. These structures must not be
treated as live drive communication until the real G120C telegrams and feedback
signals are configured and tested.

## 4. Cable and power information currently available

| Cable | Equipment | From | Cable description | Available motor data |
|---|---|---|---|---|
| C-032 | M100 Main machine | G120C output | VFD motor, `3P + PE` | Marked "Motor data known"; actual values are not present in the reviewed list |
| C-033 | M101 Bottle conveyor | VFD output | VFD motor, `3P + PE` | Awaiting motor data |
| C-034 | M102 Product pump | VFD output | VFD motor, `3P + PE` | Awaiting motor data |
| TBC | Cap distributor | G120C output expected | VFD motor expected | No controlled cable row or motor code identified |
| C-035 | M103 Vacuum pump | Contactor/VFD — TBC | `3P + PE` | Awaiting motor data |
| C-036A | M104A Hood 1 assembly | 230 VAC single-phase distribution | `L + N + PE`; size/protection TBC | Complete hood stated as 230 VAC single-phase; photographed motor is Nidec SIC-67FV-F15B-1, brushless, DC 310 V, 58 W, 8 poles, 1350 rpm |
| C-036B | M104B Hood 2 assembly | 230 VAC single-phase distribution | `L + N + PE`; size/protection TBC | Same architecture stated for Hood 2; second nameplate must be verified before construction |

The `DC 310 V` motor marking is an internal motor-bus rating, not permission to
apply 230 VAC to its motor leads. The complete hood's 230 VAC input must feed
the manufacturer's associated/integrated electronics. The five motor-interface
leads visible on the plate must not be assigned to PLC or mains terminals until
the hood controller and terminal documentation are identified.

## 5. Information required to complete the motor list

For every motor, confirm and record:

- manufacturer, model, order number and serial number;
- rated power in kW;
- rated voltage, current and frequency;
- rated speed in rpm;
- power factor, efficiency class and duty class;
- frame size, mounting arrangement, IP rating and insulation class;
- brake, thermistor/PTC and forced-ventilation requirements;
- installed drive or starter designation and order number;
- drive firmware and motor-identification data;
- G120C telegram, control/status words, speed scaling and process-image addresses;
- PROFINET device name and approved IP address;
- ready, running, warning, fault and reset mappings;
- safe torque off or other safety interface and required safe state;
- cable size, length, shielding, routing and protective-device rating;
- rotation direction and field-test evidence.

Specific unresolved identification items:

1. Assign and approve the cap-distributor motor code and cable reference.
2. Confirm the M103 starter and identify the two hood assemblies' integral/associated brushless controller terminals and protective devices.
3. Confirm whether `AirFilterRelayCmd` is a common enable for M104A and M104B and identify the required interposing relay/interface.
4. Provide the actual M100 data currently described only as "known".
5. Verify the second hood motor nameplate and provide remaining nameplate/sizing data for M101 and the cap distributor.

## 6. Source references

- `REV12/REV12_MANIFEST.json` — states that four SINAMICS G120C drives are required.
- `REV12/Documentation/REV11_to_REV12_Merge_Register.md` — identifies the four retained drive functions.
- `REV12/Documentation/REV12_Master_Cable_Schedule.csv` — M100–M104 cable records and motor-data status.
- `REV12/HMI/REV12_HMI_Tags.csv` — symbolic motor command and feedback tags.
- `REV12/PLC_Sources/01_UDT_Types.scl` — PLC input/output structures.
- `REV12/PLC_Sources/10O_FB_OpenPoints.scl` — four logical drive managers.
- `REV12/PLC_Sources/11_FB_Application.scl` — application commands, permissives and feedback integration.
- `runs/open-points-integral-20260828/final-compile-inventory.txt` — latest offline hardware inventory.

## 7. Release condition

This list may be marked commissioned only after the installed motors and drives
are reconciled with the electrical drawings and nameplates, configured in TIA
Portal, compiled without relevant errors, and tested against approved
development hardware under separate authorization.
