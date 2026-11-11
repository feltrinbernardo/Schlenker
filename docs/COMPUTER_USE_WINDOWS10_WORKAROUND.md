# Computer Use: Windows 10 capture workaround

Adopted by the operator on 2026-11-10 for this workstation. This is an unofficial, unsigned, exact-binary workaround, not an OpenAI release or engineering/FAT approval. Continue using the official Computer Use wrapper; no alternate capture API or custom helper client is introduced.

## Installation identity

- SIEMENS SIMATIC Field PG M5, Windows 10 Pro build 19045, Intel HD Graphics 530.
- Installed package: @oai/sky 0.7.17.
- Original signed helper SHA256: `7F73CF564E1F7FF1CC6DFF117E2BAA6743BE2CB5423BDC0F50274FA0E9A4002C`.
- Adopted unsigned helper SHA256: `DDC00C9D93D76D058E946DC032E776F262229F9BF3AC3FBCCB92F31CBF0DD9C7`.
- Local recovery directory: `%LOCALAPPDATA%/CodexCaptureRepair/2026-11-10`.
- Backup: `codex-computer-use.original.exe`; reviewed sources/tests: `review` and `candidate-review`; guarded installation/rollback: `switch-helper.ps1`.
- Active helper is beneath `%LOCALAPPDATA%/OpenAI/Codex/runtimes/cua_node/db1cc9e06a6c48d6/bin/node_modules/@oai/sky/bin/windows/`.

The community source archive SHA256 is `27AFCC49FA225C63ACA68F98D38FCF4016C1CD7E6B7B3CD33EB7D2E51F090BE5`. Provenance: https://github.com/openai/codex/issues/35446 and https://github.com/user-attachments/files/34519757/capture-patch-source-review.zip . Its original hash is different. The local builder was adapted only after PE-region, import, relocation and callback-disassembly review; the exact input hash and checksum guards were updated, not removed. Do not apply this profile to other binaries, even if the version string matches.

## What changed and what passed

The FrameArrived callback delegates work to a retained MTA worker so the callback can return without waiting for bitmap conversion. The candidate adds unwind metadata and handles worker-creation/WinRT-initialization failure. It removes the original signature overlay rather than presenting the changed binary as signed. No Windows security/privacy setting or app-access policy was disabled.

Four static and five synthetic runtime tests passed. Through the official wrapper, four Notepad capture requests succeeded, including a menu and a capture after kernel reconnection. Observed-coordinate clicks selected the editor and opened File; typed test text was visible in the next image. This supersedes the earlier Notepad capture-blocker reports.

TIA compatibility, reboot persistence, long-duration operation and cancellation/late-frame behavior were not established by the Notepad test. Record subsequent TIA evidence separately. Updates may replace the workaround.

## Recovery and future sessions

1. Verify the active helper hash before relying on this repair. An unknown hash requires new review; never force the old patch onto an updated helper.
2. Preserve the signed original and local reviewed sources. Do not commit helper executables, backups, downloaded archives or private runtime data.
3. To restore the original, run the local `switch-helper.ps1 -Restore` using an ordinary shell tool. It validates known hashes, stops only the exact helper path and atomically replaces the file. Then reconnect Computer Use. Rollback tooling exists but rollback of the working installation has not been exercised.
4. Use a fresh harmless window to verify capture and coordinate input after a runtime/app update or recovery. Do not repeat unchanged failing tests indefinitely.
5. For TIA, retain all root AGENTS.md rules: current isolated-test-network confirmation, unique observed target, fresh observations and explicit operation-specific authority for writes. This workaround grants no additional device permissions.

Detailed local evidence: `runs/2026-11-10-computer-use-runtime-recovery/report.md` (ignored). Git preserves this procedure and hashes, not the local binary.

## Subsequent TIA verification

### Windows path arguments in the JavaScript wrapper

On 2026-11-10 a later restart failed with `ShellExecuteW returned 2` even
though the TIA executable existed. A path typed as a JavaScript string with
single backslashes lost separators; a dated path also raised an octal-escape
parse error. Construct paths without ambiguous literal escapes, for example:

