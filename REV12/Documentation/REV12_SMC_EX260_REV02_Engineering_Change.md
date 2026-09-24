# SMC EX260 final output mapping — REV02

Implemented offline from the approved 2026-09-24 output schedule.

## Naming rule

PLC and HMI command names remain aligned with the existing functional valve
identifiers (`EV001`, `EV010`, `EV011`, `EV210`, `EV211`, `EV212`, `EV220`,
`EV221`). Manifold station labels are physical commissioning references only;
they are not used as PLC or HMI tag names.

## Four-byte output image

| Address | Functional command | Duty |
|---|---|---|
| `%Q128.0` | `EV010` | Product inlet |
| `%Q128.2` | `EV011` | Product close |
| `%Q128.4` | `EV210` | Vacuum after pump |
| `%Q128.6` | `EV211` | Vacuum in pump |
| `%Q129.0` | `EV212` | Vacuum before pump |
| `%Q129.2` | `EV220` | Cap distributor air |
| `%Q129.4` | `EV221` | Cap channel air |
| `%Q129.6` | `ExternalBottleWashValveCmd` | Bottle External Washing pneumatic enable |
| `%Q130.0` | `EV001 OPEN` | Infeed gate open |
| `%Q130.1` | `EV001 CLOSE` | Infeed gate close |
| `%Q130.2` | `FillerExternalWashValveCmd` | Filler External Washing pneumatic enable, 125Y10-A |

All odd companion bits, `%Q130.3`, and `%Q130.4` through `%Q131.7`
remain reserved and are forced off.

## Wash-function separation

`Bottle External Washing` and `Filler External Washing` are separate commands.
`ExternalBottleWashValveCmd` is assigned to `%Q129.6`.
`FillerExternalWashValveCmd` is separately assigned to 125Y10-A / `%Q130.2`.
Neither command can energise the other wash output.

## Interlocks and diagnostics

- `EV001 OPEN` and `EV001 CLOSE` are mutually exclusive.
- `EV010` and `EV011` are mutually exclusive.
- The output image is cleared on safety, communication, permission, or command
  conflict loss.
- Anybus PROFINET health is read from the configured device using CPU system
  diagnostics. Independent EtherCAT/manifold status remains unclaimed until a
  verified diagnostic source is mapped.
- A single `DWord` tag writes the complete image at `%QD128`.

## Verification

The selected PLC sources were imported into the working TIA V19 project and
compiled offline with `0 errors / 0 warnings`. No PLC/HMI download or online
operation was performed.

## Manual HMI valve test

The Manual page now opens a dedicated `MANUAL VALVE TEST` screen. It provides
momentary hold-to-run commands and final interlocked output indications for
EV001 open/close, EV010, EV011, EV210, EV211, EV212, EV220, EV221, Bottle
External Washing and Filler External Washing. Commands are accepted only while Manual mode, the page-active
flag, valve-test enable, safety, Anybus communication and machine interlocks are
valid. Leaving Manual clears test enable and every momentary request.

The PLC and Unified HMI were compiled offline after this addition with `0
errors / 0 warnings` each.
