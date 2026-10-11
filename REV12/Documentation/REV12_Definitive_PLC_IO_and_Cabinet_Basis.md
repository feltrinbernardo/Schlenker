# Schlenker REV12 — Definitive PLC I/O and Cabinet Construction Basis

**Project:** Backup Schlenkers 36-10 190036-7-8v2.12
**Engineering platform:** Siemens TIA Portal V19
**PLC:** Siemens S7-1512C-1 PN
**Issue date:** 2026-10-10
**Purpose:** definitive PLC address allocation, physical connection destination and cabinet construction baseline.

## 1. Use of this document

The addresses in Sections 4 and 5 are the approved definitive allocation for the **integrated PLC I/O**. Every signal in Section 4 is wired **directly to the integrated digital-input channel of PLC_1 shown in its first column**. Every signal in Section 5 is wired **directly to the integrated digital-output channel of PLC_1 shown in its first column**. They are not IO-Link signals and shall not be wired to an AL1403 master.

The network areas in Section 5 are allocated in the TIA hardware configuration. Section 6 defines the separate AL1403/IO-Link/SIO architecture. A field device must appear in exactly one architecture. If a device is marked **HOLD — DO NOT WIRE**, the electrical office shall not select a master/port or duplicate a direct-PLC signal.

### 1.1 Authoritative connection decision

| Signals / device | Physical destination | Connection type | Cabinet instruction |
|---|---|---|---|
| `%I10.0`–`%I13.7` listed in Section 4 | `PLC_1`, integrated DI, exact channel = address in first column | Direct PLC, 24 VDC digital input | Wire to PLC integrated input terminals; **not AL1403** |
| `%Q4.0`–`%Q7.7` listed in Section 5 | `PLC_1`, integrated DQ, exact channel = address in first column | Direct PLC, 24 VDC digital output | Wire from PLC integrated output terminals; use interface relay where specified; **not AL1403** |
| `SPL100` IFM `LMT100` | `AL103`, port `P1`; process `%IW324`, PQI `%IB326` | IO-Link | Approved configured port |
| IFM `IFS240` (`IFK3004BBPKG/US-104`) units | AL1403 port, but exact master/port only after each sensor function and quantity are assigned in Section 6 | **SIO digital input**, PNP normally-open; **not IO-Link** | **HOLD — DO NOT WIRE until the assignment table is completed** |
| Other AL100–AL104 ports | Exact port schedule in Section 7 only | IO-Link or SIO as stated per row | Unassigned ports remain **HOLD — DO NOT WIRE** |

> Important: the PLC address is not a cabinet terminal number. The electrical office must assign terminal-strip numbers in the schematic, but the destination device and PLC channel above are fixed. Terminal numbers must not be invented in software documentation.

## 2. Verified automation components

| Qty | Tag / device | Component | Interface / role | Construction status |
|---:|---|---|---|---|
| 1 | PLC_1 | Siemens S7-1512C-1 PN compact CPU | PROFINET, 32 integrated DI, 32 integrated DQ | Selected and configured |
| 1 | HMI_1 | Siemens MTP1200 Unified Comfort as presently shown in the TIA project | PROFINET HMI | Configured; order number to be verified from hardware properties before purchasing |
| 1 | G120C_MainMachine | SINAMICS G120C | Main machine VSD, Standard telegram 1 | Configured |
| 1 | G120C_BottleConveyor | SINAMICS G120C | Bottle conveyor VSD, Standard telegram 1 | Configured and bench-tested |
| 1 | G120C_ProductPump | SINAMICS G120C | Product pump VSD, Standard telegram 1 | Configured |
| 1 | G120C_CAP_DISTRIBUTOR | SINAMICS G120C PN, 6SL3210-1KE11-8AF1 | Cap distributor VSD, Standard telegram 1 | Configured and bench-tested |
| 1 | anybus-smc | HMS Anybus ABC3113-A | PROFINET to EtherCAT gateway | Selected and configured |
| 1 | Valve manifold | SMC EX260-SEC1 with eight VQC1100N-51 valves | EtherCAT pneumatic output manifold | Selected; final valve/coil wiring to follow pneumatic drawings |
| 5 | AL100–AL104 | IFM AL1403 IO-Link masters | PROFINET field I/O | Selected and configured |
| 1 | SPL100 | IFM LMT100 | Product/water presence before pump, AL103 port 1 | Detected and mapped |
| 1 | M100 | Main machine motor | Driven by G120C | Motor data to be verified before final VSD commissioning |
| 1 | M101 | Bottle conveyor motor | Driven by G120C | Motor data to be verified before final VSD commissioning |
| 1 | M102 | Product pump motor, ABB M2QA90L2A | 2.2 kW, 380–420 V Y, 4.69 A, 50 Hz, 2850 rpm | Plate data recorded |
| 1 | M103 | Cap distributor motor, Western Electric | 0.37 kW, 415 V, 50 Hz, 1370 rpm | Bench-tested; rated current and cos phi still require clear plate confirmation |

