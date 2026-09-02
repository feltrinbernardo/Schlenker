# HMI Missing Configuration Backlog

This backlog records HMI items that intentionally remain marked as
`MISSING CONFIG`. An entry identifies an incomplete or unverified engineering
integration; it does not by itself mean that the PLC logic is faulty.

No item in this file authorizes creating PLC logic, HMI tags, hardware
addresses, IO-Link assignments, scaling, or process semantics without approved
engineering information.

## IO-Link AL102 — process instrumentation / product system

- Screen: `io_link_al102`
- Master: `AL102 | IFM AL1403 MASTER 3`
- Status: Open
- HMI presentation: `NO DATA` for the live value and `MISSING CONFIG` for the
  engineering integration state
- Runtime expectation: Going online alone will not resolve these entries. The
  HMI will continue to show `NO DATA` until authoritative tags and mappings are
  configured and validated.

| Port | Device ID | Intended measurement | Unit | Logical PLC reference | Current HMI assignment | Pending engineering confirmation |
| --- | --- | --- | --- | --- | --- | --- |
| P1 | `PT-PRODUCT` | Product pressure | bar | `ai_ProductPressure` | Not assigned | Physical IO-Link port/channel/address, PLC tag, HMI tag/binding, range and scaling |
| P2 | `PT-AIR` | Air/process pressure | bar | `ai_AirPressure` | Not assigned | Physical IO-Link port/channel/address, PLC tag, HMI tag/binding, range and scaling |
| P3 | `PT-CIP` | CIP/service pressure | bar | `ai_CIPPressure` | Not assigned | Physical IO-Link port/channel/address, PLC tag, HMI tag/binding, range and scaling |
| P5 | `LT-PRODUCT` | Product continuous level | % | `ai_ProductLevel` | Not assigned | Physical IO-Link port/channel/address, PLC tag, HMI tag/binding, range and scaling |
| P6 | `LT-CIP` | CIP/process level | % | `ai_CIPLevel` | Not assigned | Physical IO-Link port/channel/address, PLC tag, HMI tag/binding, range and scaling |
| P7 | `TT-PROCESS` | Process temperature | degC | `ai_ProcessTemperature` | Not assigned | Physical IO-Link port/channel/address, PLC tag, HMI tag/binding, range and scaling |

P4 (`PT-SPARE`) and P8 (`SPARE-A102-08`) are intentional spare ports and are
not backlog defects. They remain `NOT USED / SPARE` until an approved hardware
change assigns them.

### Completion criteria

An AL102 entry may be closed only after:

1. the installed instrument and physical IO-Link port/channel are confirmed;
2. the PLC source tag, datatype, engineering range, scaling and unit are
   approved;
3. the corresponding HMI tag and read-only screen binding are configured;
4. communication quality and bad/offline behavior are validated;
5. the HMI displays the real value without substituting static or simulated
   data;
6. the screen is saved, compiled where resource conditions allow, and verified
   without overlap or duplicate legacy objects.

## IO-Link AL104 — customer CIP interface

- Screen: `io_link_al104`
- Master: `AL104 | IFM AL1403 MASTER 5`
- Status: Open
- HMI presentation: communication-gated symbolic process states and
  `MISSING CONFIG` for the unproven physical assignments on P1-P6
- Runtime expectation: The existing PLC/HMI tags are preserved and may show
  their logical states when `Network_AL104_OK` is active. This does not prove
  that a physical AL104 port, channel or address has been commissioned.

| Port | Device ID | Function | PLC reference | Existing HMI tag | Pending engineering confirmation |
| --- | --- | --- | --- | --- | --- |
| P1 | `CIP-DO-01` | Cold water request | `CIP_REQ_COLD_WATER` | `CIP_Req_Cold_Water` | Physical AL104 port/channel/address, output assignment and end-to-end validation |
| P2 | `CIP-DO-02` | Hot water request | `CIP_REQ_HOT_WATER` | `CIP_Req_Hot_Water` | Physical AL104 port/channel/address, output assignment and end-to-end validation |
| P3 | `CIP-DO-03` | Acid/chemical request | `CIP_REQ_ACID` | `CIP_Req_Acid` | Physical AL104 port/channel/address, output assignment and end-to-end validation |
| P4 | `CIP-DO-04` | Citra/neutralizer request | `CIP_REQ_CITRA` | `CIP_Req_Citra` | Physical AL104 port/channel/address, output assignment and end-to-end validation |
| P5 | `CIP-DO-05` | Discharge request | `CIP_REQ_DISCHARGE` | `CIP_Req_Discharge` | Physical AL104 port/channel/address, output assignment and end-to-end validation |
| P6 | `PROD-DO-01` | Production/product request | `PRODUCTION_REQ_PRODUCT` | `Production_Req_Product` | Physical AL104 port/channel/address, output assignment and end-to-end validation |

