# Dedicated Schlenker offline workspace

Established 11 November 2026, Australia/Sydney, at the operator's request.

## Preserved state and locations

Source of the agent's latest saved pendant/JOG work:
`runs/2026-11-11-pendant-implementation/Schlenker_Pendant_Offline/Schlenker_Pendant_Offline.ap19`.

The saved source includes the prior documented pendant implementation and HMI/Omron source separation. The earlier export verification reports native/proposal correspondence, unchanged physical symbol set, retained AL102 hold and saved software compilation with zero errors/warnings. These are historical results, not a fresh compile of the new copy. The source project marker is dated 11 November 2026 18:50:25 local time. No claim is made that this includes subsequent edits to the operator's separate main project.

Dedicated local root: `runs/2026-11-11-rob-isolated-workspace/`.

| Directory | Purpose |
|---|---|
| `baseline-preserved/` | Unmodified full snapshot of the saved source; never use for editing. |
| `work-copy/` | Only approved agent working project for subsequent TIA/Openness editing and compilation. |
| `source-manifest.json` | SHA-256 and length for each source file. |
| `copy-verification.json` | Copy paths, timestamp, counts and verification result. |

Open only `work-copy/Schlenker_Pendant_Offline.ap19` for agent work. The internal project name remains unchanged; select by full path, not title alone.

## Verification performed

- No TIA Portal process was running at snapshot creation or completion.
- All 237 source files passed an exclusive read-open check before copying.
- No reparse points were found in the source tree.
- Both copies contain the original file contents; SHA-256 comparison of all 237 files against both destinations and the source after copying found no mismatches.
- A residual Siemens FileStorage process (PID 7112, parent PID 4092 no longer present) was observed. It was not terminated; no exclusive-open failure was found on this project. Its existence alone does not establish an Openness conflict.
- Native projects and manifests stay under ignored `runs/`; they are not Git deliverables.

No project was opened, imported, compiled or downloaded during this isolation task. No PLC, installed HMI, hardware or safety configuration was changed. Native reopening/recovery remains to be verified before editing. This snapshot preserves disk state; it cannot certify recovery of already-lost unsaved memory state.

## Working and integration rules

Use one identified TIA process and one operation at a time. Verify the exact working-copy path before attaching any helper. Review and update old helper allowlists for the new destination before use; do not remove their guards or fall back to the first running project. Do not run GUI changes concurrently with an Openness import/save/compile.

Keep the operator's main project untouched. Future integration requires exported block/HMI differences, relevant compilations, an explicit list of unresolved physical assignments, a saved main-project backup and operator authorization for that integration. Copying the entire agent folder over the main project is not an approved integration method.

The operator's comparison tests make an access conflict a plausible hypothesis, not a confirmed root cause. Separate-path operation is a containment measure; a controlled reproduction and process/log evidence would be needed to establish causality.

## Independent helper review

Read-only source review found that `AttachImportSelectedSources.cs`, `AttachCompilePlc.cs` and `ExportCurrentPlcBlockSource.cs` select an exact supplied project path rather than a generic active project. They do not provide application-level exclusive-access coordination, so concurrent GUI/API editing is possible unless prevented by the workflow. This is a risk finding, not a demonstrated cause of the freeze.

Older `ApplyUnifiedLayoutRefinement.cs` and `ArchiveCurrentProject.cs` use first-match/suffix selection and must not be used with a short project-name hint. SMC helpers with fixed historical recovery paths must not be run unchanged. Before any future helper execution, review its full-path guard for this isolated copy, require a unique target, recheck identity after attachment, serialize operations and verify the helper exits before GUI editing resumes.
