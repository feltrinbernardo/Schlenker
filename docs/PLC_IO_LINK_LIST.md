# SCHLENKER 36/10 — PLC IO-Link List

## 1. Current verified status

The PLC project contains a logical IO-Link model for five master references, `AL100` through `AL104`, with eight logical ports per master.

The latest offline TIA Portal hardware inventory contains only the S7-1500/ET200MP station and `HMI_1`. It does **not** contain a configured IFM AL1403, AL100–AL104 PROFINET device, physical port address, or process-data mapping.

Therefore:

- physically configured IO-Link masters in the current TIA hardware: **0**;
- logical master references in the PLC: **5**;
- logical port allocations: **40**;
- confirmed intended IFM AL1403 scope: **4 masters**, represented by `AL100`–`AL103`;
- `AL104`: fifth logical interface reserved provisionally for Customer CIP;
- `AL100`–`AL103` are currently marked `Required := TRUE`;
- `AL104` is currently marked `Required := FALSE`;
- all physical addresses and port assignments remain `NOT CONFIGURED`.

No entry in this document should be interpreted as proof that a physical sensor, master, address, GSDML module, or IO-Link port has been commissioned.

## 2. PLC data structures

Each logical master uses:

```text
DB_Global.AL100 .. DB_Global.AL104 : UDT_IOLinkMasterMapping
```

Each master contains:

- `CommunicationOK` — verified master communication state;
- `HardwareConfigured` — true only after real TIA hardware configuration;
- `Port[1..8]` — eight instances of `UDT_IOLinkPortMapping`.

Each port contains:

- `Configured`;
- `QualityOK`;
- `DeviceFault`;
- `LiveBool`;
- `LiveReal`.

The current symbolic communication inputs are:

| Logical master | PLC input/status | Required | Current hardware status |
|---|---|---:|---|
| AL100 | `DB_Global.Inp.IOLinkMaster1OK` | TRUE | NOT CONFIGURED |
| AL101 | `DB_Global.Inp.IOLinkMaster2OK` | TRUE | NOT CONFIGURED |
| AL102 | `DB_Global.Inp.IOLinkMaster3OK` | TRUE | NOT CONFIGURED |
| AL103 | `DB_Global.Inp.IOLinkMaster4OK` | TRUE | NOT CONFIGURED |
| AL104 | `DB_Global.Inp.IOLinkMaster5Configured` / `IOLinkMaster5OK` | FALSE | PROVISIONAL / NOT CONFIGURED |

## 3. Master summary

| Master | Intended function | Planned active/reserved ports | Spare ports | Status |
|---|---|---:|---:|---|
| AL100 | Bottle and accumulation sensors | 4 | 4 | Required; hardware missing |
| AL101 | Cap system and air pressure | 5 | 3 | Required; hardware missing |
| AL102 | Process instrumentation | 6 candidates | 2 | Required; devices and scaling unconfirmed |
| AL103 | Additional machine sensors | 4 reserved/not installed | 4 | Required flag needs confirmation |
| AL104 | Customer CIP digital interface | 8 provisional | 0 | Optional; physical architecture unconfirmed |

## 4. Complete port allocation

### AL100 — Bottle and accumulation sensors

| Port | Device ID | Function | PLC logical tag | HMI tag | Mode | Project status |
|---|---|---|---|---|---|---|
| P1 | DI-031 | Bottle Shortage 1 — line slowdown | `b_BottleShortage_1` | `DI_Bottle_Shortage_1` | DI/SIO or IO-Link | FROZEN PORT / NOT CONFIGURED |
| P2 | DI-032 | Bottle Shortage 2 — gate/standby | `b_BottleShortage_2` | `DI_Bottle_Shortage_2` | DI/SIO or IO-Link | FROZEN PORT / NOT CONFIGURED |
| P3 | DI-033 | Outfeed Accumulation 1 — slowdown | `b_Accumulation_1` | `DI_Accumulation_1` | DI/SIO or IO-Link | FROZEN PORT / NOT CONFIGURED |
| P4 | DI-034 | Outfeed Accumulation 2 — gate/standby | `b_Accumulation_2` | `DI_Accumulation_2` | DI/SIO or IO-Link | FROZEN PORT / NOT CONFIGURED |
| P5 | SPARE-A100-05 | Future bottle sensor | — | — | Spare | SPARE |
| P6 | SPARE-A100-06 | Future bottle sensor | — | — | Spare | SPARE |
| P7 | SPARE-A100-07 | Future expansion | — | — | Spare | SPARE |
| P8 | SPARE-A100-08 | Future expansion | — | — | Spare | SPARE |

The final field-sensor models, IODDs, port modes, physical addresses, and polarities are not available.

### AL101 — Cap system and air pressure

