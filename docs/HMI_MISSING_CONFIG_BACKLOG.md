# HMI Missing Configuration Backlog

## SMC final station numbering — 2026-11-10 source update

Operator confirmed stations 125Y1–125Y11 only. Y8 is Cap Distributor Air,
Y9 Infeed Gate Open, Y10 Cap Opening and Y11 External Washing. Y9/Y10 are
single-coil spring-return functions. The inherited REV03 bit allocation is
not a verified match to these physical station identities. Preserve the
dual-loading double-solenoid arrangement pending coil evidence.

MISSING CONFIG remains open: installed coil A/B order, gateway EtherCAT PDO
mapping, byte order, physical output assignments, communication-loss behavior
and the Y10 command sequence are unresolved. No configuration-complete flag
has been forced. This indicator is not itself an output interlock. Source
corrections have now been applied to a recovered native work copy: PLC software
rebuild 0/0, PLC hardware rebuild 0/8, WinCC Unified rebuild 0/0. Y8/Y9/Y11
test-page buttons remain disabled and Y10 has no assigned operational command.
The new repository SMC125 metadata members remain source-only. See
[native recovery report](TIA_RECOVERY_SMC_NATIVE_2026-11-10.md).
See [mapping and validation report](../REV12/Documentation/SMC_FINAL_NUMBERING_2026-11-10.md).

This backlog records HMI items that intentionally remain marked as
`MISSING CONFIG`. An entry identifies an incomplete or unverified engineering
integration; it does not by itself mean that the PLC logic is faulty.

No item in this file authorizes creating PLC logic, HMI tags, hardware
addresses, IO-Link assignments, scaling, or process semantics without approved
engineering information.

## IO-Link AL102 — confirmed functions, hardware commissioning open

- Screen: `io_link_al102`
- Master: `AL102 | IFM AL1403 MASTER 3`
- Status: X1-X8 functional allocation approved 2026-11-10; physical commissioning open
- HMI presentation: normalized read-only values with separate validity and an
  explicit hardware/safety hold indication
- Runtime expectation: Going online alone will not resolve these entries. The
  HMI continues to show `NO DATA` while configuration or quality is invalid.

| Port | Function | Logical PLC reference | HMI tag | Pending engineering confirmation |
| --- | --- | --- | --- | --- |
| X1 | Product Presence | `DB_Global.AL102Process.X1ProductPresence` | `AL102_X1_Product_Presence` | Address, device, polarity and end-to-end test |
| X2 | Product Temperature | `DB_Global.AL102Process.X2ProductTemperature` | `AL102_X2_Product_Temperature` | Address, device, scaling and unit |
| X3 | Vacuum Sensor | `DB_Global.AL102Process.X3Vacuum` | `AL102_X3_Vacuum` | Address, device, scaling and unit |
| X4 | Distributor Anti-Rotation | `DB_Global.AL102Process.X4AntiRotationHealthy` | `AL102_X4_AntiRotation_Healthy` | Address, polarity, diagnostic state and stop test |
| X5 | Tank Level | `DB_Global.AL102Process.X5TankLevel` | `AL102_X5_Tank_Level` | Address, device, scaling and unit |
| X6 | Filler Zero Position – Valve #1; IFM IM5135 (operator/photo confirmed 2026-11-12) | `DB_Global.AL102Process.X6Valve1Reference` | `AL102_X6_Valve1_Reference` | Address, selected PNP output/polarity, native SIO port configuration and positioning proof; EF1MM sharing not validated |
| X7 | CIP Presence / Engaged | `DB_Global.AL102Process.X7CIPPresenceEngaged` | `AL102_X7_CIP_Presence` | Address, polarity and independent safety validation |
| X8 | Spare | — | validity only | Confirm unused |

### Completion criteria

An AL102 entry may be closed only after:

1. the installed instrument and physical IO-Link port/channel are confirmed;
2. polarity, datatype, engineering range, scaling and unit are approved;
3. the corresponding HMI tag and read-only screen binding are configured;
4. communication quality and bad/offline behavior are validated;
5. the HMI displays the real value without substituting static or simulated data;
6. X7 is validated as an independent safety interlock preventing rotation;
7. the screen is saved, compiled where resource conditions allow, and verified
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

## SMC 125Y1–125Y7 — logical positions complete, physical mapping open

- Screen: `smc_diagnostics`
- Status: Open
- HMI presentation: read-only pneumatic target states and an explicit physical
  mapping hold point
- Runtime expectation: Going online may update the logical PLC command tags,
  but it does not prove that the physical SMC manifold bit/channel actuates the
  intended valve or gate.

| Station | Production pneumatic target | HMI indication | Pending confirmation |
| --- | --- | --- | --- |
| `125Y1` | OPEN | `SMC_125Y1_Desired_Open` | Anybus bit, coil polarity and actuation proof |
| `125Y2` | OPEN | `SMC_125Y2_Desired_Open` | Anybus bit, coil polarity and actuation proof |
| `125Y3` | CLOSED | `SMC_125Y3_Desired_Open` | Anybus bit, coil polarity and actuation proof |
| `125Y4` | CLOSED | `SMC_125Y4_Desired_Open` | Anybus bit, coil polarity and actuation proof |
| `125Y5` | CLOSED | `SMC_125Y5_Desired_Open` | Anybus bit, coil polarity and actuation proof |
| `125Y6` | OPEN; close only after valid pump-stopped feedback on level pause | `SMC_125Y6_Desired_Open` / `SMC_125Y6_Held_For_Pump_Stop` | Anybus bit, coil polarity and end-to-end stop sequence |
| `125Y7` | CLOSED | `SMC_125Y7_Desired_Open` | Anybus bit, coil polarity and actuation proof |

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
