# REV12 HMI Page Structure

1. Login
2. Home / Machine Overview
3. Production
4. Filler
   - Automatic
   - Manual
   - Settings
   - Height
5. Capper
   - Automatic
   - Manual
   - Settings
   - Height
6. Bottle Conveyor
7. Product Pump and Tank Level
8. Vacuum / CIP
9. Forward JOG
10. Recipes
11. Active Alarms
12. Alarm History
13. Maintenance
14. Diagnostics
   - PLC I/O
   - Encoder / Homing
   - Drives
   - PROFINET
   - IO-Link
   - Pilz status
   - SMC / Gateway
15. Service / Manufacturer
16. Door Access
   - Implemented as the dedicated `Safety & Door Access` Unified screen
   - Request status from panel / front / rear stations
   - Controlled stop and three-phase-off confirmation
   - All 11 doors closed / unlocked status
   - Auxiliary reset, alarm reset and restart-permitted indication
17. Main-page Auxiliaries
   - Machine lights ON / OFF and relay-command status
   - Air filter ON / OFF, pressure proof and replacement warning
18. Manual Gate
   - Gate Open (-125Y1) and Gate Close momentary commands
   - Controls are active only in Manual with engineer permission and this page visible
19. REV12.1 Home Process Commands
   - External bottle wash valve status
   - Gate OFF / Gate ON maintained automatic admission request
   - Run Out Product momentary start with Active / Completed indication
   - Product Pump OFF / Product Pump ON maintained gravity-feed selection
20. REV12.2 Dedicated CIP
   - CIP Start / Stop / Reset and medium selection
   - Sequence step and elapsed HH:MM:SS display
   - Product/vacuum pump, valves 210/217/213/212/247 and media presence
   - Tank level, vacuum value, forced-closed gate and alarm status
21. REV12.2 Production
   - Implemented as the dedicated Unified screen named `production`; it is separate from the existing `operate` screen
   - Explicit Production ON/OFF and Run Out Product command
   - Product presence, pump selection/status and vacuum actual/minimum/alarm
   - Bottle wash, bottle gate and logical status of valves 210/217/213/212/247
22. REV12.3 UI Layout Refinement
   - Home, Safety, Operate, Production, CIP, Function, Alarms, Recipe, Setup, Manual, Efficiency and Diagnostics use the shared header, status bar, navigation rail, footer and content grid
   - OPERATE is included in the shared navigation rail on all 12 screens, between Safety and Production
   - Infeed, Filler, Capper, Outfeed and Filter Service use the same 12-pixel rounded treatment while retaining the existing palette and typography
   - Diagnostics provides read-only network, safety/access and process-input status using existing HMI tags
   - Obsolete overlapping legacy objects are removed from Home and Production
   - Final TIA audit: 144 navigation, 33 command, 231 field and 24808 boundary/overlap checks passed with zero failures
23. 2026-08-07 Approved HMI Design Update
   - Main Auxiliaries follows the Bernardo reference arrangement: separate Lights and Air Filter rows, relay/proof fields, and a right-side Filter Service card
   - Home contains Machine Overview, speed/tank information, Main Auxiliary, process-device commands/status and navigation only
   - Safety and door-access objects were deleted from Home and placed on the dedicated Safety page
   - The enlarged Home Process Commands panel contains Product Pump, Bottle Gate, automatic Vacuum Pump and External Wash status, plus reserved future-actuator positions
   - All 12 screens passed expanded foreground-to-foreground and interactive-object overlap checks
24. REV12 SET03 Settings and Cable Integration
   - `settings_inputs`: read-only live PLC digital-input diagnostics with engineering device/cable codes
   - `settings_outputs`: read-only live PLC digital-output diagnostics; no new manual forcing path
   - `settings_timers`: authorised timing values with displayed units/ranges and PLC-side validation
   - `settings_external_wash`: read-only bottle-wash command, activity and timing diagnostics
   - `lift_warning`: explicit filler/capper confirmation workflow; PLC retains movement authority
   - `production`: read-only product/filler/vacuum mimic with selectable diagnostics for M102, M103 and EV210/217/213/212/247
   - Final SET03 audit: 144 navigation, 33 command, 218 field, 25,185 layout and 530 wording checks; zero failures

## Required behavior
- Selecting Filler Height ON blocks all rotation and permits filler UP/DOWN only.
- Selecting Capper Height ON blocks all rotation and permits capper UP/DOWN only.
- Height modes are mutually exclusive.
- JOG is forward only and is disabled in either height mode.
- Speed is entered only on HMI in bottles/hour.
- Operator may load recipes but protected users save/edit recipes.
- Leaving a manual valve page returns control to the automatic command path in the same PLC scan.
- Door access never causes automatic restart; a separate physical Start command is required.
- All visible HMI objects remain inside the 1366 x 768 screen boundary and interactive button hit areas must not overlap.
