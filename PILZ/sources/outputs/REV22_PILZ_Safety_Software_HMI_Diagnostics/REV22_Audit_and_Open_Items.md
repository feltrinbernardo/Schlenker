# REV22 Pilz Safety Software / HMI Diagnostics Audit

## Proven project state

- Offline TIA Portal V19 project: `schlenkers 36-10 190036-7-8v2.12.ap19`.
- TIA Device Configuration contains the S7-1512C-1 PN CPU and MTP1500 Unified Comfort HMI only.
- No Pilz station, Pilz PROFINET device, or Pilz hardware channels are configured in the TIA project.
- No Pilz PNOZmulti project file or electrical safety drawing is present in the repository.
- Existing PLC/HMI interfaces provide grouped diagnostic signals only: overall safety ready, Pilz/network health, aggregate E-stop chain, all doors closed/unlocked, safety circuit closed, safety-system fault, zero speed, three-phase off, and reset-required coordination.

## Permitted REV22 implementation

Create a read-only `SAFETY / PILZ DIAGNOSTICS` HMI page using only the existing verified aggregate tags. Individual E-stop, individual guard, STO, pneumatic dump, pressure-feedback, and EDM states shall display as `NOT CONFIGURED` and shall not be bound to invented signals.

The existing alarms remain authoritative:

- 1001 — Emergency stop pressed (aggregate chain)
- 1003 — Safety circuit not ready
- 2231 — Safety system diagnostic fault

No new safety alarm is created without a proven source signal.

## Required information to close REV22

1. Native Pilz PNOZmulti project and exact controller/module catalogue numbers.
2. Electrical safety drawings showing every input, output, terminal, channel, feedback loop, reset circuit, STO circuit, and pneumatic dump circuit.
3. Confirmed PROFINET/fieldbus mapping between Pilz and Siemens PLC.
4. Individual guard/lock feedback topology, if individual HMI diagnostics are required.
5. Commissioning evidence for E-stop, guard, reset, STO, pneumatic dump, standstill, and EDM tests.

Until these inputs are supplied, physical safety validation is `NOT COMMISSIONED`.

## Implementation and validation ledger

| Scope | Status | Evidence |
|---|---|---|
| Repository/Pilz artifact audit | VALIDATED | No native Pilz project or electrical safety drawing found |
| TIA Device Configuration audit | VALIDATED | Read-only Openness audit; CPU and HMI present, Pilz device absent |
| Safety I/O matrix | VALIDATED | `REV22_Safety_IO_Matrix.csv` |
| Offline project checkpoint | VALIDATED | `pre-REV22-Pilz-HMI-checkpoint` |
| `safety_pilz_diagnostics` HMI page | VALIDATED | Grouped verified tags only; Production master navigation applied |
| Diagnostics-page entry button | VALIDATED | Navigates to `safety_pilz_diagnostics` |
| PLC logic / hardware / network changes | VALIDATED | No changes performed |
| HMI compile | VALIDATED | 0 errors, 0 warnings; `REV22_build_resume.log` |
| Pilz software compile | BLOCKED | Native Pilz project is missing |
| Physical safety functional test | NOT COMMISSIONED | Requires approved machine test and commissioning evidence |

The HMI transaction completed offline and saved successfully. The new page is diagnostic-only and does not provide a safety reset, bypass, force, or hardware command.