## 3. Project tree — automation, drives and field I/O

This tree is the construction view of the project. It separates direct PLC I/O, PROFINET devices, the Anybus/SMC pneumatic branch and the AL1403 field-sensor branch.

| Parent | Child / device | Interface | Connected functions |
|---|---|---|---|
| Project | `PLC_1` S7-1512C-1 PN | Main controller | Program, integrated DI/DQ and PROFINET controller |
| `PLC_1` | Integrated DI `%I10.0–%I13.7` | Direct 24 VDC inputs | All Section 4 input signals; not IO-Link |
| `PLC_1` | Integrated DQ `%Q4.0–%Q7.7` | Direct 24 VDC outputs | Lights, filter relay, requests and Section 5 outputs; not IO-Link |
| `PLC_1` | `HMI_1` MTP1200 Unified Comfort | PROFINET | Operator commands, status, alarms and setpoints |
| `PLC_1` | `G120C_MainMachine` | PROFINET / telegram 1 | Main-machine motor |
| `PLC_1` | `G120C_BottleConveyor` | PROFINET / telegram 1 | Bottle-conveyor motor |
| `PLC_1` | `G120C_ProductPump` | PROFINET / telegram 1 | Product-pump motor |
| `PLC_1` | `G120C_CAP_DISTRIBUTOR` | PROFINET / telegram 1 | Cap-distributor motor |
| `PLC_1` | `anybus-smc` HMS ABC3113-A | PROFINET gateway | Four-byte command image `%QD128` |
| `anybus-smc` | SMC EX260-SEC1 valve manifold | EtherCAT | Eight VQC1100N-51 pneumatic valves; final coil/function schedule follows pneumatic design |
| `PLC_1` | `AL100` | PROFINET IO-Link master | Ports remain HOLD until verified field allocation |
| `PLC_1` | `AL101` | PROFINET IO-Link master | Ports remain HOLD until verified field allocation |
| `PLC_1` | `AL102` | PROFINET IO-Link master | Ports remain HOLD until verified field allocation |
| `PLC_1` | `AL103` | PROFINET IO-Link master | P1 = SPL100/LMT100; remaining ports HOLD |
| `PLC_1` | `AL104` | PROFINET IO-Link master | Ports remain HOLD until verified field allocation |
| `AL100–AL104` | IFM IFS240 sensors | M12, SIO DI, PNP NO | Exact sensor quantity/function/master/port still required; DO NOT WIRE until allocated |

### 3.1 Available verified photographs

![SINAMICS G120C photographed on the bench](C:/www/Schlenker/.codex-remote-attachments/01a0b2a8-9afd-7e40-8df2-f046eda2b7d7/cdc9d5a4-7abe-4f25-b8db-5fd5008d6d79/1-1000156603.jpg)

![IFM IFS240 package photographed by the operator](C:/www/Schlenker/.codex-remote-attachments/01a0b2a8-9afd-7e40-8df2-f046eda2b7d7/6e95ecb9-6431-440f-ba23-0fe584a0fa1d/1-1000156638.jpg)

