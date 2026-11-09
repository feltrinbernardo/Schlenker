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

On 2026-11-10, TIA Portal V19 capture and coordinate input succeeded during read-only discovery of 12 workshop devices. See [network discovery record](TIA_NETWORK_DISCOVERY_2026-11-10.md) for observed addresses and limitations. This establishes the tested TIA capture/navigation path, not full engineering validation or reboot persistence.
