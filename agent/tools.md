# Desktop-Control Contract

## Active Windows integration

Use the official `computer-use@openai-bundled` plugin and its `computer-use`
skill for app discovery and all GX Works2 input. Do not operate GX Works2 with
PowerShell UI Automation while Computer Use is available.

The allowed GX Works2 application identifier is:

`{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}\MELSOFT\GPPW2\GD2.EXE`

App and window objects must come directly from Computer Use discovery. Require
one unique GX Works2 window, observe before each action, and refresh immediately
after each action.

On Windows 10 build 19045, use `scripts/capture-gxworks2-window.ps1` as a
separate, read-only observation channel because the official screenshot call is
unavailable. The helper is not a desktop protocol client: it cannot activate a
window or send any input, it accepts only the exact foreground `GD2.exe`, it
rejects unsafe project titles, and it writes only beneath `runs/`.

External PNGs may support visual reasoning and evidence collection. They do not
produce an official Computer Use screenshot ID and must not be used to justify
blind coordinate clicks. Use accessibility and GX Works2 keyboard workflows for
actions.

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
- action summary;
- compilation output;
- final report.

The first version does not need multi-agent coordination, external services, or
online PLC communication.