![Cap distributor motor nameplate photographed by the operator](C:/www/Schlenker/.codex-remote-attachments/01a0b2a8-9afd-7e40-8df2-f046eda2b7d7/d3aab938-8ca7-408c-bcff-3f87afb9b4fc/1-1000156113.jpg)

Only photographs that identify the actual supplied or bench-tested component are used. Components without an operator photograph remain identified by verified tag and model in the tree; no catalogue image of a possibly different variant is substituted.

## 4. Integrated digital inputs — definitive allocation

**PHYSICAL DESTINATION FOR EVERY ROW BELOW: `PLC_1` integrated DI, direct 24 VDC input. The exact input channel is the `%I...` address in the first column. Do not connect these signals to AL100–AL104.**

| PLC address | PLC signal | Field function | Electrical drawing requirement |
|---|---|---|---|
| `%I10.0` | `DB_Global.Inp.SafetyOK` | Safety chain healthy | Potential-free/24 VDC status from safety system; not a replacement for safety-rated wiring |
| `%I10.1` | `DB_Global.Inp.AirPressureOK` | Main pneumatic pressure healthy | Digital sensor/contact |
| `%I10.2` | `DB_Global.Inp.EncoderHealthy` | Main encoder healthy | Diagnostic contact/interface status |
| `%I10.3` | `DB_Global.Inp.MachineZero` | Machine zero/reference position | Digital sensor |
| `%I10.4` | `DB_Global.Inp.BottleInfeed` | Bottle detected at infeed | Photoelectric sensor |
| `%I10.5` | `DB_Global.Inp.TankFull100` | Tank full / 100% limit | Level switch/status |
| `%I10.6` | `DB_Global.Inp.BottleShortage1` | Bottle shortage stage 1 | Digital sensor/status |
| `%I10.7` | `DB_Global.Inp.BottleShortage2` | Bottle shortage stage 2 | Digital sensor/status |
| `%I11.0` | `DB_Global.Inp.Accumulation1` | Bottle accumulation stage 1 | Digital sensor/status |
| `%I11.1` | `DB_Global.Inp.Accumulation2` | Bottle accumulation stage 2 | Digital sensor/status |
| `%I11.2` | `DB_Global.Inp.GateOpenFB` | Gate open feedback | Limit/reed switch |
| `%I11.3` | `DB_Global.Inp.GateClosedFB` | Gate closed feedback | Limit/reed switch |
| `%I11.4` | `DB_Global.Inp.FillerLowLimit` | Filler lower limit | Limit switch |
| `%I11.5` | `DB_Global.Inp.FillerHighLimit` | Filler upper limit | Limit switch |
| `%I11.6` | `DB_Global.Inp.CapperLowLimit` | Capper lower limit | Limit switch |
| `%I11.7` | `DB_Global.Inp.CapperHighLimit` | Capper upper limit | Limit switch |
| `%I12.0` | `DB_Global.Inp.CapPresent` | Cap present | Cap sensor |
| `%I12.1` | `DB_Global.Inp.CapHopperLow` | Cap hopper low | Level sensor |
| `%I12.2` | `DB_Global.Inp.CapChannelDemand` | Cap channel demand | Digital sensor/status |
| `%I12.3` | `DB_Global.Inp.CapsMissing` | Caps missing / chute empty | Digital sensor/status |
| `%I12.4` | `DB_Global.Inp.HoodFilterAlarm` | Hood/filter alarm | Differential-pressure/status contact |
| `%I12.5` | `DB_Global.Inp.ControlPowerOK` | 24 VDC control power healthy | PSU/monitor auxiliary contact |
| `%I12.6` | `DB_Global.Inp.AirFilterPressureOK` | Air-filter pressure proof | Pressure-switch contact |
| `%I12.7` | `DB_Global.Inp.AirFilterChangeRequired24V` | Filter replacement required | Filter monitor contact |
| `%I13.0` | `DB_Global.Inp.RequestOpenDoorPanelPB` | Panel-door request pushbutton | 24 VDC pushbutton contact |
| `%I13.1` | `DB_Global.Inp.RequestOpenDoorFrontPB` | Front-door request pushbutton | 24 VDC pushbutton contact |
| `%I13.2` | `DB_Global.Inp.RequestOpenDoorRearPB` | Rear-door request pushbutton | 24 VDC pushbutton contact |
| `%I13.3` | `DB_Global.Inp.ControlledStopComplete` | Controlled stop complete | External sequence/status contact |
| `%I13.4` | `DB_Global.Inp.ZeroSpeedConfirmed` | Zero speed confirmed | Drive/safety status contact |
| `%I13.5` | `DB_Global.Inp.ThreePhaseOffConfirmed` | Three-phase supply isolated confirmation | Contactor/monitor auxiliary contact |
| `%I13.6` | `DB_Global.Inp.AllDoorsClosed` | All guarded doors closed | Safety-system status only; safety function remains hardwired/safety-rated |
| `%I13.7` | `DB_Global.Inp.ResetAlarmPanelPB` | Panel alarm reset pushbutton | 24 VDC pushbutton contact |

