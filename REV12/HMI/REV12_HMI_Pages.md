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

## Required behavior
- Selecting Filler Height ON blocks all rotation and permits filler UP/DOWN only.
- Selecting Capper Height ON blocks all rotation and permits capper UP/DOWN only.
- Height modes are mutually exclusive.
- JOG is forward only and is disabled in either height mode.
- Speed is entered only on HMI in bottles/hour.
- Operator may load recipes but protected users save/edit recipes.
- Leaving a manual valve page returns control to the automatic command path in the same PLC scan.
- Door access never causes automatic restart; a separate physical Start command is required.
