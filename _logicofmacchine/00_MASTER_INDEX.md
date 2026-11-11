# SCHLENKER — PLC/HMI Functional Specification
Date: 2026-11-10 | Status: Working specification for Rob — NOT commissioned

## Files
- [01 Production](01_PRODUCTION_LOGIC.md)
- [02 Bottle tracking and capper](02_BOTTLE_TRACKING_CAPPER.md)
- [03 CIP](03_CIP_SEQUENCE.md)
- [04 Safety and alarms](04_SAFETY_ALARMS.md)
- [05 I/O mapping and open points](05_IO_MAPPING_OPEN_POINTS.md)

## Rules
Production, runout, controlled accumulation standby, CIP and fault states are distinct.
A requested operational stop is not an emergency stop.
All outputs, timers, phases, sensor polarities and physical valve routes require onsite verification before energizing.
No code deployment or message to Rob is implied by these documents.