## 5. Integrated digital outputs — definitive allocation

**PHYSICAL DESTINATION FOR EVERY ROW BELOW: `PLC_1` integrated DQ, direct 24 VDC output. The exact output channel is the `%Q...` address in the first column. Do not connect these signals to AL100–AL104.**

| PLC address | PLC signal | Field function | Electrical drawing requirement |
|---|---|---|---|
| `%Q4.0` | `DB_Global.Out.VacuumPump` | Vacuum-pump contactor command | Interposing relay/contactor circuit; protection and overload feedback per motor design |
| `%Q4.1` | `DB_Global.Out.MachineLightsRelayCmd` | Machine-lights relay command | Interposing relay for lighting circuit |
| `%Q4.2` | `DB_Global.Out.AirFilterRelayCmd` | Air-filter relay command | Interposing relay/contactor for filter unit |
| `%Q4.3` | `DB_Global.Out.GateOpen` | Gate-open solenoid command | Valve/relay circuit |
| `%Q4.4` | `DB_Global.Out.GateClose` | Gate-close solenoid command | Valve/relay circuit |
| `%Q4.5` | `DB_Global.Out.FillerUp` | Filler lift up command | Valve/relay circuit |
| `%Q4.6` | `DB_Global.Out.FillerDown` | Filler lift down command | Valve/relay circuit |
| `%Q4.7` | `DB_Global.Out.CapperUp` | Capper lift up command | Valve/relay circuit |
| `%Q5.0` | `DB_Global.Out.CapperDown` | Capper lift down command | Valve/relay circuit |
| `%Q5.1` | `DB_Global.Out.EV210` | Solenoid valve EV210 | Valve coil/relay circuit |
| `%Q5.2` | `DB_Global.Out.EV211` | Solenoid valve EV211 | Valve coil/relay circuit |
| `%Q5.3` | `DB_Global.Out.EV212` | Solenoid valve EV212 | Valve coil/relay circuit |
| `%Q5.4` | `DB_Global.Out.EV220` | Solenoid valve EV220 | Valve coil/relay circuit |
| `%Q5.5` | `DB_Global.Out.EV221` | Solenoid valve EV221 | Valve coil/relay circuit |
| `%Q5.6` | `DB_Global.Out.EV230FastRelease` | EV230 fast-release command | Valve coil/relay circuit |
| `%Q5.7` | `DB_Global.Out.ExternalBottleWashValveCmd` | External bottle-wash valve | Valve coil/relay circuit |
| `%Q6.0` | `DB_Global.Out.UnlockAllDoorsRequest` | Guard-door unlock request | Request to safety/locking circuit; never use as the safety function itself |
| `%Q6.1` | `DB_Global.Out.RequestPanelBlueLampCmd` | Panel request blue lamp | 24 VDC lamp/relay circuit |
| `%Q6.2` | `DB_Global.Out.RequestFrontBlueLampCmd` | Front request blue lamp | 24 VDC lamp/relay circuit |
| `%Q6.3` | `DB_Global.Out.RequestRearBlueLampCmd` | Rear request blue lamp | 24 VDC lamp/relay circuit |
| `%Q6.4` | `DB_Global.Out.Valve217Cmd` | Valve 217 command | Valve coil/relay circuit |
| `%Q6.5` | `DB_Global.Out.Valve213Cmd` | Valve 213 command | Valve coil/relay circuit |
| `%Q6.6` | `DB_Global.Out.Valve212Cmd` | Valve 212 command | Valve coil/relay circuit |
| `%Q6.7` | `DB_Global.Out.Valve247Cmd` | Valve 247 command | Valve coil/relay circuit |
| `%Q7.0` | `DB_Global.Out.InternalWashValveCmd` | Internal-wash valve command | Valve coil/relay circuit |
| `%Q7.1` | `DB_Global.Out.CIPReqColdWater` | CIP cold-water request | Interface relay/status output |
| `%Q7.2` | `DB_Global.Out.CIPReqHotWater` | CIP hot-water request | Interface relay/status output |
| `%Q7.3` | `DB_Global.Out.CIPReqAcid` | CIP acid request | Interface relay/status output |
| `%Q7.4` | `DB_Global.Out.CIPReqCitra` | CIP Citra request | Interface relay/status output |
| `%Q7.5` | `DB_Global.Out.CIPReqDischarge` | CIP discharge request | Interface relay/status output |
| `%Q7.6` | `DB_Global.Out.ProductionReqProduct` | Production product request | Interface relay/status output |
| `%Q7.7` | Reserved, forced `FALSE` | Spare integrated output | Bring to spare terminal; keep unenergised in software |

