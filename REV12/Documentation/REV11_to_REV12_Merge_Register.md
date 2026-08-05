# REV11 to REV12 Cumulative Merge Register

## Merge rule

REV11 working is the functional baseline. REV12 adds corrections and confirmed
machine behavior without silently deleting REV11 requirements. A requirement
that is not yet implemented is retained as an open point and must not be marked
complete until it is generated and compiled in TIA Portal V19.

## Retained and upgraded PLC source

| REV11 function | REV12 disposition |
|---|---|
| Machine state manager | Retained and upgraded for guarded access and standby |
| Product pump | Retained and corrected for level behavior and ramping |
| Accumulation gate | Retained with parameterized delays and standby request |
| Filler/capper height | Retained; mutually exclusive and rotation interlocked |
| Forward-only JOG pendant | Retained with tracking invalidation |
| Vacuum/CIP | Retained with deterministic timer execution |
| SMC valve manager | Retained and upgraded with safety, communication and conflict gating |
| Manual valve override | Retained; manual commands revert to automatic in the same scan |
| Network diagnostics | Restored in REV12 as `10D_FB_NetworkDiagnostics.scl` |
| Door access | Retained and integrated with 11-door aggregate status and three request stations |
| Lights and air filter | Retained and integrated as independent 24 VDC relay commands |
| OB1 call structure | Upgraded to a real `FB_Application` instance call |
| Startup behavior | Added explicit safe output initialization |

## Retained confirmed hardware decisions

- PLC: Siemens S7-1512C-1 PN.
- HMI: MTP1500 Unified Comfort, 6AV2128-3QB06-0AXX, WinCC Unified V19.
- Drives: four SINAMICS G120C units remain required.
- Safety: Pilz PNOZmulti 2 with PROFINET module 772138 remains required.
- IO-Link: four IFM AL1403 masters remain required.
- Encoder: IFM RO3101 HTL A/B/Z 2048 PPR through TM Count remains required.
- Pneumatics: HMS Anybus ABC3113-A to SMC EX260-SEC1 with eight VQC1100N-51
  and two VQC1200N-51 valves remains the confirmed gateway/manifold basis.
- EV230 remains outside the valve island and requires deterministic fast-output
  integration through the validated TM Count design.

## Retained machine rules

- Mechanical and JOG rotation are forward only.
- Filler and capper height modes are mutually exclusive and block rotation.
- Speed setpoint is HMI-only; no speed potentiometer is used.
- Continuous tank level is 0 to 100 percent.
- Design speed is 12,000 bottles/hour.
- SAT target remains 11,040 bottles/hour for eight consecutive hours.
- Four emergency stops are represented by one series-chain alarm; individual
  HMI identification is not required.
- Door access never restarts automatically. The operator must restore the
  auxiliary and alarm circuits and press the separate physical Start button.

## REV11 master-brief functions retained as open work

The following requirements were named by REV11 but do not yet have complete,
compile-validated implementation in this source release:

- Safety interface and final Pilz I/O mapping.
- Encoder manager, homing manager, bottle tracking and tracking queue.
- Bottle gate sequence beyond the current accumulation-gate scope.
- Main, conveyor and cap-distributor drive managers and G120C telegrams.
- Filling manager, cap feed, pick-and-place, EV230 fast release and capper manager.
- Gateway cyclic I/O mapping and final SMC valve-to-bit assignment.
- Emptying manager beyond the current state and product-pump enable path.
- Recipe manager/storage, maintenance counters, simulation manager and protected FAT mode.
- OB82, OB86 and OB121 handling where required by the final hardware design.
- Native WinCC Unified screens, recipes, users, trends, bilingual text resources
  and compiled HMI project.
- Final hardware configuration, PROFINET network, Startdrive, TM Count and device catalog validation.
- PLC/HMI compile reports, FAT/SAT evidence and commissioning documentation.

These are cumulative REV12 open points, not exclusions.