| Port | Device ID | Function | PLC logical tag | HMI tag | Mode | Project status |
|---|---|---|---|---|---|---|
| P1 | DI-040 | Cap Present — Pick and Place | `b_CapPresent_PP` | `DI_Cap_Present` | DI/SIO or IO-Link | FROZEN PORT / NOT CONFIGURED |
| P2 | DI-060 | Cap Hopper Low Level | `b_CapHopper_Low` | `DI_Cap_Hopper_Low` | DI/SIO or IO-Link | FROZEN PORT / NOT CONFIGURED |
| P3 | DI-061 | Cap Channel Request | `b_CapChannel_Req` | `DI_Cap_Channel_Demand` | DI/SIO or IO-Link | FROZEN PORT / NOT CONFIGURED |
| P4 | DI-062 | Cap Channel Empty | `b_CapChannel_Empty` | `DI_Caps_Missing` | DI/SIO or IO-Link | FROZEN PORT / NOT CONFIGURED |
| P5 | DI-010 | Machine Air Pressure OK | `b_AirPressureOK` | `DI_AirPressure_OK` | DI or IO-Link if supported | FROZEN PORT / NOT CONFIGURED |
| P6 | SPARE-A101-06 | Cap/pneumatic expansion | — | — | Spare | SPARE |
| P7 | SPARE-A101-07 | Cap/pneumatic expansion | — | — | Spare | SPARE |
| P8 | SPARE-A101-08 | Future expansion | — | — | Spare | SPARE |

The final field-sensor models, IODDs, port modes, physical addresses, and polarities are not available.

### AL102 — Process instrumentation

| Port | Device ID | Function | Candidate device | PLC logical tag | HMI binding | Mode | Project status |
|---|---|---|---|---|---|---|---|
| P1 | PT-PRODUCT | Product Pressure | IFM PN7094 candidate | `ai_ProductPressure` | Missing | IO-Link | ASSIGN DEVICE AT BUILD / NOT CONFIGURED |
| P2 | PT-AIR | Pneumatic/Process Pressure Diagnostic | IFM PN7094 candidate | `ai_AirPressure` | Missing | IO-Link | ASSIGN DEVICE AT BUILD / NOT CONFIGURED |
| P3 | PT-CIP | CIP/Service Pressure Diagnostic | IFM PN7094 candidate | `ai_CIPPressure` | Missing | IO-Link | ASSIGN DEVICE AT BUILD / NOT CONFIGURED |
| P4 | PT-SPARE | Spare Pressure Measurement | IFM PN7094 candidate | `ai_SparePressure` | Missing | IO-Link | SPARE INSTRUMENT / NOT CONFIGURED |
| P5 | LT-PRODUCT | Product Continuous Level | IFM LR2050 candidate | `ai_ProductLevel` | Missing | IO-Link | ASSIGN DEVICE AT BUILD / NOT CONFIGURED |
| P6 | LT-CIP | CIP/Process Level | IFM LR2050 candidate | `ai_CIPLevel` | Missing | IO-Link | ASSIGN DEVICE AT BUILD / NOT CONFIGURED |
| P7 | TT-PROCESS | Process Temperature | IFM TA2405 candidate | `ai_ProcessTemperature` | Missing | IO-Link | ASSIGN DEVICE AT BUILD / NOT CONFIGURED |
| P8 | SPARE-A102-08 | Future process instrument | — | — | — | Spare | SPARE |

The IFM PN7094, LR2050, and TA2405 references are candidates only. Installed models, measuring ranges, IODDs, units, scaling, filtering, process-data sizes, and HMI bindings require confirmation.

### AL103 — Reserved additional machine sensors

| Port | Device ID | Function | PLC logical tag | Mode | Project status |
|---|---|---|---|---|---|
| P1 | SEN-103-01 | Additional machine sensor 1 | `io_AL103_P1` | IO-Link/SIO | RESERVED / NOT INSTALLED |
| P2 | SEN-103-02 | Additional machine sensor 2 | `io_AL103_P2` | IO-Link/SIO | RESERVED / NOT INSTALLED |
| P3 | SEN-103-03 | Additional machine sensor 3 | `io_AL103_P3` | IO-Link/SIO | RESERVED / NOT INSTALLED |
| P4 | SEN-103-04 | Additional machine sensor 4 | `io_AL103_P4` | IO-Link/SIO | RESERVED / NOT INSTALLED |
| P5 | SPARE-A103-05 | Future smart sensor | — | Spare | SPARE |
| P6 | SPARE-A103-06 | Future smart sensor | — | Spare | SPARE |
| P7 | SPARE-A103-07 | Future smart sensor | — | Spare | SPARE |
| P8 | SPARE-A103-08 | Future smart sensor | — | Spare | SPARE |

