# TIA Portal V19 Offline Project Creator

`CreateOfflineProject.cs` is a local Siemens TIA Portal Openness helper. It
creates and saves a new `.ap19` project only; it contains no online, download,
CPU control or device-memory operations.

## Current prerequisite

The Windows account running this helper must be a member of the local
`Siemens TIA Openness` group. An administrator must make that change outside
Codex, then the user must sign out and back in (or restart Windows) so the new
group token is active.

After that prerequisite is satisfied, rebuild the helper against:

`C:\Program Files\Siemens\Automation\Portal V19\PublicAPI\V19\Siemens.Engineering.dll`

The target project for this package is:

`fixtures/references/tia-import-rev10/rev11-working/tia-v19/Schenker_REV11_V19.ap19`

The helper refuses to run if any `.ap19` project already exists below its
target directory, preventing accidental overwrite.

## Page-batch failure handling

The two `scripts/run-global-common-*-page-transactions.ps1` runners stop at the
first failed or incomplete build/audit and return exit code 1. Success returns 0
and `RUN_STATUS=PASS`. They require an empty output directory: an old ledger is
not proof of current native state. Preserve previous logs and reconcile the
project before selecting remaining pages for a new run. Do not blindly rerun
partially applied operations. Duplicate screen names are rejected.

The regular runner expects the `Rev25FrameAudit` argument order
`project, report, screen`; the render runner expects `AuditCommonRenderTarget`
with `project, screen, report`. Custom auditor executables must follow those
contracts. Both require exactly one audit row matching the requested screen.

The builder and these two auditors emit flushed `PHASE=...|UTC=...` markers.
Caught failures include `FAILURE_CODE` and the last phase while retaining the
original exception. A started phase without completion is not success. This
change adds no automatic retries, watchdog termination, or permission bypass.
A helper that remains blocked still requires state reconciliation; do not start
another helper against that project. Compile/save behavior is unchanged.

Offline regression commands (no Siemens assemblies or TIA required):

```text
PWSH=/path/to/pwsh python3 scripts/test-batch-transactions.py
pwsh -NoProfile -File scripts/test-openness-diagnostics.ps1
```

The Python subprocess harness uses POSIX executable fixtures. The PowerShell
diagnostic test extracts the actual diagnostic methods and compiles them with
fake types; it does not compile the complete helpers against Siemens V19.
Windows PowerShell 5.1 and native TIA behavior remain bench-validation items.

## Exclusive access for the corrected batch paths

Both page runners now acquire the same atomic per-project lease before creating
run evidence or launching helpers. On Windows the marker is under
`%LOCALAPPDATA%/Schlenker/ProjectLeases/`; POSIX offline tests use the OS temp
directory. Project identity uses a normalized full path (case-insensitive on
Windows). Different checkout directories do not create separate leases for the
same project path. The project itself is never opened by the lease code.

There is no lease expiry, waiting loop or automatic takeover. Normal return and
returned helper errors release the lease in `finally`. Abrupt parent termination
leaves a marker; interruption while a child might still be executing also keeps
it. Before manually removing a stale marker, verify the owning process and any
child helper have stopped and reconcile TIA state. PID alone is not sufficient
proof because it can be reused. Do not delete a live lease.

`BuildUnifiedHomeScreen`, `Rev25FrameAudit`, `AuditCommonRenderTarget`, and the
builder's `VerifyNavigationMaster` delegation use `ExclusiveAccess` around their
attached native work and check project identity after acquisition. Existing
source build commands need no extra C# dependency, but the executables must be
rebuilt before these protections exist on the bench. The builder/navigation
verifier now require an absolute project path; filename hints are rejected.
The page runner passes the full path accordingly.

The local lease covers participating batches under the same Windows user; it
is not a security boundary or protection for alternate junction/UNC aliases.
Native ExclusiveAccess covers the guarded helper interval in the TIA process,
not all Computer Use sessions or unmodified helpers. Between helper calls,
GUI ownership must remain with the batch's specialist. Do not treat API access
as permission to operate equipment. No global coordinator/desktop lock or
background service was added.

Additional offline tests:

```text
PWSH=/path/to/pwsh python3 scripts/test-project-exclusivity.py
pwsh -NoProfile -File scripts/test-openness-exclusivity.ps1
```

The process tests exercise contention, release and crash markers without any
project file. The native-scope tests compile extracted `using` statements with
fake API types; actual V19 locking, GUI interaction and cancellation behavior
require a separately authorized Windows bench test.


## Optional SMC readability/state-background session

`RefineSmcReadability` accepts an optional fourth argument `--with-state-backs`.
It applies the existing readability stage followed by the existing
`MatchSmcStateBacks` stage in one attached session and one native ExclusiveAccess
scope. On success it compiles the HMI once and saves once, instead of the two
attachments, compilations and saves from running both commands separately.
No timing improvement has been measured against TIA.

Both stages retain their original guards and mutations: 67 tag mappings,
11 command bindings, lifecycle markers, object types and expected dynamics.
The second stage re-enumerates the screen after the first stage creates the
Y6/Y7/Y10 backgrounds. It does not depend on a saved/reopened project by design;
this in-memory behavior still requires Windows/V19 bench validation. The final
labels and appearance are those of the existing MatchSmcStateBacks stage.
No other historical SMC migrations are included.

The exact approved project path and complete-backup validation remain required.
The backup is checked once, under exclusive access, before either stage mutates
the project. This command is for the original pre-readability baseline: it
rejects already-created backgrounds and must not be replayed on a migrated
project. Do not bypass guards to apply it to the current bench project.

The original three-argument RefineSmcReadability command still runs only the
readability stage. MatchSmcStateBacks retains its three-argument standalone
command. **Rebuild is required:** when compiling RefineSmcReadability with the
existing Siemens V19 references, include both `RefineSmcReadability.cs` and
`MatchSmcStateBacks.cs`, and select
`/main:Schlenker.TiaV19.RefineSmcReadability` because both sources have Main.
MatchSmcStateBacks can still be built from its own source alone. No executable
was rebuilt against Siemens as part of this change.

This is sequential batching, not an atomic transaction. A stage or compilation
failure stops without saving, but partial changes can remain in memory. A save
failure has uncertain persistence. Reports distinguish phase start/completion;
reconcile actual project state before retry or recovery. There is no automatic
retry, rollback, or save on failure. Cancellation is checked between stages,
before compilation and before saving. GUI ownership remains with the specialist
for the whole operation; native access does not replace coordinator ownership.
Required visual/integration verification after a successful save is unchanged.

Offline orchestration test:

```text
pwsh -NoProfile -File scripts/test-smc-session.ps1
```

The test executes the actual session method with simulated attach, stages,
compiler and save. It checks grouped and standalone execution, operation counts,
ordering, resource release, cancellation and failure paths. It does not execute
the visual mutations, open a native project, compile TIA, or validate Siemens
assemblies. The existing `node scripts/test-smc-logical-hmi.cjs` checks the
logical HMI scripts separately.
