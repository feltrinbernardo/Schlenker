# Agent Capability Matrix

## Status vocabulary

Every capability must use exactly one of these values:

- **IMPLEMENTED** — code or a controlled repository process exists, and evidence
  confirms it within the stated scope.
- **SIMULATED** — available only through a fake, fixture, replay, or test double;
  it cannot affect a real external target.
- **DISABLED** — intentionally unavailable or unbound, even if a future contract
  or underlying product capability exists.
- **PLANNED** — accepted into the roadmap but not yet available through the
  executable agent runtime.

The status describes the Schlenker agent runtime or its controlled repository
process, not what a vendor engineering product could theoretically do.

## Foundation capabilities

| Capability | Status | Current evidence or boundary |
|---|---|---|
| Written GX Works2 safety policy | IMPLEMENTED | Root `AGENTS.md`; will move to a narrower scope after Gate 0.2 |
| Mandatory append-only change log | IMPLEMENTED | `scripts/write-change-log.ps1` and repository hook policy |
| GX protected-source profile | IMPLEMENTED | `profiles/schlenker-gxworks2.yaml` |
| GX development profile | IMPLEMENTED | `profiles/schlenker-gxworks2-development.yaml`; stored confirmation is historical only |
| Disposable GX project fixture | IMPLEMENTED | Ignored fixture exists and matches the protected source hash at Phase 0 start |
| Hybrid GX window capture helper | IMPLEMENTED | Evidence-only helper governed by current root policy |
| Kiro requirements/design/tasks | IMPLEMENTED | `specs/agent-foundation/` with validated traceability |
| Executable Python agent package | PLANNED | Task 1.1; `pyproject.toml` is not present |
| Typed schemas and generated JSON Schema | PLANNED | Phase 2 |
| Deterministic run state machine | PLANNED | Phase 3 |
| Fail-closed policy engine | PLANNED | Phase 4 |
| Typed tool registry and executor | PLANNED | Phase 5 |
| Repository/evidence adapters | PLANNED | Phase 6 |
| Agent CLI | PLANNED | Phase 7 |
| Deterministic fake model adapter | SIMULATED | Design accepted; implementation is Task 8.2 |
| Real model provider adapter | PLANNED | Gate 8.5 |
| Advisory LLM-as-judge | PLANNED | Existing judge work requires separate ownership/review before runtime integration |

## Platform capability matrix

| Platform | Capability | Risk class | Status | Release boundary |
|---|---|---|---|---|
| Repository | Read status, revision, metadata, and hashes | READ_ONLY | PLANNED | First vertical slice |
| Repository | Allowlisted offline validation command | WORKSPACE_WRITE | PLANNED | First vertical slice; no model-generated shell |
| Repository | Atomic run/evidence writes | WORKSPACE_WRITE | PLANNED | First vertical slice; run directory only |
| GX Works2 | Environment and profile diagnostics | READ_ONLY | PLANNED | Phase 12; no application launch in `doctor` |
| GX Works2 | Fixture/protected-source verification | READ_ONLY | PLANNED | Phase 12 |
| GX Works2 | Offline project open and compile | EXTERNAL_READ | PLANNED | Separate reviewed UI run with current confirmation |
| GX Works2 | Hybrid screenshot evidence | EXTERNAL_READ | IMPLEMENTED | Evidence only; never an input/click channel |
| GX Works2 | Online, Monitor, or Read from PLC | EXTERNAL_READ | DISABLED | Not part of release 1 |
| GX Works2 | Write, Transfer, Remote Operation, PLC state, or device memory | HARDWARE_CONTROL | DISABLED | Requires a separate future spec and exact current authorization |
| TIA V19/WinCC | Runtime platform profile | READ_ONLY | PLANNED | Gate 13.1; profile currently absent |
| TIA V19/WinCC | Openness helper inventory/contracts | READ_ONLY | PLANNED | Existing untracked helpers are not yet runtime capabilities |
| TIA V19/WinCC | Offline project/export validation | EXTERNAL_READ | PLANNED | Phase 13 after nested policy approval |
| TIA V19/WinCC | Offline compile/export evidence | EXTERNAL_READ | PLANNED | Gate 13.4 |
| TIA V19/WinCC | Online diagnostics | EXTERNAL_READ | DISABLED | Not part of release 1 |
| TIA V19/WinCC | Download, run-state change, or hardware write | HARDWARE_CONTROL | DISABLED | Requires a separate future spec |

## Status transition rules

- `PLANNED` becomes `SIMULATED` only when a test double or replay is executable
  and deterministic tests pass.
- `PLANNED` or `SIMULATED` becomes `IMPLEMENTED` only when the real adapter or
  process is bound, policy-gated, documented, tested, and evidenced.
- Any capability becomes `DISABLED` immediately when its safety assumptions,
  target identity, evidence, or authorization model is incomplete.
- `DISABLED` never becomes `IMPLEMENTED` through configuration alone. The
  applicable gate, tests, policy review, and change log must be completed.

## First-release rule

The first release exposes only repository-local capabilities needed for the
offline vertical slice. GX Works2 and TIA/WinCC adapters are delivered in later,
independently reviewed increments. All hardware-control executors remain
unbound and disabled.