```javascript
const tiaPath = 'C:/Program Files/SIEMENS/Automation/Portal V19/Bin/Siemens.Automation.Portal.exe'
  .replaceAll('/', String.fromCharCode(92));
```

The corrected argument launched TIA, and the approved recovery copy and new
SMC source were subsequently opened/imported through the official GUI wrapper.
This identifies a caller-side path error; it does not prove that every earlier
stale-input or capture symptom had the same cause. Verify the exact visible
file path before confirming a file dialog. No helper binary change was needed
for this correction.

On 2026-11-10, TIA Portal V19 capture and coordinate input succeeded during read-only discovery of 12 workshop devices. See [network discovery record](TIA_NETWORK_DISCOVERY_2026-11-10.md) for observed addresses and limitations. This establishes the tested TIA capture/navigation path, not full engineering validation or reboot persistence.


## 2026-11-11 recovery after runtime replacement

The observed failures were `coordinate input geometry is unavailable` and
`window capture timed out: timed out waiting on channel`. The running helper
was in runtime `76f5b5935a63c7ba`; the previously repaired runtime directory
`db1cc9e06a6c48d6` no longer existed. Both packages reported Sky 0.7.17, but the
new helper had a different full-file hash. Version equality alone was not used
to authorize the old patch.

### Exact profile and preserved recovery files

- Signed input SHA256: `0EBB47BFAAF729E22BCDB08DCFCE1419EEFEDA80AFAEFED3EDBC088C24A2E778`.
- Installed unsigned candidate SHA256: `401A93581328998C679E570A81E0CDB4C9132E275CAF4050D8FAFAB8E7B7B18E`.
- Target: `%LOCALAPPDATA%/OpenAI/Codex/runtimes/cua_node/76f5b5935a63c7ba/bin/node_modules/@oai/sky/bin/windows/codex-computer-use.exe`.
- Local recovery directory: `%LOCALAPPDATA%/CodexCaptureRepair/2026-11-11`.
- Preserved signed input: `codex-computer-use.original.exe`; installation also preserved `replaced-original.exe`.
- Sources and tests: `candidate-review`; manifests: `review-evidence.json` and `candidate-review/candidate-report.json`.
- Guarded installer/rollback: `switch-helper.ps1` in this new recovery directory. Default selects the candidate; `-Restore` selects the signed input. This script was prepared and syntax-checked but not executed during recovery; installation used separately reviewed guarded atomic replacement. Rollback has not been exercised.

Read-only comparison established identical bytes in all nine PE sections,
including executable code, imports, relocations and unwind data. Before the
certificate overlay, only five bytes differed from the 2026-11-10 signed input:
three PE timestamp bytes and two checksum bytes. The new input's OpenAI
Authenticode signature was valid. The separate new builder changed only its
exact expected input hash and expected checksum (`32C81700` to `64AF1700`);
all remaining byte-region guards and generated worker code were preserved.
The earlier originals, candidate and tools were left unchanged.

### Validation and observed recovery

Four static tests and five synthetic worker tests passed. These cover exact
input rejection, declared change boundaries, PE/unwind integrity,
repeatability, worker separation/concurrency and controlled failure paths.
They do not alone establish real capture behavior or general COM lifetime
correctness.

The primary agent verified the signed backup and candidate hashes, stopped
only the process matching the exact helper path, and atomically replaced that
helper. After resetting the official Node REPL connection, actual Notepad
capture and an observed-coordinate File-menu action passed. Subsequent TIA
Portal V19 capture and navigation passed, followed by a read-only connection
to `PLC_1` and tag-table monitoring. These observations are recorded from the
primary agent's execution report; they are not synthetic test results.

No PLC/HMI download, forced value, output activation or security-setting
change was part of this recovery. Successful UI recovery does not establish
that the pendant signals have been verified: input observations and physical
transitions must be reported separately. Reboot persistence, long-duration
capture and cancellation/late-frame behavior remain unverified. Future runtime
updates require a new exact-hash review rather than reuse of this profile.
