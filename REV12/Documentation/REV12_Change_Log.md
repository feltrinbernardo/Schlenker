# REV12 Change Log

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
