# REV23 Pilz PROFINET Non-Safety Diagnostics Mapping Report

## Outcome

REV23 adds a diagnostics-only logical interface between the existing Siemens PLC data model and the WinCC Unified HMI. The Pilz controller remains the sole safety authority. No PLC or HMI output, reset command, bypass, force, or safety decision was added.

The current TIA Portal project does **not** contain a configured Pilz/PNOZ device, Pilz GSDML, verified PROFINET device name/IP address, or a proven Pilz process-data byte/bit map. Consequently:

- `DB_Global.PilzDiag.PhysicalMapConfigured` is fixed to `FALSE`.
- `DB_Global.PilzDiag.DataValid` cannot become true.
- The HMI displays `COMMUNICATION FAULT / DATA INVALID` for dependent status values.
- No raw process-data address or bit position has been invented.

## Implemented logical interface

| HMI tag | PLC logical member | Existing aggregate source | Meaning | Validity rule |
|---|---|---|---|---|
| `PilzDiag_CommunicationOK` | `DB_Global.PilzDiag.CommunicationOK` | `DB_Global.Inp.PilzDeviceOK` | Existing aggregate device-health indication | Diagnostic only; does not prove the physical map |
| `PilzDiag_PhysicalMapConfigured` | `DB_Global.PilzDiag.PhysicalMapConfigured` | Constant `FALSE` | Physical Pilz mapping verification gate | Remains false until GSDML and byte/bit map are proven |
| `PilzDiag_DataValid` | `DB_Global.PilzDiag.DataValid` | `CommunicationOK AND PhysicalMapConfigured` | Validity gate for every dependent diagnostic | False in the present project |
| `PilzDiag_SafetyReady` | `DB_Global.PilzDiag.SafetyReady` | `DB_Global.Inp.SafetyOK` | Aggregate safety-ready diagnostic | Cleared when `DataValid=FALSE` |
| `PilzDiag_GeneralFault` | `DB_Global.PilzDiag.GeneralFault` | `DB_Global.Inp.SafetySystemFault` | Aggregate safety-system fault diagnostic | Cleared when `DataValid=FALSE` |
| `PilzDiag_ResetRequired` | `DB_Global.PilzDiag.ResetRequired` | `DB_Global.DoorAccess.AlarmResetRequired` | Reset-required indication | Indication only; never resets Pilz |
| `PilzDiag_EStopChainHealthy` | `DB_Global.PilzDiag.EStopChainHealthy` | `DB_Global.Inp.EStopChain24VHealthy` | Aggregate E-stop chain diagnostic | Individual E-stop identity is unavailable |
| `PilzDiag_AllGuardsClosed` | `DB_Global.PilzDiag.AllGuardsClosed` | `DB_Global.Inp.AllDoorsClosed` | Aggregate status for the 11 guarded doors | Individual door identity is unavailable |
| `PilzDiag_AllGuardsUnlocked` | `DB_Global.PilzDiag.AllGuardsUnlocked` | `DB_Global.Inp.AllDoorsUnlocked` | Aggregate unlock indication | Lock topology and individual states are unverified |
| `PilzDiag_SafetyCircuitClosed` | `DB_Global.PilzDiag.SafetyCircuitClosed` | `DB_Global.Inp.SafetyCircuitClosed` | Aggregate closed-circuit diagnostic | Cleared when `DataValid=FALSE` |
| `PilzDiag_StandstillConfirmed` | `DB_Global.PilzDiag.StandstillConfirmed` | `DB_Global.Inp.ZeroSpeedConfirmed` | Aggregate standstill/zero-speed diagnostic | Cleared when `DataValid=FALSE` |
| `PilzDiag_ThreePhaseOffConfirmed` | `DB_Global.PilzDiag.ThreePhaseOffConfirmed` | `DB_Global.Inp.ThreePhaseOffConfirmed` | Aggregate three-phase-off diagnostic | Cleared when `DataValid=FALSE` |

## HMI implementation

The existing `safety_pilz_diagnostics` screen was updated in place. It includes:

- PROFINET diagnostic-interface state and explicit physical-map status.
- Grouped emergency-stop and guard diagnostics.
- Motion/energy diagnostic indications.
- A dedicated unresolved-physical-data panel.
- `DATA INVALID` presentation whenever the validity gate is false.
- The approved production-master header, user indicator, and right-side navigation geometry.

No screen was duplicated and no global navigation behavior was changed.

## Verification evidence

| Check | Result | Evidence |
|---|---|---|
| PLC source import/rebuild | 0 errors, 0 warnings | `REV23_PLC_import_compile.log` |
| HMI rebuild | 0 errors, 0 warnings | `REV23_HMI_build_final.log` |
| Target-screen navigation master | PASS | `REV23_navigation_master_final.log` |
| Target-screen common header | PASS | `REV23_common_header_final.tsv` |
| TIA device/tag audit | PASS; no Pilz station found | `tia_audit/AuditIoMapping.log` |

The repository-wide legacy HMI verifier expects obsolete REV12 object names and reports unrelated baseline failures. It is not used as proof of REV23 target-screen compliance. The target screen passes the current production-master navigation and header checks.

## Safety boundary

This interface is non-safety diagnostics only. Siemens PLC or HMI values must never substitute for the Pilz safety program, safety outputs, or certified safety circuit. Alarm acknowledgement and reset indications do not reset the Pilz controller or bypass safety interlocks.

