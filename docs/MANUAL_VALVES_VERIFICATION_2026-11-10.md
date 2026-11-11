# Manual Valves verification — 2026-11-10

## Latest owner revision — native screen persisted and software rebuilds passed

The approved recovered-work-copy reopened successfully after the save stall. All eleven final valve names, numerical 3/3/3/2 layout and separate unverified-mapping notice persisted without restoration edits. Independent visual evidence is runs/2026-11-10-smc-owner-final/native-reopened-final.png and native-final-after-compile.png. Full WinCC Unified software rebuild completed at 15:17:39 Sydney with 0 errors / 0 warnings. The native FB_Application [FB13] General > Information > Comment field was initially empty; it now contains the complete owner station legend and explicit unverified physical/gateway/legacy-EV associations, spring-return Y9/Y10 and preserved dual-loading constraints. Only that descriptive field changed, with no executable code, member or address edits. It saved normally. The final full PLC software rebuild after this comment completed at 15:24:08 with 0 errors / 0 warnings, superseding the earlier 15:19:32 pass.

Compiler evidence is native-hmi-rebuild-result.png and native-plc-post-comment-rebuild-result.png in the same folder; comment evidence is native-plc-legend-top-screenshot-1.png and native-plc-legend-middle-screenshot-1.png. These results replace the pending statuses in the recovery history below. The three protected AP19 entry-file hashes were checked again at 15:25:27 and remained unchanged; companion files are not covered by that statement. TIA remains open on the final Manual Valves page. Physical mapping remains unverified. No hardware configuration change, download, online access or output actuation occurred. The eight previously recorded hardware warnings were not recompiled or modified by this software-only task.

The final owner-confirmed valve functions supersede the older naming statements below. Current identities are 125Y1 Infeed Vacuum; 125Y2 Vacuum; 125Y3 Drainage; 125Y4 Return Dummy Bottle CIP; 125Y5 Bypass; 125Y6 Infeed Product; 125Y7 Spray CIP; 125Y8 Cap Distributor Air; 125Y9 Infeed Gate Open; 125Y10 Cap Opening; 125Y11 External Washing. There is no 125Y12. Required order is three columns with rows 1–3, 4–6, 7–9 and 10–11.

The repository HMI builder and audit expectations now describe eleven cards. The PLC source files 01_UDT_Types.scl, 09_FB_SMC_ValveManager.scl and 11_FB_Application.scl received descriptive owner-function legends; this increment does not establish new executable logic or physical assignments. Existing uncommitted functional changes in those files predate this descriptive increment. The HMI CSV description claiming a verified external-wash Q129.6 assignment was corrected to explicitly unverified; its tag name and PLC binding were preserved.

The updated [mapping table](../REV12/Documentation/SMC_FINAL_NUMBERING_2026-11-10.md) separates confirmed identities from unverified legacy EV associations, output channels, coil order and gateway transfer. New names do not validate the inherited production sequences. All unverified manual commands must remain disabled. MISSING CONFIG is a configuration finding, not a valve identity or a global PLC output inhibit.

Native visual editing now shows all eleven exact owner names in the required 3/3/3/2 order. The primary agent independently reviewed runs/2026-11-10-smc-owner-final/native-manual-valves-final-labels.png. The general notice says physical coil/channel and gateway mapping remain unverified and manual commands are disabled. Existing request/status bindings were preserved; there were no commanded physical outputs or downloads.

During the 14:56 save, completion could not be confirmed: the Save project / Please wait modal remained active, with Cancel unavailable and the main TIA process not responding. At that point the working AP19 last-write was 14:56:17 Sydney, and native recompilation and the PLC comment legend were pending. A normal Alt+F4 close attempt timed out without a discard prompt or closure; the delegated agent then explicitly relinquished GUI control. Operator authorization was requested before any forced termination of the affected instance (PID 7568), because unsaved edits could have been lost. No forced termination occurred.

