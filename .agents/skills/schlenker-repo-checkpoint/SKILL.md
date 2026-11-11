---
name: schlenker-repo-checkpoint
description: Review, commit and push Schlenker repository checkpoints, including decisions, findings, tests, next steps, the hourly scheduled checkpoint and accumulated uncommitted work. Use for repository version history, not engineering deployment or release approval.
---

# Schlenker repository checkpoint

Keep `C:/www/Schlenker` versioned through small, descriptive commits. The user's
hourly automation authorizes routine commits and normal pushes of eligible project
changes to `origin` on `codex/revised-open-points`. That authorization does not
extend to other branches or remotes. The expected remote is
`https://github.com/feltrinbernardo/Schlenker.git` (the equivalent GitHub SSH URL is
acceptable). Verify the push URL as well as the fetch URL.

## Each run

1. Read root and applicable nested AGENTS.md instructions. Inspect status, the
   index, current branch, upstream, hooks and Git LFS configuration. Stop this
   run if the branch or destination differs, HEAD is detached, the index already
   contains staged changes, a merge/rebase/cherry-pick is active, or conflicts
   exist. Leave existing files and Git state intact; report the specific blocker.
2. Fetch origin normally. If the remote branch has commits absent from local
   HEAD, report that reconciliation is needed. Do not auto-merge, rebase, reset,
   stash, switch branches, force-push, amend commits, or bypass hooks.
3. Inventory modified, deleted and untracked files. Respect `.gitignore` and
   `.gitattributes`; never force-add excluded native TIA/Pilz projects, archives,
   credentials, context archives, runs, caches or helper binaries. Do not change
   ignore rules just to make status clean. Existing LFS rules remain in effect.
4. Review the candidate diff and file types. Check new or changed text for
   credentials, tokens, private keys and local secret exports; report paths,
   never secret values. Hold ambiguous files out with a reason. Preserve source,
   documentation and intentional deliverables, including clearly labelled WIP
   and historical revisions. Skip files changing during the run or modified in
   the last five minutes. Do not run engineering helpers or open TIA to validate
   a checkpoint. Treat vendor manuals and unfamiliar binary files separately:
   only include when their repository purpose and suitability are established.
5. Reconcile `docs/CONTINUITY.md` using the continuity procedure below. Include
   substantive handoff changes even when there is no source-code change. If the
   current task has no accessible conversation, use the existing handoff, source
   diffs, change log and available test evidence; mark missing context rather than
   inventing decisions or results. Then select explicit file paths, not blanket
   `git add .` or `git add -A`. Before staging, record candidate content hashes; recheck hashes and the initially
   empty index immediately before staging. If any change concurrently, defer
   this run rather than mixing versions. Add a change-log entry using
   `scripts/write-change-log.ps1` and include it in the selected files. Its result
   must distinguish version preservation from compilation/FAT verification.
6. Stage only selected files. Inspect the staged name list and diff, verify that
   the candidate content still matches the reviewed snapshot, and run
   `git -c core.whitespace=-blank-at-eof diff --cached --check` plus the normal
   repository commit hooks. The change-log writer intentionally leaves a blank
   separator at EOF; other whitespace errors still fail the check. Do not
   repair unrelated source just to get a checkpoint through. If validation or a
   hook fails, stop and report; do not loop or bypass it. Record any staged files
   left by this run so the user can recover the exact state.
7. Commit coherent groups with descriptive messages, for example
   `chore(checkpoint): preserve HMI and wiring work in progress`. The body lists
   included areas and validations actually performed. A checkpoint is not an
   engineering approval, released baseline, successful compilation or FAT.
8. Push normally to the verified origin branch. Existing unpushed commits must
   also be inspected for scope and sensitive material before pushing; never push
   an unknown backlog blindly. If pushing fails, preserve the local commit and
   report the error. On the next run, retry a normal push after the same checks;
   do not create duplicate/empty commits. Verify the remote branch SHA matches
   the pushed commit and report the remaining working-tree files.

