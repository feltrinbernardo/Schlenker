# Schenker Monobloc REV12 - Cumulative REV11 Merge Release

## Target
- Siemens TIA Portal V19 (source files are also intended to be portable to V17 after device/version review)
- PLC: S7-1512C-1 PN
- HMI: MTP1500 Unified Comfort, 6AV2128-3QB06-0AXX
- Main language: SCL
- HMI languages: English / Italian

## Purpose
REV12 is a cumulative upgrade of REV11. It does not exclude or supersede a
REV11 function unless a REV12 entry explicitly identifies a safe correction.
The REV11 working source package is the functional baseline, and the REV12
changes are merged into that baseline.

The cumulative package includes:

1. A real cyclic application call is provided.
2. Function blocks are instantiated as multi-instances in `FB_Application`.
3. Pneumatic outputs are gated by SafetyOK, mode permissives and communication health.
4. CIP timer execution and reset are deterministic.
5. Product-pump behavior above setpoint is corrected.
6. Tracking invalidation is latched until recovery/homing.
7. Bottle-shortage and accumulation timers are parameterized.
8. Drive and network status are separated by device.
9. Alarm IDs and bilingual texts are included.
10. HMI tag and page specifications are included.
11. The confirmed 11-door access sequence and three request stations are included.
12. Manual SMC gate control returns to automatic control on page or mode exit.
13. Main-page machine-light and air-filter relay controls are included.
14. The aggregate four-E-stop alarm is separated from other safety conditions.
15. REV11 network diagnostics and the bit-per-device fault mask are retained.
16. REV11 hardware, I/O, guarded-door and access-station decisions are retained
    in the merge register and supporting CSV files.

See `Documentation/REV11_to_REV12_Merge_Register.md` for the requirement-level
merge status. Items from the REV11 master brief that do not yet have compiled
implementation remain explicit open points; they are not treated as removed.

## Important engineering limitation
This is a professional TIA source/import release, not a native `.ap19` archive.
The PLC software rebuild passed in Siemens TIA Portal V19 with zero errors and
zero warnings on 2026-08-06. The empty HMI baseline also compiled with zero
errors and zero warnings, but it contained zero tags. PLC-HMI connection,
operational HMI configuration and PLCSIM/FAT validation remain pending. The
TIA engineer must:

- import the source files in the numbered order;
- generate PLC data types first;
- generate FB/DB/OB blocks;
- assign the final hardware addresses;
- connect the real TM Count technology object;
- configure all G120C telegrams;
- configure the Pilz, IO-Link masters and SMC gateway;
- compile the PLC hardware configuration and HMI;
- close the two deferred hardware-security warnings by configuring the PLC
  access-protection level and CPU-display password protection;
- correct any catalog/version-specific syntax;
- validate in PLCSIM and FAT before downloading to the machine.

The door-access block is standard-PLC coordination only. It must never replace
the validated Pilz safety logic or directly energize guard-lock hardware.

Do not download to real machinery until the safety validation, I/O test and FAT are complete.