A separate byte-level backup at runs/2026-11-10-smc-owner-final/saved-disk-backup-145811 contains 351 files, each matching its source hash before and after its copy. The final manifest is saved-disk-backup-final-manifest.json. Initial sharing errors and two missing destination directories were corrected using read-only shared streams and explicit directory creation. This is not proof of transactional consistency, a successful native reopen, or preservation of unsaved editor memory. No process was forcibly terminated.

The operator answered the recovery decision with **keep open and wait**. Forced termination/restart was not authorized. Only spaced observations continued while the save was pending; native editing and compilation waited for a responsive confirmed working copy.

At approximately 15:07 Sydney, the original window disappeared during the waiting period and PID 7568 was no longer present. No forced termination was performed. A different existing TIA window showed the older latest-10-11-copy project; it was inspected without editing. The cause of the original instance's exit remains unconfirmed (including whether the earlier normal close request completed later). The approved recovered-work-copy was then opened through the ordinary GUI workflow; TIA closed the older project without a save/discard prompt. All final labels and layout persisted, as independently verified before compilation. Evidence of the intermediate changed window: runs/2026-11-10-smc-owner-final/native-wait-ended-new-window.png.

The source builder, offline audit and existing navigation dependency compiled after the final Title Case label changes at 14:56:08 Sydney: exit code 0, no compiler diagnostics. Evidence: runs/2026-11-10-smc-owner-final/csharp-owner-labels-compile.txt. The resulting library was not executed against the native project. Existing native cards have now been grouped; the builder searches root ScreenItems and has not been validated for these grouped objects. Do not replay it against this working copy without resolving that difference. Source compilation is not native project compilation or FAT acceptance.

## Outcome

Corrected a false-positive in the offline builder audit. The previous predicate independently searched for the retired tag name and any Write(true) anywhere in an event. The Back navigation event resets SMC_EV001_Close_Manual to false and enables other destination-page tags; this wrongly triggered the retired-command exception. The new predicate associates the literal true/1 write with the specific retired tag. Six regression cases pass using the extracted C# method. This is a narrow generated-script audit, not a general JavaScript interpreter.

In TIA Portal V19, the existing manual_valves page in the disposable copy was inspected and the complete HMI_1 software rebuilt through its context menu. Result: Compiling finished (errors: 0; warnings: 0). Compiler reported 518 tags, 378 used tags, one connection and 32 used screens. Save project was invoked, followed by the disabled Save state. No native screen event was changed in this run because the suspected Close activation proved to be an audit error.

## Native UI checks

- REV50_ValveTest_Back click event visibly contains HMIRuntime.Tags("SMC_EV001_Close_Manual").Write(false).
- REV50_ValveTest_EV001Open_Command Press writes SMC_EV001_Open_Manual true.
- Its Release event writes SMC_EV001_Open_Manual false.
- Full-page editor view shows one single-opening-coil gate card, the other valve cards, permissives, return navigation, a 125Y10 spare card and the unresolved physical-mapping notice.
- Hardware mapping remains MISSING CONFIG. Compilation does not validate physical wiring or safety behavior.

The current-run isolated workshop confirmation was received. No runtime valve actuation, PLC edit or download was performed. One mistaken toolbar click opened Extended download to device; it was immediately cancelled without loading. TIA explicitly displayed Action canceled before download. The subsequent compile was selected from HMI_1 > Compile > Software (rebuild all).

## Project and evidence

Working project: runs/2026-11-10-manual-valves-hmi-compile/latest-10-11-copy/Backup Schlenkers 36-10 190036-7-8v2.12 10-11.ap19.

Protected AP19 sources under tia v19 programming hmi and plc and programmer_progetv19 were hashed before and after; all unchanged. Evidence manifests and regression output are in runs/2026-11-10-manual-valves-final-check/.

GUI screenshots of compile result and events were displayed in the conversation; standalone PNG files were not persisted. The full Openness Audit method was not re-executed against the project. Thus this result establishes the targeted event checks and a successful native HMI rebuild, not full runtime/FAT acceptance or verification of every valve binding.

## Follow-up: parallel review and native save at 13:43 Sydney

