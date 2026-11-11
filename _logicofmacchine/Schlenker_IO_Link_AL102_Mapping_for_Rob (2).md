# SCHLENKER — IO-Link AL102 (Node 102) — Mapping for Rob

**Date:** 10 November 2026\
**Scope:** Schlenker refurbishment — PLC / TIA Portal / HMI Unified. Do not mix with the new Bernardo monobloc project.

## Port assignments (user-confirmed functional mapping)

| Port | Function | Required PLC / HMI behavior |
|---|---|---|
| X1 | Product Presence | Product presence input; use in product pump/CIP permissives according to validated logic. |
| X2 | Product Temperature | Product temperature measurement; verify sensor type, units, scaling and IO-Link process data. |
| X3 | Vacuum Sensor | Vacuum monitoring; verify sensor type, threshold and alarms. |
| X4 | Distributor Anti-Rotation | Must remain ON during machine operation; if OFF, request immediate machine stop according to the validated safety design. |
| X5 | Tank Level | Tank level measurement; confirm scaling and production/runout thresholds. |
| X6 | Filler Zero Position — Valve #1 | Machine zero/reference sensor for positioning filler valve no. 1, including CIP positioning. |
| X7 | CIP Presence / Engaged | Confirms CIP tube inserted/engaged; when engaged, inhibit all machine rotation. Confirm signal polarity and safety implementation. |
| X8 | SPARE — Not Connected | Leave unassigned; no assumed function. |

## Implementation notes for Rob

- Preserve X1–X7 assignments and leave X8 spare. Do not remap ports without checking physical wiring and the agreed functional specification.
- This is a **functional mapping**, not proof of actual PLC addresses, IO-Link process-data bytes, device IDs or physical connections. Verify those in TIA Portal and on the installed hardware.
- X4 machine stop and X7 rotation inhibit require validation against the machine safety architecture. Do not rely on an ordinary IO-Link input as a safety-rated channel without an appropriate safety design.
- Encoder reference is **hardwired**, not connected through IO-Link; do not confuse it with the X6 machine-zero sensor.
- Confirm the installed master model, port configuration, IO-Link device types, signal polarity, diagnostics, PLC tags and HMI display before commissioning.

**Status:** Functional port assignments recorded; hardware/TIA verification pending.
