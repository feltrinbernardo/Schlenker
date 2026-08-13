# Agent Foundation Baseline Inventory — 2026-08-10

## Purpose

Record the repository state observed when Phase 0 began. This is a read-only
inventory. It does not assign ownership to existing changes and does not
authorize discarding, stashing, rebasing, or committing them together.

## Git baseline

| Item | Observed value |
|---|---|
| Current branch | `codex/initial-agent-scaffold` |
| Local `HEAD` | `978205f593e326a6c82bdc66cca04615f62788f7` |
| Local tracking ref before refresh | `978205f593e326a6c82bdc66cca04615f62788f7` |
| Remote branch observed with `ls-remote` | `f96d2d0a17cd9a8e73ab64bd9678a3f1bbe45eac` |
| Worktree | Dirty |
| Modified tracked entries | 24 |
| Deleted tracked entries | 1 |
| Untracked entries reported by Git | 35 |

The local checkout does not yet represent the merged remote commit. The local
tracking reference is also stale, so a fetch is required before creating a
clean baseline from the remote branch.

## Worktree inventory

### Modified tracked entries

- `.codex/hooks.json`
- `.codex/hooks/user_prompt_submit.py`
- `.gitignore`
- `README.md`
- `REV12/00_README_FIRST.md`
- `REV12/Documentation/REV12_Change_Log.md`
- `REV12/HMI/REV12_Alarm_List_EN_IT.csv`
- `REV12/HMI/REV12_HMI_Pages.md`
- `REV12/HMI/REV12_HMI_Tags.csv`
- `REV12/PLC_Sources/01_UDT_Types.scl`
- `REV12/PLC_Sources/02_DB_Global.scl`
- `REV12/PLC_Sources/03_FB_MainState.scl`
- `REV12/PLC_Sources/04_FB_ProductPump.scl`
- `REV12/PLC_Sources/06_FB_HeightMode.scl`
- `REV12/PLC_Sources/08_FB_VacuumCIP.scl`
- `REV12/PLC_Sources/10_FB_AlarmManager.scl`
- `REV12/PLC_Sources/11_FB_Application.scl`
- `REV12/PLC_Sources/12_DB_Application.scl`
- `REV12/PLC_Sources/13_OB100_Startup.scl`
- `REV12/PLC_Sources/14_OB1_Main.scl`
- `REV12/REV12_MANIFEST.json`
- `judge/README.md`
- `judge/judge.py`
- `logs/change-log/2026-08-05.md`

### Deleted tracked entry

- `Automation_Development_Pipeline (1).md`

The apparent replacement `docs/automation-development-pipeline.md` is currently
untracked. The rename relationship has not been assumed or staged.

### Untracked entries

- `.github/`
- `REV12/Documentation/REV12_1_Engineering_Change.md`
- `REV12/Documentation/REV12_2_Acceptance_Test_Matrix.md`
- `REV12/Documentation/REV12_2_Engineering_Change.md`
- `REV12/Documentation/REV12_3_HMI_UI_Layout_Refinement.md`
- `REV12/Documentation/REV12_Device_Code_Cross_Reference.csv`
- `REV12/Documentation/REV12_IO_Master_Port_Mapping.csv`
- `REV12/Documentation/REV12_Master_Cable_Schedule.csv`
- `REV12/Documentation/REV12_SET03_Acceptance_Test_Matrix.md`
- `REV12/Documentation/REV12_SET03_Engineering_Change.md`
- `REV12/PLC_Sources/10E_FB_ExternalBottleWash.scl`
- `REV12/PLC_Sources/10F_FB_AutoBottleGateRequest.scl`
- `REV12/PLC_Sources/10G_FB_RunOutProduct.scl`
- `REV12/PLC_Sources/10H_FB_CIPMediaPump.scl`
- `REV12/PLC_Sources/10I_FB_ProductionVacuum.scl`
- `REV12/PLC_Sources/10J_FB_ProductCircuitManager.scl`
- `REV12/PLC_Sources/10K_FB_LiftConfirmation.scl`
- `docs/`
- `judge/config.json`
- `judge/requirements.txt`
- `judge/test_judge.py`
- `logs/change-log/2026-08-06.md`
- `logs/change-log/2026-08-07.md`
- `logs/change-log/2026-08-08.md`
- `logs/change-log/2026-08-09.md`
- `logs/change-log/2026-08-10.md`
- `scripts/test-prompt-judge.ps1`
- `scripts/test-repository.ps1`
- `specs/`
- `tools/tia-v19-openness/ApplyUnifiedLayoutRefinement.cs`
- `tools/tia-v19-openness/ArchiveCurrentProject.cs`
- `tools/tia-v19-openness/BuildUnifiedHomeScreen.cs`
- `tools/tia-v19-openness/ImportRev12Sources.cs`
- `tools/tia-v19-openness/InspectUnifiedProject.cs`
- `tools/tia-v19-openness/VerifyUnifiedHmi.cs`

