# HMI and Omron JOG source separation

## Result and scope

Applied and saved only in `runs/2026-11-11-pendant-implementation/Schlenker_Pendant_Offline/Schlenker_Pendant_Offline.ap19` using the authorized offline TIA Openness workflow. PLC software compilation: **0 errors, 0 warnings**. No online session, download, device writes or output activation. Hardware and HMI were not modified or recompiled in this task.

This completes command-source separation, not physical commissioning or restoration of carousel motion. The existing unresolved common AL102 hold remains active in this disposable copy. The operator reports that the machine already moves using its installed HMI JOG; this task did not compare or alter that installed PLC program.

## Native evidence and changes

Fresh native HMI audits before and after confirm `manual/REV12_Manual_Jog_Enable`: Down writes 1 and Up writes 0 to `Jog_PB`, connected to `DB_Global.Cmd.JogPB`. These scripts and tags were not changed.

Changed `FB_Application` (FB13) and added the logical diagnostic/arbitration DB `DB_ManualJogSources`. No physical I/O addresses were introduced or reassigned.

| Item | HMI | Omron |
|---|---|---|
| Request | `DB_Global.Cmd.JogPB` | `DB_PendantCommissioning.JogRequest` |
| Source validity | Existing `Inp.HMILinkOK` heartbeat | Existing pendant connection and commissioning validation gates |
| Speed | Existing bench formula `ProductionSpeedBPH / MaxSpeedBPH * 100`, limited to 0–100% | Separate `ValidatedSpeedPct`, still 0 pending verification |
| Unverified pendant mapping | Does not determine HMI source eligibility | Keeps Omron commands disabled |

Both requests use actual SafetyOK, MANUAL selection/state, drive ready/no fault, height-motion exclusion, no simulation publication and the common AL102 hold. Simultaneous requests reject both. Selection resets each cycle; no command latch was added. The existing single FB5 instance remains the execution path. Its preserved input name `PendantConnected` receives qualified source acceptance; this is an internal execution gate, not a claim that a physical pendant is connected when HMI is selected.

The HMI speed formula is the previous bench formula, not the older non-simulation `Par.JogSpeedPct` branch. No new Hz values or mechanical conversion were invented. Parameter checks do not certify a safe manual speed.

`HmiSourceEligible` distinguishes source eligibility from the common process hold. `HmiAccepted` and `OmronAccepted` indicate software arbitration only, not valve/drive feedback or physical movement. `SelectedSource`: 0 none, 1 HMI, 2 Omron. These are new PLC diagnostics, not new HMI displays.

## Unresolved conditions

The prior implementation still writes `AL102InterlockVerified := FALSE` and `RotationInhibited := TRUE` every scan. Thus `ProcessInterlockBlocked` remains TRUE and neither source is accepted for physical rotation. Actual X4/X7 mapping, polarity, quality handling and safety validation remain to be reconciled. This is an explicit commissioning hold, not a measured live AL102 fault.

Omron enabling-switch/PILZ channels, input mapping and speed conversion remain unverified and disabled. No `SimulationMode` or bench exception was restored to substitute for actual safety consent. Behavior on restoring consent with a button still held requires acceptance testing; no release-to-rearm latch was added.

## Verification evidence

Run directory: `runs/2026-11-11-hmi-omron-separation/`.

- `software-compile.txt`: generated both sources, root 0 errors/0 warnings, save invoked, PASS.
- `after/FB_Application.scl` and `after/DB_ManualJogSources.db`: fresh native re-exports.
- `verification.json`: native FB matches proposal after whitespace normalization; physical symbol set and final publication section unchanged; baseline export SHA-256 hashes unchanged; shared holds retained.
- `hmi-jog-audit.txt` and `hmi-jog-audit-after.txt`: unchanged native JOG bindings and press/release scripts.
- `final-compile.jpg`: offline project title and visible compiler result 0 errors/0 warnings.

These checks are source/compilation evidence, not PLC execution, PLCSIM, measured stopping performance, FAT or physical safety validation. Protected originals were not targeted; the hash checks recorded here cover the baseline block exports, not a fresh whole-project comparison of the protected original.
