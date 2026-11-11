# Desktop-Control Contract

## Active Windows integration

Use the official `computer-use@openai-bundled` plugin and its `computer-use`
skill for app discovery and all GX Works2 input. Do not operate GX Works2 with
PowerShell UI Automation while Computer Use is available.

The allowed GX Works2 application identifier is:

`{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}\MELSOFT\GPPW2\GD2.EXE`

App and window objects must come directly from Computer Use discovery. Require
one unique GX Works2 window before acting.

## Observation and action cadence

Use three distinct channels:

- **Accessibility observation** identifies the application, window, project,
  dialog, focused control, visible text, and enabled state.
- **Visual observation** uses an official screenshot when supported or the
  approved read-only hybrid capture when necessary.
- **Evidence capture** persists an image and manifest for later review.

Routine work may use one accessibility observation before and after a batch of
at most five deterministic, reversible keyboard/accessibility actions in the
same unchanged window and control. Stop the batch on any focus, window, dialog,
project, target, connection, warning, error, or ambiguity change.

Opening/closing a dialog, changing editor, navigating the project tree, saving,
and compiling are material transitions. Refresh after them and capture an image
when the result is materially visual or required as evidence.

Project/target selection, online-mode entry, Read from PLC, transfer, write,
remote operation, run/stop/reset, connection change, and device-memory change
are critical. Perform one action at a time with explicit pre/post observation
and the authorization required by `AGENTS.md`. Never batch across a critical
boundary.

On Windows 10 build 19045, use `scripts/capture-gxworks2-window.ps1` as a
separate, read-only observation channel because the official screenshot call is
unavailable. The helper is not a desktop protocol client: it cannot activate a
window or send any input, it accepts only the exact foreground `GD2.exe`, it
rejects unsafe project titles, and it writes only beneath `runs/`.

External PNGs may support visual reasoning and evidence collection. They do not
produce an official Computer Use screenshot ID and must not be used to justify
blind coordinate clicks. Use accessibility and GX Works2 keyboard workflows for
actions. Routine actions do not each require a PNG when accessibility provides
the needed state. Never reuse coordinates, screenshot IDs, or accessibility
indexes after a material state change.

The agent must have these capabilities in the test environment:

- take a screenshot or inspect the current desktop;
- click, type, use keyboard shortcuts, and scroll;
- wait for an application or dialog to render;
- read visible build/compiler messages;
- save files and capture run artefacts.

Each run records:

- timestamp and selected profile;
- task brief identifier;
- screenshots at material UI transitions;
- accessibility-observation and action-batch counts;
- screenshots submitted to the model versus retained only as evidence;
- action summary;
- compilation output;
- final report.

Lifecycle hooks may add deterministic pre-tool denial, post-tool validation,
and local telemetry. They are guardrails, not the authority for capability or
authorization decisions. The first version does not need multi-agent
coordination or hardware-control executors.
