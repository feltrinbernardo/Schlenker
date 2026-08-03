# Schlenker

Local test harness for an industrial automation agent that engineers and edits
offline Mitsubishi GX Works projects through a desktop-control loop.

## What is in this repository

- `agent/`: the stable system prompt, the per-task brief template, and the
  computer-use contract.
- `profiles/`: target-specific facts such as PLC family, GX Works version,
  programming language, and test I/O mappings.
- `fixtures/`: local offline GX Works sample projects and requirements. Project
  files are deliberately ignored because they may be proprietary.
- `evals/`: repeatable acceptance tasks and their expected outcomes.
- `runs/`: generated screenshots, action traces, build output, and reports.

## Local test loop

1. Choose a profile in `profiles/`.
2. Copy `agent/task-template.md` into a new task brief and fill in its fields.
3. Give the agent the system prompt, selected profile, and task brief.
4. Run the task against an offline fixture in GX Works.
5. Save evidence in `runs/<run-id>/` and assess it against the matching eval.

## Initial model setting

Use `gpt-5.6-sol` with `reasoning.effort: high` as the baseline. Keep the
configuration fixed for the first evaluation batch, then compare alternatives
using the same cases and evidence requirements.

## Definition of a passing local test

- The project was changed only as requested.
- The requested program structure and comments exist.
- The project compiles without errors.
- The run record identifies changed artefacts, build result, and open items.

This repository is intentionally an offline-test environment. Do not place live
PLC credentials, production projects, or physical-equipment connection details
in it.
