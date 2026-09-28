# Task Brief

## Target

- Profile: `<profiles/example.yaml>`
- Approved project/workspace: `<relative disposable path>`
- Protected sources: `<paths that must remain unchanged>`
- Engineering platform/version: `<verify against profile>`
- Programming language: `<ST | Ladder | SFC>`

## Authority and capabilities

- Task class: `<repository-only | offline engineering UI | external read | hardware control>`
- Requested capabilities: `<exact capability names>`
- Prohibited capabilities: `<exact capability names>`
- Current operator confirmation required: `<yes | no; do not paste secrets>`
- Exact authorization required: `<target and operation, or none>`

## Requested change

Describe the required machine behaviour in plain language. Include operating
states, trigger conditions, transition conditions, expected outputs, timeouts,
reset behaviour, and alarms.

## Available signals

| Name | Direction | Address / Label | Meaning |
| --- | --- | --- | --- |
| | Input | | |
| | Output | | |

Unknown signals, meanings, units, addresses, feedback, interlocks, and target
identity must be recorded as `MISSING` or `AMBIGUOUS`; they must not be invented.

## Deliverables

- [ ] PLC logic / configuration change
- [ ] Labels and comments
- [ ] Compile result
- [ ] Run report

## Required evidence

- [ ] Applicable task/profile/policy snapshot
- [ ] Before/after protected-source integrity
- [ ] Validation or compile output
- [ ] Material/critical UI captures and manifests, if UI work is authorized
- [ ] Accessibility/action-batch and screenshot counts
- [ ] Changed-artifact inventory

## Acceptance criteria

List observable outcomes that make this task pass. Example: "The automatic
sequence advances only after `ExtendedSensor` is true and faults after three
seconds without that signal."

## Stop conditions

List ambiguity, target mismatch, missing evidence, unsupported capability, or
unexpected state that must stop the task without a success claim.
