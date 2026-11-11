# Hood fan electrical correction - M104A / M104B

## Status and evidence

This correction supersedes only the hood-fan portions of drawing sheets 15 and
31 in `SCHLENKER 3610 v05.pdf`.

The original drawing shows one `M105 HOOD FAN` as a 400 VAC, three-phase, 4 kW
motor operated by contactor `K105`. The photographed installed motor plate
instead identifies:

- manufacturer: Nidec;
- model: `SIC-67FV-F15B-1`;
- type: permanent-magnet brushless motor;
- motor bus: `DC 310 V`;
- rated output: `58 W`;
- poles: `8P`;
- speed: `1350 r/min`.

The operator states that each complete hood is supplied by 220-230 VAC
single-phase. The resulting approved architecture basis is therefore:

```text
230 VAC SINGLE PHASE
        L  N  PE
        |  |  |
   [protection / isolation TBC]
        |  |  |
   HOOD ASSEMBLY 1 (M104A)              HOOD ASSEMBLY 2 (M104B)
   associated/integrated electronics    associated/integrated electronics
        |                                     |
   Nidec BLDC motor                     Nidec BLDC motor - plate to verify
   DC 310 V motor bus                   same architecture stated by operator
```

## Mandatory construction holds

- Do not connect either motor directly to `L-N`, `L1-L2-L3`, a contactor motor
  output, a VFD output or a PLC 24 VDC output.
- The `DC 310 V` marking is the motor-side bus, not the complete hood's field
  supply connection.
- Identify and document the complete hood input terminals, associated or
  integrated controller, protective-device rating, isolation method, conductor
  size and control terminal functions before construction.
- Verify the Hood 2 motor plate. The available photograph proves the data of
  one physical motor only.
- `%Q4.2 / DB_Global.Out.AirFilterRelayCmd` may only operate an approved
  interposing/interface device. Direct connection is not approved.
- Hood filter alarms remain separate: Hood 1 `%I12.4`; Hood 2 `%I12.7`.

## Cable schedule basis

- `C-036A`: Hood 1 complete assembly, `L + N + PE`, conductor size/protection
  TBC.
- `C-036B`: Hood 2 complete assembly, `L + N + PE`, conductor size/protection
  TBC.

This correction is an engineering redline. It is not a construction release
until every mandatory hold above is closed by inspection of the installed hood
controller and terminal markings.