On 2026-11-10, three subagents independently reviewed SMC mapping, HMI tags/dependencies, and compilation evidence. Only the primary agent manipulated TIA. The operator confirmed the isolated workshop network for this session. No download, online operation or output actuation occurred.

Working copy: `runs/2026-11-10-tia-recovery-smc/recovered-work-copy/Backup Schlenkers 36-10 190036-7-8v2.12 10-11.ap19`.

The interrupted native edits were present. All five legacy commands EV010, EV011, EV210, EV211 and EV212 were individually re-observed with **Operator control - allow unchecked**, no dynamization overriding it, and text MISSING CONFIG. The existing Y8/Y9/Y11 holds remain visible. Y10 remains a configuration-hold card without an invented command or output. Fixed disabling is the current hold while physical mapping is unknown; it is not a completed dynamic Valve Test permissive implementation.

The existing global alarm object `REV13_Common_AlarmText` was moved/resized to `(950,81,165,30)`, font 15, Layer_1, above the covering status-bar background. The page label at `(735,81)` was narrowed to 190 pixels. The final editor view shows both labels separately. Existing text/colour scripts were preserved; the reviewed source counts active bits in `Alarm_Word1` and `Alarm_Word2`, with `ALARMS --` on invalid data. The editor's static ALARMS 0 is not a live machine observation. Runtime refresh was not tested.

Save completed; AP19 last-write is 13:43:52 Sydney. Full HMI software rebuild completed **13:45:08, 0 errors / 0 warnings**, 518 tags, 378 used tags, one connection, 32 used screens. This fresh result supersedes the earlier compilation evidence for these HMI edits. PLC software was not modified or rebuilt in this follow-up.

Repository builder changes preserve the five additional command holds and the alarm/page-label geometry. The builder was not executed against TIA. Native Layer_1 placement is documented but not automated in the builder because no established SDK layer setter was available in the repository patterns; it must be checked if regenerating the screen.

Compilation-only validation of the builder, audit and existing VerifyNavigationMaster dependency against installed TIA V19 assemblies passed with exit 0 and no compiler diagnostics. The resulting library was not executed. Evidence: `runs/2026-11-10-manual-valves-parallel-review/csharp-source-compile.txt`; initial command setup failures are retained alongside final success. Six existing retired-command audit regression cases also passed; these tests do not establish runtime/FAT acceptance.

### Remaining mapping and naming constraints

The [125Y1–125Y11 table](../REV12/Documentation/SMC_FINAL_NUMBERING_2026-11-10.md) remains partial. Y8/Y9/Y10/Y11 identities are operator-confirmed. EV010/EV011/EV210/EV211/EV212 associations to Y1–Y5 are historical, not newly verified physical wiring; their native labels were therefore retained. Y6/Y7 associations and the dual-loading coil order remain unresolved. No Y12 was introduced. No separate spring-return closing output was added.

The gateway PDO export, coil A/B channel table, transfer byte order through SWAP, Y10 output/sequence and legacy direct gate-output reconciliation are still required. MISSING CONFIG is retained and is **not a global PLC output inhibit**. `_Active` tags represent logical outputs, not physical valve feedback. Four displayed permissive badges are not equivalent to every condition in the PLC's ManualSMCTestActive logic; an enabling tag must not be invented from their conjunction.

### Persisted evidence

- `runs/2026-11-10-manual-valves-parallel-review/manual-valves-final.png`
- `runs/2026-11-10-manual-valves-parallel-review/hmi-compile.png`
- `runs/2026-11-10-manual-valves-parallel-review/plc-hardware-warnings.png`
- `source-before.json` and `source-after.json` in that folder: all three protected AP19 entry-file hashes unchanged; companion files are not covered by that statement.

The separate node_repl entry point failed before import with `failed to write kernel assets: The system cannot find the path specified. (os error 3)`, including after reset. The official cua_repl connector successfully selected, captured and edited the unique returned TIA V19 window. This is a working access path, not proof the failing node_repl entry point was repaired. Initial window resizing and one stale screenshot-ID error required fresh observation; an unintended alarm X-position edit was corrected and its final value verified before saving.