No installed devices are identified for AL103. The current `Required := TRUE` setting must be confirmed because this master is described as reserved.

### AL104 — Provisional Customer CIP interface

| Port | Device ID | Function | PLC logical tag | HMI tag | Mode | Project status |
|---|---|---|---|---|---|---|
| P1 | CIP-DO-01 | Cold Water Request | `CIP_REQ_COLD_WATER` | `CIP_Req_Cold_Water` | Digital output | FROZEN FUNCTION / NOT CONFIGURED |
| P2 | CIP-DO-02 | Hot Water Request | `CIP_REQ_HOT_WATER` | `CIP_Req_Hot_Water` | Digital output | FROZEN FUNCTION / NOT CONFIGURED |
| P3 | CIP-DO-03 | Acid/Chemical Request | `CIP_REQ_ACID` | `CIP_Req_Acid` | Digital output | FROZEN FUNCTION / NOT CONFIGURED |
| P4 | CIP-DO-04 | Citra/Neutralizer Request | `CIP_REQ_CITRA` | `CIP_Req_Citra` | Digital output | FROZEN FUNCTION / NOT CONFIGURED |
| P5 | CIP-DO-05 | Discharge Request | `CIP_REQ_DISCHARGE` | `CIP_Req_Discharge` | Digital output | FROZEN FUNCTION / NOT CONFIGURED |
| P6 | PROD-DO-01 | Production/Product Request | `PRODUCTION_REQ_PRODUCT` | `Production_Req_Product` | Digital output | FROZEN FUNCTION / NOT CONFIGURED |
| P7 | CIP-DI-01 | Customer CIP Ready/Accepted | `CIP_REMOTE_READY` | `CIP_Remote_Ready` | Digital input | RESERVED FEEDBACK / NOT CONFIGURED |
| P8 | CIP-DI-02 | Customer CIP Fault/Busy | `CIP_REMOTE_FAULT` | `CIP_Remote_Fault` | Digital input | RESERVED FEEDBACK / NOT CONFIGURED |

AL104 is a logical fifth master/interface and is currently optional. The physical implementation, AL1403 requirement, port/channel/address mapping, and final Customer CIP handshake are not confirmed.

The PLC also preserves the following Customer CIP feedback functions, but they are not allocated to a confirmed physical AL104 port in the current mapping:

- `CIP_REMOTE_BUSY`;
- `CIP_REMOTE_REQUEST_ACCEPTED`;
- `CIP_MEDIUM_AVAILABLE`;
- `CIP_DISCHARGE_COMPLETE`.

## 5. Items that are not confirmed as IO-Link devices

The project contains references whose final interface remains undecided:

| Device | Function | Current status |
|---|---|---|
| TLS100 | Filler tank level sensor | IO-Link or 4–20 mA — TBC |
| VS100 | Vacuum process value | AI or IO-Link — unverified |
| DI-031 to DI-034 | Bottle/accumulation sensors | SIO or IO-Link; final sensor model missing |
| DI-040, DI-060 to DI-062 | Cap-system sensors | SIO or IO-Link; final sensor model missing |
| DI-010 | Machine air pressure | Digital input or IO-Link if supported |

These references must not be counted as installed IO-Link devices until the electrical design, device model, IODD, and physical port are approved.

## 6. Information still required for commissioning

For every master and occupied port, provide:

- installed master Order Number and firmware;
- GSDML and TIA device configuration;
- PROFINET device name and approved IP address;
- input/output start addresses and process-data lengths;
- installed sensor/device Order Number;
- IODD and revision;
- physical port number;
- port mode: IO-Link, DI/SIO, DO, or disabled;
- process-data layout and byte order;
- engineering unit, range, scaling, and filtering;
- polarity and safe state;
- `Required` decision;
- diagnostic and quality bits;
- electrical device code and drawing reference;
- end-to-end PLC/HMI binding and test evidence.

## 7. Source references

- `REV12/Documentation/REV20_AL1403_Master_Port_Allocation.csv` — controlled logical port allocation.
- `REV12/Documentation/REV12_IO_Master_Port_Mapping.csv` — earlier master/device mapping and open points.
- `REV12/PLC_Sources/01_UDT_Types.scl` — IO-Link master and port UDTs.
- `REV12/PLC_Sources/02_DB_Global.scl` — AL100–AL104 instances and current Required defaults.
- `REV12/PLC_Sources/11_FB_Application.scl` — symbolic master communication integration.
- `runs/open-points-integral-20260828/final-compile-inventory.txt` — latest hardware inventory showing no configured AL1403 device.

## 8. Release condition

This list can be changed from logical/provisional to commissioned only after the real devices and port mappings are present in TIA hardware, the PLC and HMI bindings are complete, the project compiles without errors, and each port is validated against the approved electrical documentation and physical development/test system.
