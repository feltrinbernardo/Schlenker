REV11 WORKING - TIA Portal Import Package
Project: Refurbished Monoblocco Schenker

REV11 working additions:
- HMI hardware is confirmed as MTP1500 Unified Comfort, Siemens order number
  6AV2128-3QB06-0AXX, engineered with WinCC Unified in TIA Portal V19.
- Added FB_DoorAccess and UDT_DoorAccess for the operator Request Open Door
  sequence: controlled stop, standstill, required three-phase power-off
  feedback, request to the validated safety system to unlock all doors,
  reclose, auxiliary reset, alarm reset, closed safety circuit and separate
  restart permission.
- Added HMI door-access command/status tag definitions.
- Confirmed 11 guarded doors, four series-wired emergency stops, and three physical
  Request Open Door stations with blue lamps (panel, front and rear).
- The latest panel photograph confirms seven physical devices below the HMI,
  left to right: AUX. VOLTAGE illuminated pushbutton, keyed MAN/AUT selector,
  OPEN/CLOSE DOORS illuminated pushbutton, blue RESET, green START, illuminated
  red STOP and the emergency stop. See OPERATOR_PANEL_REFERENCE.md.
- The working aliases retain AUX. VOLTAGE as the auxiliary reset/enable step and
  RESET as alarm reset. Their electrical contact functions and the MAN/AUT
  selector's authority relative to the HMI mode commands require confirmation.
- Added Main-page ON/OFF controls for the machine lights and air filter. The
  air filter uses the existing 24 VDC hood/filter pressure input for proof and
  a parameterized delayed alarm. Physical output addresses remain open.
- Machine lighting is confirmed as a 24 VDC load. The final PLC output or
  interposing-relay address remains open pending the electrical drawing.
- Lights and air filter use separate 24 VDC PLC digital outputs to energize
  dedicated relay coils. The relay contacts power the respective load circuits.
- Added a separate 24 VDC filter-change input and a non-stopping bilingual HMI
  warning when the filter needs replacement.
- Added MECHANICAL_LAYOUT_REFERENCE.md. The operator-supplied assembly image may
  be used as the visual basis for HMI overview/diagnostics layouts only. Its
  numbered panel callouts are not mapped to guard tags until field and drawing
  identifiers are cross-checked.
- Added HMI_LAYOUT_STYLE_REFERENCE.md from operator-supplied example screens.
  These photographs are from another machine and define preferred visual
  organisation only; none of their tags, values, parameters or logic are copied.
- Any emergency-stop operation removes the confirmed 24 VDC series return to
  the Pilz safety PLC. The HMI provides one aggregate `Emergency stop pressed`
  alarm; individual E-stop identification is not required.
- Added GuardedAccess to the machine state manager.
- Manual valve override now requires both machine Manual mode and the relevant
  HMI page to be active. Leaving either returns outputs to the automatic path
  in the same PLC scan.

SAFETY BOUNDARY:
- FB_DoorAccess is standard-PLC coordination, not safety logic.
- UnlockAllDoorsRequest is only a request to the validated Pilz safety system;
  it must never be wired as a direct guard-lock output.
- No automatic restart is permitted.

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
- Final safety validation and Pilz project (Pilz remains the authoritative
  safety layer; all interlocks in this package are functional/process
  interlocks that read safety status, they do not replace it).
- Final encoder mechanical ratio and mounting.
- Final valve-to-bit mapping confirmation after FAT.
- Final MAN/AUT selector contact arrangement, polarity and authority relative
  to the HMI Auto/Manual commands.
- Controlled CAD/PDF and approved mapping between the supplied layout callouts
  and the 11 guarded-door identities.

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
- HMI visual-style and screen-pattern guidance

Note: This package covers the Schenker monoblocco (36 filling valves,
3 cappers) only. The separate FBS fruit juice monoblock rebuild is a
different machine with its own configuration and is not included here.