## 6. PROFINET and distributed I/O areas

| Device / area | PLC input area | PLC output area | Function |
|---|---:|---:|---|
| G120C main machine | `%IW308`, `%IW310` | `%QW140`, `%QW142` | Status word, actual speed; control word, speed setpoint |
| G120C bottle conveyor | `%IW312`, `%IW314` | `%QW144`, `%QW146` | Status word, actual speed; control word, speed setpoint |
| G120C product pump | `%IW316`, `%IW318` | `%QW148`, `%QW150` | Status word, actual speed; control word, speed setpoint |
| G120C cap distributor | `%IW320`, `%IW322` | `%QW152`, `%QW154` | Status word, actual speed; control word, speed setpoint |
| HMS Anybus ABC3113-A | Device diagnostics | `%QD128` | Four-byte command image to SMC EX260-SEC1 |
| IFM AL104 | 36-byte input area beginning `%IB126` | `%QB132` | IO-Link master process/diagnostic area; port functions per final device schedule |
| IFM AL102 | 36-byte input area beginning `%IB162` | `%QB133` | IO-Link master process/diagnostic area; port functions per final device schedule |
| IFM AL101 | 36-byte input area beginning `%IB198` | `%QB134` | IO-Link master process/diagnostic area; port functions per final device schedule |
| IFM AL100 | 36-byte input area beginning `%IB234` | `%QB135` | IO-Link master process/diagnostic area; port functions per final device schedule |
| IFM AL103 | 36-byte input area beginning `%IB270` | `%QB136` | IO-Link master; SPL100/LMT100 process word at `%IW324`, PQI at `%IB326` |

## 7. IFM AL1403 port allocation and IFS240 connection

### 7.1 Approved configured device

| Device | Function | Master | Port | Mode | PLC process data | Status |
|---|---|---|---|---|---|---|
| `SPL100` / IFM `LMT100` | Product/water presence before pump | `AL103` | `P1` | IO-Link | `%IW324`; PQI `%IB326` | **APPROVED / CONFIGURED** |

