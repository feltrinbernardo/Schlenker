# Task Brief: Schlenker Ladder Documentation in the Development Environment

This brief continues `schlenker-commenting.md` but replaces its offline-only
safety gate with the active development-environment policy in `AGENTS.md` and
the connection rules in `profiles/schlenker-gxworks2-development.yaml`.

All documentation, accuracy, presentation, procedure, deliverable, and
logic-preservation requirements from `schlenker-commenting.md` remain in force,
with these explicit changes:

1. A current operator confirmation must establish that every PLC reachable from
   this engineering host is a development/test target and production equipment
   is not accessible.
2. An Online menu or connection-related UI is not by itself a stop condition.
   It may be dismissed or inspected without selecting an operation.
3. Online observation, Monitor, connected diagnostics, or Read from PLC may be
   used only if required by the task and only after the visible target identity
   is verified as a confirmed development PLC.
4. Write to PLC, Transfer, Remote Operation, PLC run/stop/reset, connection
   changes, and device-memory writes are outside this commenting task. They
   require a separate operator authorization naming the target and operation.
5. The protected original `D:\GX Works\schlenker.gxw` remains immutable. Only
   `fixtures/projects/schlenker-working/schlenker.gxw` may be edited.
6. The baseline compiler result must still be established before comment entry.
   The full 614-step program must still be reviewed, comments must remain
   evidence-based, and executable logic must remain unchanged.
7. If target identity is ambiguous, a path or CPU differs, production routing
   is possible, the baseline cannot be proven, or logic-preserving editing is
   uncertain, stop and report without improvising.

For this task, development PLC access is permission to use a verified target
when genuinely necessary. It is not a requirement to connect, and the preferred
commenting workflow remains project-file based.

