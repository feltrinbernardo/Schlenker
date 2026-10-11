# SMC EX260 complete output mapping — REV03

REV03 supersedes the 2026-09-24 and 2026-09-26 REV02 mappings. It implements
the approved 2026-09-27 complete SMC output allocation. This revision removes
the former Filler External Washing assignment from 125Y10-A and reserves both
coils of 125Y10.

## Naming rule

The physical output is written through the single `DWord` tag
`Y_SMC_EX260_OutputImage` at `%QD128`. Functional names in the table describe
the individual bits of `DB_Global.Out.ValveBits`; they are not additional PLC
output aliases and must not be created as overlapping physical tags.

## Four-byte output image

| Station | Address | Functional command | Duty |
|---|---|---|---|
| `125Y1-A` | `%Q128.0` | `EV010` | Product inlet |
| `125Y2-A` | `%Q128.2` | `EV011` | Product shutoff / pump circuit |
| `125Y3-A` | `%Q128.4` | `EV210` | Vacuum after pump / CIP |
| `125Y4-A` | `%Q128.6` | `EV211` | Vacuum in pump / CIP timed stage |
| `125Y5-A` | `%Q129.0` | `EV212` | Vacuum before pump |
| `125Y6-A` | `%Q129.2` | `EV220` | Cap distributor air |
| `125Y7-A` | `%Q129.4` | `EV221` | Cap channel air |
| `125Y8-A` | `%Q129.6` | `ExternalBottleWashValveCmd` | Bottle-wash pneumatic enable |
| `125Y9-A` | `%Q130.0` | `EV001 OPEN` | Infeed gate open |
| `125Y9-B` | `%Q130.1` | `EV001 CLOSE` | Infeed gate close |
| `125Y10-A` | `%Q130.2` | Spare | Forced `FALSE` |
| `125Y10-B` | `%Q130.3` | Spare | Forced `FALSE` |

The B-side bits for single-solenoid stations `%Q128.1`, `%Q128.3`, `%Q128.5`,
`%Q128.7`, `%Q129.1`, `%Q129.3`, `%Q129.5` and `%Q129.7` are reserved. Bits
`%Q130.4` through `%Q131.7` are also reserved. The output manager initializes
the complete `DWord` to zero before assigning active commands, so every spare
bit remains `FALSE`.

## Interlocks and diagnostics

- `EV001 OPEN` and `EV001 CLOSE` are mutually exclusive.
- `EV010` and `EV011` are mutually exclusive.
- The output image is cleared on safety, communication, permission or command
  conflict loss.
- Gate feedback remains separate: open `%I0.5`, closed `%I0.6`.
- EV230 and the Pilz-controlled pneumatic safety dump remain outside this
  normal SMC manifold map.
- Anybus PROFINET health is obtained through CPU system diagnostics. Separate
  EtherCAT/manifold health remains unclaimed until a verified diagnostic source
  is mapped.

## HMI alignment

The Manual Valve Test screen provides hold-to-run commands and final
interlocked indications for EV001 open/close, EV010, EV011, EV210, EV211,
EV212, EV220, EV221 and Bottle External Washing. The former Filler External
Washing command and indication are retired. The screen identifies 125Y10 as a
spare double-solenoid station with `%Q130.2/%Q130.3` forced off.

## Validation status

Offline compilation verifies software consistency only. Physical station,
coil, actuator, feedback and fail-state validation remains pending and requires
separate commissioning authority. No physical test result is claimed here.