### 7.2 IFM IFS240 sensors shown by the operator

The photographed package identifies IFM `IFS240`, order code `IFK3004BBPKG/US-104`. Manufacturer data identifies it as a three-wire, 10–30 VDC, PNP normally-open inductive sensor with M12 connector. It has **no IO-Link interface**. When used through an AL1403 it must therefore use **SIO digital-input mode**, not IO-Link/IODD mode.

Manufacturer reference: `https://media.ifm.com/dam/f1582b25-9600-4283-9368-1fd3025d6291/Original/IFS240-01_EN-GB.pdf`

| Sensor instance | Machine function | Master | Port | Mode | PLC tag/address | Construction status |
|---|---|---|---|---|---|---|
| `IFS240-01...n` | Quantity and individual function have not yet been supplied | **NOT ASSIGNED** | **NOT ASSIGNED** | AL1403 **SIO DI**, PNP NO | **NOT ASSIGNED** | **HOLD — DO NOT WIRE** |

The office must receive the quantity and one machine-function name for each physical IFS240 before master/port allocation. Assigning AL100–AL104 ports without that information would invent the wiring and may duplicate a signal already fixed to the direct PLC inputs in Section 4.

### 7.3 AL1403 port rule

- A port is usable only when its row states device, machine function, master, port, mode and PLC tag/address.
- `IO-Link` is used only for a verified IO-Link-capable device with the correct port process-data configuration.
- `SIO DI` is used for a compatible conventional switching sensor such as IFS240.
- No signal from Section 4 may be repeated on an AL1403 port unless this document and the PLC program are formally revised together.
- The earlier planning allocation that associated bottle/cap functions with AL100/AL101 is **not a wiring authorization** because those functions currently exist in the definitive direct-PLC schedule.

## 8. Cabinet construction rules

1. Provide fused/protected 24 VDC distribution and separate field-power groups according to the final load calculation and applicable electrical standard.
2. Use interposing relays/contactors wherever the field load exceeds the PLC output rating or requires galvanic separation. Confirm every coil current before selecting the relay.
3. Fit suppression appropriate to every inductive DC coil. Confirm polarity where suppression devices are polarised.
4. Keep safety functions in the approved safety architecture. Standard PLC inputs and outputs in this schedule provide status and requests only; they do not replace safety-rated devices or circuits.
5. Terminate `%Q7.7` as a labelled spare and leave it unenergised.
6. Label wires, terminals and field devices with the PLC address and the tag in this document.
7. Keep PROFINET, motor, encoder, analogue and low-level sensor cables segregated from power wiring according to the manufacturer instructions.
8. Before energisation, perform point-to-point continuity, insulation, polarity, protective-earth and 24 VDC short-circuit checks.

## 9. Items the electrical office must complete before purchasing

- Where an exact order number is not stated in Section 2, obtain it from the approved TIA hardware properties or final vendor quotation; do not derive it from the description alone.
- Confirm the exact HMI order number because current project evidence shows MTP1200 Unified Comfort while older REV12 documentation names MTP1500 Unified Comfort.
- Export the exact CPU and accessory order numbers from TIA hardware properties.
- Size the 24 VDC power supply, protective devices, interposing relays, contactors, terminals and cable cross-sections from the final load calculation.
- Confirm motor protection and cable data for M100, M101 and M103 from clear motor/VSD nameplates.
- Provide the quantity and machine function of every IFS240, then formally allocate each unit to an AL1403 master/port in SIO mode before wiring.
- Finalise the IO-Link/SIO port allocation for all devices other than SPL100/LMT100 on AL103/P1.
- Produce the safety schematic and validate the safety performance level independently of the standard PLC allocation above.

## 10. Software verification

The definitive integrated I/O assignments are implemented in `FB_Application`. All 32 integrated outputs are cleared on CPU startup in `OB100_Startup`. The TIA V19 import and PLC compilation completed with **0 errors and 0 warnings** on 2026-10-09.
