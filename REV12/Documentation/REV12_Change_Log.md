# REV12 Change Log

## REV12.2 production, vacuum and CIP integration - 2026-08-07
- Added explicit Production, CIP and Run Out modes under the existing main
  state-machine authority, including CIP/Production mutual interlocks.
- Added the logical product-circuit valve matrix for 210, 217, 213, 212 and
  247 while leaving unconfirmed physical bits unassigned.
- Added product-presence pump permissives, gravity mode, manual filler pump,
  CIP media-loss hold/alarm and two-attempt production-vacuum protection.
- Replaced the simplified CIP vacuum timer with the parameterized 4 s + 1 s +
  7 s + 10 s sequence and an operator-visible elapsed timer.
- Integrated Run Out as state 95 and retained gate, wash and safety priorities.
- Expanded the HMI contract to 128 tags and 26 alarms; added dedicated CIP and
  updated Home, Production and Manual screens.
- Completed HMI compile with zero errors and zero warnings. No download was
  performed; PLCSIM and physical I/O validation remain pending.

## REV12.1 bottle handling and run-out change - 2026-08-07
- Added the external bottle-washing valve with bottle-count permissive and a
  parameterized 2.5-second normal OFF delay.
- Forced the bottle gate closed during CIP and above every manual/automatic
  gate command.
- Added an automatic gate-request sequence with actual-speed proof, admission
  handshake, close confirmation, timeout, alarm abort and controlled stop.
- Added the automatic Run Out Product sequence and Home Active/Completed status.
- Added maintained Product Pump ON/OFF selection and the gravity-feed minimum
  level start interlock with alarm 1303.
- Extended the HMI import set to 91 tags and 23 Alarm_Word1 alarms.
- Added explicit commissioning interfaces rather than assigning unverified
  physical inputs or an unconfirmed SMC solenoid number.

## Corrected from REV10
- Replaced the comments-only OB1 guide with a real OB1 call.
- Added `FB_Application` and `DB_Application`.
- Added a global parameter/status/command database.
- Added deterministic single-call CIP timer logic.
- Added SafetyOK, communication and output-permissive gating to SMC outputs.
- Corrected product-pump behavior above level setpoint.
- Added ramping for the product-pump reference.
- Added anti-bounce timers for bottle shortage and accumulation.
- Added gate timeout and conflicting-feedback alarm.
- Latched bottle-tracking invalidation after JOG.
- Added separate four-drive readiness/fault inputs.
- Added filler and capper height multi-instance control.
- Added bilingual alarm register and HMI planning files.

## Corrective review - 2026-08-05
- Restored the confirmed 11-door Request Open Door sequence and three request stations.
- Restored the aggregate four-E-stop input and dedicated HMI alarm text.
- Restored manual gate commands, including Gate Open (-125Y1), with same-scan return to automatic control.
- Restored the Main-page lights and air-filter ON/OFF relay commands, pressure proof and filter-change warning.
- Connected the accumulation-gate standby request to the machine state manager.
- Added safe validation for a zero or negative maximum production-speed parameter.
- Replaced the startup output self-assignment with explicit safe initialization.
- Added the new functions to FB_Application and the HMI planning files.

## Cumulative REV11 merge correction - 2026-08-05
- Defined REV11 working as the retained functional baseline for REV12.
- Restored `FB_NetworkDiagnostics` and its HMI fault-mask interface.
- Restored the confirmed HMS Anybus ABC3113-A / SMC EX260 gateway decision.
- Restored the selected MTP1500 Unified Comfort order number 6AV2128-3QB06-0AXX.
- Carried the REV11 tag, guard and physical-station inventories into REV12.
- Added a merge register so incomplete REV11 master-brief items remain visible
  as open work and cannot be mistaken for excluded functionality.

## First TIA V19 rebuild correction - 2026-08-05
- Renamed the `FB_SMC_ValveManager` multi-instance in `FB_Application` from
  `SMC` to `ValveManager`. TIA V19 treated `SMC(...)` as an invalid function
  name, which produced one primary error and 30 cascading parameter errors.
- Added the mandatory CPU-supplied OB interfaces: `LostRetentive` and `LostRTC`
  for Startup OB100, and `Initial_Call` and `Remanence` for Main OB1.
- Added explicit initial values for the product-pump speed reference and the
  vacuum/CIP step output to resolve the five initialization warnings reported
  by the first rebuild.