P7 (`CIP-DI-01`) and P8 (`CIP-DI-02`) retain the existing
`CIP_Remote_Ready` and `CIP_Remote_Fault` bindings, but remain
`RESERVED / NOT COMMISSIONED`. They are not classified as current missing
configuration defects until the customer feedback interface is approved for
commissioning.

### Completion criteria

An AL104 P1-P6 entry may be closed only after:

1. the physical master port, channel and address are confirmed from the TIA
   hardware configuration and approved wiring records;
2. signal direction and customer-interface ownership are confirmed;
3. the existing PLC and HMI references are reconciled with the physical
   assignment without substituting another signal;
4. communication loss produces `NO DATA` and cannot be confused with a valid
   OFF, READY or HEALTHY state;
5. the complete request path is validated offline and, when separately
   authorized, against the approved development hardware;
6. the backlog and screen status are updated after successful verification.

## External bottle wash — physical valve integration

- Screen: `settings_external_wash`
- Status: Open
- HMI presentation: operating conditions are communication-gated symbolic
  states; the editable wash-valve OFF delay remains bound to
  `Par_External_Wash_Off_Delay`; the physical output integration remains
  explicitly marked `MISSING CONFIG`
- Preserved logical indication: `External_Wash_Active`
- Pending engineering confirmation: physical wash-valve device code, cable,
  SMC manifold bit/channel, electrical assignment and end-to-end feedback
- Runtime expectation: Going online may update the preserved logical command
  indication, but it does not prove that the physical valve mapping is correct.
  The screen must retain `MISSING CONFIG` until the hardware assignment is
  verified from approved TIA hardware and wiring records.

### Completion criteria

This entry may be closed only after:

1. the installed valve, cable and SMC manifold channel/bit are identified;
2. the physical assignment is reconciled with the existing PLC/HMI reference
   without inventing or substituting a signal;
3. command, permissive, CIP inhibit and loss-of-communication behavior are
   validated against approved development hardware under separate authority;
4. the HMI wording is updated from `MISSING CONFIG` only after the mapping is
   proven and documented.

## Digital outputs — SMC manifold physical mapping

- Screen: `settings_outputs`
- Status: Open
- HMI presentation: communication-gated logical command states
  (`COMMAND ON`, `COMMAND OFF`, or `NO DATA`) and an explicit
  `MISSING CONFIG` hold point
- Runtime expectation: Going online may update the logical PLC command tags,
  but it does not prove that the physical SMC manifold bit/channel actuates the
  intended valve or gate.

| Engineering code | Function | Existing HMI tag | Pending confirmation |
| --- | --- | --- | --- |
| `EV200-O` | Bottle gate open | `DO_Gate_Open` | Device/cable, SMC manifold bit/channel and end-to-end actuation |
| `EV200-C` | Bottle gate close | `DO_Gate_Close` | Device/cable, SMC manifold bit/channel and end-to-end actuation |
| `EV210` | Product valve 210 | `DO_Valve_210` | Device/cable, SMC manifold bit/channel and end-to-end actuation |
| `EV217` | Product valve 217 | `DO_Valve_217` | Device/cable, SMC manifold bit/channel and end-to-end actuation |
| `EV213` | Product valve 213 | `DO_Valve_213` | Device/cable, SMC manifold bit/channel and end-to-end actuation |
| `EV212` | Vacuum valve 212 | `DO_Valve_212` | Device/cable, SMC manifold bit/channel and end-to-end actuation |
| `EV247` | Discharge valve 247 | `DO_Valve_247` | Device/cable, SMC manifold bit/channel and end-to-end actuation |
| `WASH-TBC` | External bottle wash | `DO_External_Wash` | Device/cable, SMC manifold bit/channel and end-to-end actuation |

### Completion criteria

This entry may be closed only after:

1. each installed output device, cable and SMC manifold bit/channel is
   confirmed from approved TIA hardware and wiring records;
2. the physical assignments are reconciled with the existing PLC/HMI tags
   without inventing or substituting addresses;
3. command direction, interlocks and physical actuation are verified against
   approved development hardware under separate authorization;
4. communication-loss behavior remains distinguishable from a valid
   `COMMAND OFF` state;
5. the screen and this backlog are updated only after successful verification.
