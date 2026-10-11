# Repository Layout and Versioning

Date: 2026-10-12
Status: active repository organization policy

## Reviewable source of truth

The repository is organized by responsibility rather than by individual chat
or temporary agent session:

| Area | Canonical paths | Versioning rule |
|---|---|---|
| Agent governance | `AGENTS.md`, `agent/`, `.codex/`, `.agents/`, `policies/`, `profiles/` | Track prompts, policies, hooks, skills, and deterministic tests |
| Multi-agent architecture | `documentos/schlenker/agente-*.md`, `config/agents/`, `tools/schlenker-agent-monitor/` | Track role contracts, orchestration configuration, monitor source, and tests |
| PLC/HMI engineering source | `REV12/PLC_Sources/`, `REV12/HMI/`, `REV12/Documentation/` | Track exported source, tags, alarms, mappings, and reviewed engineering documents |
| Engineering automation | `tools/tia-v19-openness/`, `tools/tia-v16-openness/`, `tools/profinet/`, `scripts/` | Track source and validation scripts; ignore compiled binaries and build output |
| Safety engineering | `PILZ/` and reviewed safety documents | Track analysis and approved source exports; never infer safety validation from documents alone |
| Specifications and tests | `specs/`, `evals/`, `judge/`, `fixtures/` | Track requirements, deterministic cases, and non-proprietary fixtures |
| External services | `services/` | Track deployable source and tests; keep credentials in runtime secret stores |
| Audit records | `logs/change-log/` | Track append-only request/change entries; ignore runtime telemetry and archived transcripts |
| Generated deliverables | `output/` | Track reviewed PDFs through Git LFS; retain reproducible source beside the product documentation or tool |

## Local-only engineering artefacts

Native TIA Portal workspaces, automatic backup directories, compiled helper
binaries, runtime evidence, temporary files, screenshots, and context archives
remain outside normal Git history. They are not deleted. Important native
snapshots are identified by size, timestamp, and SHA-256 in
`tia-project-versioning/manifests/` so that a reviewed external backup can be
matched to the repository revision that produced it.

The current local Siemens workspaces are intentionally ignored:

- `tia v19 programming hmi and plc/`;
- `tia v19 programming hmi and plc.backup/`;
- argument-derived recovery paths beginning with `--`.

Ignoring a native project does not make it disposable. Git protects the
reviewable source and manifest; the native project still requires an external
backup and restore test.

## Commit boundaries

Use small commits that can be validated and reverted independently:

1. repository governance and agent architecture;
2. PLC/process source and its matching I/O documentation;
3. HMI screens, tags, and alarms;
4. TIA/PROFINET engineering tools;
5. safety analysis and approved interfaces;
6. reviewed deliverables and evidence manifests.

Each commit must include a new append-only change-log entry and the validation
appropriate to its files. Do not mix a native project snapshot, generated
binary, source-code change, and unrelated documentation merely because they
were produced in the same chat.

## Branch policy

`codex/revised-open-points` is the current integration branch. Older branches
remain historical until their unique commits are reviewed. After the dirty
working tree is fully classified, tested, and committed, select one stable
default branch in GitHub; do not change the remote default while unversioned
engineering work remains.