Git collapses untracked directory content in this view. Each directory requires
a separate ownership and generated/source classification before staging.

## Protected GX Works2 source and fixture

Both files were inspected through read-only filesystem hashing. GX Works2 was
not launched.

| Role | Path | Length | Last write UTC | SHA-256 |
|---|---|---:|---|---|
| Protected original | `D:\GX Works\schlenker.gxw` | 1,323,520 | `2026-08-03T03:19:21.3460000Z` | `B16C05F4A555E9E9628BE35AAAA889A21D047AC239DA1D5D9390FC8704A29BD1` |
| Disposable fixture | `fixtures/projects/schlenker-working/schlenker.gxw` | 1,323,520 | `2026-08-03T03:19:21.3460000Z` | `B16C05F4A555E9E9628BE35AAAA889A21D047AC239DA1D5D9390FC8704A29BD1` |

Result: the protected original and disposable fixture were byte-identical at
the start of Phase 0. This observation is not a current-run UI or PLC
authorization.

## Foundation prerequisites

| Check | Status | Notes |
|---|---|---|
| Python 3.13 | Ready | Python `3.13.14` observed |
| Kiro feature spec | Ready, untracked | 114 acceptance criteria and 72 tasks |
| `pyproject.toml` | Missing by design | Created in Task 1.1 |
| Root repository policy | Present, GX-specific | Must be split into shared and nested scopes |
| Nested `AGENTS.md` files | Missing | Proposed in `policy-boundaries.md` |
| GX Works2 profiles | Present | Two Schlenker profiles and two GX Works3 demos |
| TIA V19 runtime profile | Missing | Required before the TIA adapter phase |
| Clean implementation branch/worktree | Not ready | Gate 0.1 remains open |

## Safe baseline decision

### Recommended approach

1. Fetch the merged remote branch without changing the dirty worktree.
2. Create `codex/agent-foundation` from remote commit `f96d2d0`.
3. Create a separate clean worktree for that branch.
4. Apply only reviewed spec, Phase 0, and agent-runtime changes there.
5. Leave all current PLC/HMI/TIA and other user-owned changes untouched in the
   original worktree until their owners classify and commit them.

### Alternatives not selected automatically

- Commit all current changes together: rejected because it would combine
  unrelated ownership and workstreams.
- Stash all current changes: rejected because it would hide user-owned state and
  complicate untracked-file recovery.
- Reset, checkout, clean, or rebase the current worktree: prohibited without an
  explicit reviewed preservation plan.

## Gate 0.1 exit criteria

- [x] The remote commit was fetched and verified as
  `f96d2d0a17cd9a8e73ab64bd9678a3f1bbe45eac`.
- [x] Dedicated branch `codex/agent-foundation` and clean worktree
  `C:\www\Schlenker-agent-foundation` were created from that commit.
- [x] The reviewed spec and Phase 0 artifacts are present in the clean worktree.
- [x] The original worktree remains on `978205f` with its pre-existing changes
  available; it was not stashed, reset, rebased, cleaned, or switched.
- [x] The implementation base commit and branch are recorded by the mandatory
  change-log entry for this request.

Gate 0.1 status: **complete**. This does not classify or approve the unrelated
changes in the original worktree; it isolates foundation development from them.