- Explicitly enabled optimized block access on Startup OB100 and Main OB1. The
  two Boolean start-information inputs are the optimized S7-1500 OB interface;
  without this attribute TIA checked them against the larger legacy interface.
- After the successful zero-error rebuild, moved the product-pump ramp value
  and vacuum/CIP sequence step into initialized internal FB state. Their output
  parameters are now assigned on every scan, eliminating the five TIA
  read-before-write warnings without changing the retained ramp or step logic.

## TIA V19 PLC software compile validation - 2026-08-06
- Removed obsolete duplicate external-source copies and regenerated the
  cumulative REV12 source set in numbered order.
- Completed `Software (rebuild all)` for PLC_1 with zero errors and zero
  warnings.
- PLC hardware configuration, HMI compilation and PLCSIM/FAT remain pending;
  no online connection or hardware download was performed.

## TIA V19 PLC hardware compile checkpoint - 2026-08-06
- Completed the offline PLC hardware rebuild with zero errors.
- The operator deferred two security warnings: no configured PLC protection
  level and no password protection for the CPU display.
- Both warnings remain required commissioning actions before approval for a
  hardware download. HMI compilation is the next offline validation gate.

## TIA V19 HMI baseline compile checkpoint - 2026-08-06
- Compiled the MTP1500 Unified Comfort HMI baseline with zero errors and zero
  warnings after configuring encrypted transfer and the Home start screen.
- The compiler reported zero configured and zero used HMI tags, so this result
  validates only the empty HMI baseline, not the operational REV12 HMI.
- PLC-HMI connection, REV12 tags, alarms, screens and simulation remain pending.

## TIA V19 PLC-HMI connection checkpoint - 2026-08-06
- Connected PLC_1 and HMI_1 to PN/IE_1 and retained the integrated S7-1200/1500
  HMI connection.
- Recompiled the HMI connection baseline with zero errors and zero warnings.
- The compiler still reported zero configured and zero used HMI tags; tag,
  alarm and operational-screen implementation remains pending.

## TIA V19 HMI tag-import workbook - 2026-08-06
- Populated the operator-exported WinCC Unified HMI tag template with all 74
  cumulative REV12 tags.
- Retained the native Siemens column schema, symbolic PLC addresses, data types,
  acquisition defaults and the integrated `HMI_Connection_1` connection.
- Validated the generated workbook structurally and visually. TIA Portal import,
  import-log review and HMI software rebuild remain pending; no download was
  performed.

## TIA V19 HMI tag compile and alarm workbook - 2026-08-06
- The operator imported all 74 REV12 HMI tags and completed an HMI software
  rebuild with zero errors and zero warnings. TIA reported zero used tags,
  which is expected before alarms and operational screens are implemented.
- Generated a native Siemens discrete-alarm import workbook containing the 21
  `Alarm_Word1` alarms on bits 0 through 20.
- Retained the English alarm text, reaction, Italian reference text, Critical
  or Warning class, symbolic trigger tag and rising-edge trigger mode.
- The existing project has no separate Safety alarm class, so the two safety
  indications use the existing Critical class. Alarm import and rebuild remain
  pending; no download was performed.

## TIA V19 HMI machine-state text list - 2026-08-06
- The operator imported all 21 discrete alarms and completed the HMI rebuild
  with zero errors and zero warnings.
- Generated a Siemens-schema text-list workbook that preserves the exported
  system lists and adds one `MachineStateText` decimal list.
- Added the 15 PLC state mappings from 0 (`POWER UP`) through 140 (`RECOVERY`),
  matching `FB_MainState` exactly.
- Text-list import and the following HMI rebuild remain pending; no download was
  performed.

## REV12 SET03 Settings and cable integration - 2026-08-09

- Implemented PLC-controlled filler/capper lift confirmations and parameter-limit validation.
- Added Unified Settings pages for digital inputs, digital outputs, timers/delays and external wash.
- Added a read-only interactive Production process mimic and removed the superseded Production safety layer.
- Added device-code, I/O master/port and master cable schedules while preserving all unconfirmed items as TBC.
- PLC software rebuild passed with 0 errors and 0 warnings.
- HMI Openness verification passed 26,110 checks with 0 failures, including 25,185 layout checks and no Production overlaps.
- Work remained offline; PLCSIM, physical I/O, safety validation and download remain pending.
