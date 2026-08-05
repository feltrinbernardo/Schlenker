REV10 - TIA Portal Import Package
Project: Refurbished Monoblocco Schenker

IMPORTANT:
- This is NOT a compiled TIA Portal .ap project.
- It is a structured SCL/CSV import package for Siemens TIA Portal.
- It must be imported, assigned to the real hardware, compiled, tested in PLCSIM,
  and commissioned by a qualified engineer.
- Do NOT download to the real machine before safety validation, I/O checks,
  drive commissioning and FAT.

Changes since REV09:
- Fieldbus gap resolved: PROFINET-to-EtherCAT gateway confirmed as HMS Anybus
  ABC3113-A, sitting between the S7-1512C-1 PN and the SMC EX260-SEC1
  (8x VQC1100N-51, 2x VQC1200N-51). See FB_NetworkDiagnostics for gateway
  station/link diagnostic bits.
- Added FB_NetworkDiagnostics (step 2 of the execution sequence) aggregating
  PROFINET device diagnostics for Pilz, IO-Link masters, drive, HMI and the
  Anybus gateway, feeding an alarm/diagnostics bitmask for the HMI.
- Added FB_ManualValveOverride: generic manual test override block used by
  the HMI subsystem menus (Filler, Capper, ...) so an engineer can directly
  command a single pneumatic point (e.g. Open Gate / Close Gate) to verify
  cylinder and valve response during troubleshooting. Control automatically
  reverts to the normal automatic sequence output the instant the operator
  leaves the subsystem menu page - a component left open during a test is
  never left in a manual state.
- OB1 call structure updated to the confirmed 19-step order.
- Tag mapping extended with gateway diagnostics, menu-active flags, and
  manual test pushbuttons.

Still open (unchanged from REV09 unless noted above):
- Final HMI hardware model and TIA/WinCC version.
- Final safety validation and Pilz project (Pilz remains the authoritative
  safety layer; all interlocks in this package are functional/process
  interlocks that read safety status, they do not replace it).
- Final encoder mechanical ratio and mounting.
- Final valve-to-bit mapping confirmation after FAT.

Included:
- PLC data types
- Machine state manager
- Product pump level control
- Bottle shortage/accumulation logic
- Height mode interlock for filler and capper
- Forward-only JOG pendant logic
- Vacuum/CIP logic
- SMC valve mapping abstraction
- Network/device diagnostics
- Manual valve test override (HMI menu) with auto-return-to-normal behavior
- Alarm framework
- Tag list

Note: This package covers the Schenker monoblocco (36 filling valves,
3 cappers) only. The separate FBS fruit juice monoblock rebuild is a
different machine with its own configuration and is not included here.
