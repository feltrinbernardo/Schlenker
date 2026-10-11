# PILZ Safety Logic Implementation Worksheet

Status: engineering allocation documented; executable PILZ safety program not created.

## Safety boundary

- PILZ PNOZmulti 2 remains the sole authority for E-stops, guard locks, standstill, STO, safety contactors, actuator safety power, pneumatic dump/isolation, EDM and safe restart prevention.
- Siemens PLC and WinCC Unified may send non-safety requests and display diagnostics only.
- No HMI object shall directly energise a safety output.
- No HMI Safety Reset request is implemented until the authorised safety design accepts it.

## Intended functional sequence

1. A physical door-access button produces an access request.
2. The standard PLC requests a controlled machine stop and inhibits production commands.
3. PILZ independently evaluates E-stops, dual standstill sensors, STO/power removal, EDM and pneumatic dump feedback.
4. PILZ may release only the requested zone after every validated safe-state condition is true.
5. Opening a guard maintains the automatic-operation inhibit.
6. Closing a guard does not restart the machine.
7. A deliberate physical Safety Reset is required after all guards are closed/locked and every safety condition is valid.
8. A separate deliberate machine Start is required. Automatic restart is prohibited.

## Known physical allocation

The approved local module/channel allocation is recorded in `PILZ_LOCAL_SAFETY_IO_ALLOCATION.csv`.

## Implementation blockers

The following are required before executable PNOZmulti logic or a physical Siemens data map may be commissioned:

- Native PNOZmulti Configurator project and installed compatible engineering software.
- Approved risk assessment and required PLr/category/SIL targets.
- Approved stop categories and validated guard-release conditions.
- Frozen PB-DR-REQ-ALL behaviour.
- Decision on whether an HMI reset request is accepted.
- Validated pneumatic stored-energy/dump criteria.
- Final Siemens/PILZ PROFINET station name, IP configuration, input/output byte lengths and bit offsets.
- Authorised functional-safety validation and sign-off.
- Confirmation that the physical machine has 12 guarded doors; earlier Schlenker project material identifies 11.

## Current Siemens/HMI treatment

- Existing grouped interface `DB_Global.PilzDiag` remains unchanged.
- `PhysicalMapConfigured` remains false until a proven physical telegram is configured.
- Individual door runtime states are displayed as `MAP MISSING` / `NOT COMMISSIONED`; no values are invented.
- PLC machine logic, hardware configuration, network configuration and safety logic are not modified by this task.
