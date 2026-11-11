# ROB HMI/PLC modifications — bounded implementation record

## Implemented offline

- AL102 functional allocation is frozen as X1 Product Presence, X2 Product
  Temperature, X3 Vacuum Sensor, X4 Distributor Anti-Rotation, X5 Tank Level,
  X6 Filler Zero Position – Valve #1, X7 CIP Presence / Engaged and X8 Spare.
- The PLC consumes AL102 only through normalized values with independent
  configured/quality/fault validity. No raw address, polarity, scale or timing
  has been inferred.
- Loss or invalid quality of X4 while the main drive is running requests an
  immediate stop and latches alarm 2252.
- X7 engaged, invalid, or not safety-validated inhibits Automatic, Manual,
  Jog, Homing and CIP-positioning rotation in the standard PLC.
- CIP Start enters a positioning state, uses the existing Jog speed parameter,
  stops at the latched X6 valve-1 reference, waits for confirmed zero speed and
  X7 connection, then permits the CIP state only when the safety hold point is
  validated.
- Connecting X7 before the X6 position or losing X7 during CIP creates a
  latched critical alarm.
- Logical pneumatic targets `125Y1` through `125Y7` are implemented separately
  from physical coil commands. Production requests Y1, Y2 and Y6 OPEN and Y3,
  Y4, Y5 and Y7 CLOSED.
- During a level pause, logical Y6 remains OPEN until product-pump feedback is
  valid and confirms the pump stopped. No delay was introduced.
- HMI tags, alarm texts, AL102 diagnostics, CIP state indications and SMC 125
  logical target indications are defined as read-only diagnostics.
- The existing single-coil infeed-gate correction, retired Cap Channel Air
  path and live common-alarm count remain part of this offline baseline.

## Deliberately not connected to hardware

- AL102 process-image addresses, device models, polarity and engineering-unit
  scaling;
- the independent safety implementation and validation for X7;
- physical Anybus bits and energisation polarity for 125Y1–125Y7;
- operational download, RUN/STOP/reset, forcing or output enable.

All commissioning flags default to false. Consequently this source baseline is
fail-safe: rotation and CIP start remain inhibited until the missing hardware
and safety evidence is completed under a separate authorised activity.

## Evidence required for operational release

1. Approved AL102 hardware/port process map and device data for X1–X7.
2. Confirmed normal polarity, invalid-quality behaviour, ranges and units.
3. Validated safety design and test record for X7 rotation inhibition.
4. Approved Anybus/SMC bit schedule and coil polarity for 125Y1–125Y7.
5. End-to-end proof that product-pump stopped feedback controls Y6 closure.
6. Successful TIA V19 compile and offline simulation on a disposable project.
7. Separate current-run authorization before any development-hardware download.