## Continuity record

Maintain `docs/CONTINUITY.md` as a compact, current handoff, with separate entries
for active tasks when needed. Git preserves earlier revisions; do not append an
unbounded copy of the chat. For each task record:

- **Scope and revision:** task objective, checkout/branch, observed base commit,
  observation date with timezone, and whether work is uncommitted, committed or
  remotely verified. Do not claim the new checkpoint SHA inside its own commit;
  report that SHA after the commit/push is actually verified.
- **Decisions:** what was approved and why, with source (user/task reference or
  versioned requirement). Mark proposals separately. Historical authorization
  does not authorize a new hardware operation.
- **Findings:** what was discovered, its consequence, and supporting file,
  report or observation. Label inference and operator reports explicitly.
- **Changes:** what is implemented and what remains WIP, with file references.
- **Tests:** command or test, result, tested revision or explicit working-tree
  scope, evidence reference, and limitations. Distinguish not run, failed,
  simulated, compiled and physically validated. Listing a test file is not proof
  of a pass. Do not execute engineering tests for this record.
- **Pending items and next step:** unresolved questions, blockers and the next
  concrete action within the authorized scope, including what must be checked
  before continuing. Identify missing evidence explicitly.

Compare with the prior record before editing. Retain unresolved facts and
unrelated task entries; correct stale facts only with newer attributable evidence.
Do not change timestamps or create commits on an unchanged/no-news run. If
sources are unavailable, preserve last-known facts with their original date and
state the specific gap only when new; do not convert unknowns to success.

The task owner writes conversation-only decisions during work (see AGENTS.md).
The hourly run consolidates that record; it does not automatically see all chats,
recover hidden reasoning or load private context-archive databases. Link larger
local evidence only by a sanitized reference, marking it unavailable on another
machine when applicable. Never stage raw transcripts, credentials, addresses of
production devices, or excluded evidence/native archives.

Treat this as a shared file: capture its hash before preparing an update, and
recheck immediately before replacing it. If the file changed or is actively
being edited, preserve it and defer the continuity-dependent checkpoint. Do not
commit a partial handoff without explaining the missing evidence.

The five-minute exclusion still applies to existing candidate files, including
a recently edited handoff: defer that checkpoint rather than race its author.
The continuity file and mandatory change-log entry deliberately written by this
checkpoint are exempt from that age rule only; review and hash-check them like
all other candidates. Recheck every candidate before staging as described above.
If an evidence file is deferred, mark the related claim WIP/not included; do not
claim the checkpoint contains it. Continuity edits require the normal change log.

A new chat reads AGENTS.md, then this handoff, confirms Git state, and consults
only the evidence needed for the next task. It must not infer the current open
TIA project, unsaved native edits, loaded PLC version or current authorization
from a repository checkpoint. Native recovery follows `tia-project-versioning/`.

## Scheduling and reporting

The skill does not schedule itself. Use the Codex hourly thread automation to
invoke it. Stay quiet when there are no eligible changes, no new continuity facts
and no unpushed commits.
Notify on a successful checkpoint, a new failure or a blocker needing action;
avoid repeating an unchanged blocker every hour. Include commit SHA, branch,
push result, file count, the continuity-record path and any deferred paths/reasons.
Explicitly report missing handoff evidence without asking for already documented
information. Do not create release tags or PRs as part of this routine.

The canonical skill is this repository copy. The installed copy lives at
`C:/Users/SIMATIC User/.codex/skills/schlenker-repo-checkpoint/SKILL.md`; when
updating the workflow, keep both copies identical.

When adopting this revision on the bench, update the installed skill copy from
this canonical file and verify identical hashes. Preserve the existing hourly
schedule, target branch and remote. In the existing automation prompt, require
reading this repository copy on each run and updating `docs/CONTINUITY.md` before
the checkpoint. Do not create a second automation. Editing this file alone does
not prove the installed copy or scheduled prompt has been updated.
