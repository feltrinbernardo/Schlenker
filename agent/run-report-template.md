# Run Report

## Identification

- Run ID: `<YYYYMMDD-HHMMSS-profile-task>`
- Timestamp and timezone: `<ISO 8601>`
- Profile: `<profile path and id>`
- Task brief: `<task brief path>`
- Operator / agent: `<name>`

## Safety gate

- Physical PLC disconnected and verified by: `<person and time>`
- Original project path: `<path>`
- Working-copy path: `<path>`
- Original pre-run SHA-256: `<hash>`
- Original pre-run modification time: `<timestamp>`

## Verified project metadata

- GX Works product and version: `<value>`
- PLC family and CPU: `<value>`
- Project type: `<simple | structured | other>`
- Program languages: `<value>`

## Actions and changed artefacts

List material UI actions and every changed program, label, parameter, or other
project artefact. State `none` for a baseline-only run.

## Build result

- Result: `<pass | fail | not run>`
- Errors: `<count and summary>`
- Warnings: `<count and summary>`
- Compiler evidence: `<relative evidence path>`

## Original-project integrity

- Original post-run SHA-256: `<hash>`
- Original post-run modification time: `<timestamp>`
- Matches pre-run values: `<yes | no>`

## Assumptions and open items

Record unresolved engineering questions, missing signals, and any reason the run
could not be completed.
