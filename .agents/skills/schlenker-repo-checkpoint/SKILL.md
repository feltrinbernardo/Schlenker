---
name: schlenker-repo-checkpoint
description: Review, commit and push Schlenker repository checkpoints, including the hourly scheduled checkpoint and accumulated uncommitted work. Use for repository version history, not engineering deployment or release approval.
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
5. Select explicit file paths, not blanket `git add .` or `git add -A`. Before
   staging, record candidate content hashes; recheck hashes and the initially
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

## Scheduling and reporting

The skill does not schedule itself. Use the Codex hourly thread automation to
invoke it. Stay quiet when there are no eligible changes and no unpushed commits.
Notify on a successful checkpoint, a new failure or a blocker needing action;
avoid repeating an unchanged blocker every hour. Include commit SHA, branch,
push result, file count and any deferred paths/reasons. Do not create release
tags or PRs as part of this routine.

The canonical skill is this repository copy. The installed copy lives at
`C:/Users/SIMATIC User/.codex/skills/schlenker-repo-checkpoint/SKILL.md`; when
updating the workflow, keep both copies identical.
