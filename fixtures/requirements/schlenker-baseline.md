# Task Brief: Schlenker Offline Baseline

## Target

- Profile: `profiles/schlenker-gxworks2.yaml`
- Original project: `D:\GX Works\schlenker.gxw`
- Working copy: `fixtures/projects/schlenker-working/schlenker.gxw`
- GX Works version: GX Works2 1.560J
- Programming language: Ladder, observed in the open `MAIN` editor

## Objective

Create a safe, repeatable baseline of the existing Schlenker project without
changing the original project and without communicating with physical equipment.

## Required safety gate

Do not operate the GX Works2 user interface until a person has confirmed that:

1. monitoring is stopped;
2. GX Works2 is disconnected from every PLC communication path;
3. closing the current project will not interrupt an active engineering task;
4. the original project can be copied to the disposable working-copy path.

## Baseline procedure

1. Run `scripts/prepare-schlenker-fixture.ps1 -ConfirmDisconnected` only after a
   person has completed the safety gate. The script records the original
   project's size, modification time, and SHA-256 hash.
2. Confirm the script created the verified working copy and local fixture
   manifest.
3. Open only the working copy in GX Works2.
4. Verify and record the PLC family, CPU, project type, and program languages.
5. Build the unmodified working copy.
6. Save compiler output and screenshots under a new `runs/<run-id>/` directory.
7. Close the working copy without writing to the original.
8. Confirm the original project's hash and modification time are unchanged.

## Deliverables

- [ ] Disposable working copy
- [ ] Verified PLC family and CPU in the active profile
- [ ] Baseline compile result
- [ ] Evidence and completed run report
- [ ] Confirmation that the original file remained unchanged

## Acceptance criteria

- No PLC connection, write, download, or online operation occurred.
- Only the disposable copy was opened for baseline work.
- The CPU and project type were verified from GX Works2 rather than inferred.
- The baseline compile outcome and all compiler messages were recorded.
- The original file's hash and modification time match the pre-run values.
