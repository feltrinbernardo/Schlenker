# 04 — Safety, faults, alarms
- Safety door interlocks, E-stops and PILZ safety system retain authority. No automatic restart after safety trip.
- Antirotation distributor detection missing: immediate protective stop, `ANTIROTATION DISTRIBUTOR SENSOR FAULT`.
- Filler cylinder presence: check each of 36 cylinders in encoder window indexed from valve #1; missing → stop, `FILLER CYLINDER MISSING`, position if valid.
- Capper cylinder presence: check each of 10 in mechanically phased window; missing → stop, `CAPPER CYLINDER MISSING`.
- Monitoring active during machine motion including homing/standby; define qualified windows and safe stop response.
- Drive overload/torque monitoring for jams; does NOT confirm bottle presence.
- `CAPS MISSING` prevents production and requires cap recovery even while stationary.
- CIP: verify pressure relief, safe fluid routes, CIP Presence, chemical handling, M102 dry-run, M103 interlock and independent protective devices before commissioning.
- Distinguish warnings, controlled process stops, protective stops and safety stops. Validate safe restart warnings/requirements.
