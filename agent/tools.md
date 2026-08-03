# Desktop-Control Contract

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
