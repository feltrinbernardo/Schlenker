## Summary

Describe the problem and the resulting repository change.

## Validation

- [ ] `scripts/test-repository.ps1` passes.
- [ ] The request has a new append-only entry under `logs/change-log/`.
- [ ] Native GX Works/TIA projects, generated archives, screenshots, logs, and
      compiled helper binaries are not included.
- [ ] Any PLC/HMI compile or test evidence is summarized and stored only in the
      approved evidence location.
- [ ] The protected source project was not changed.
- [ ] No production credentials, connection details, or other secrets are in
      the diff.

## Industrial safety impact

- [ ] Offline/repository-only change; no PLC communication occurred.
- [ ] Development PLC interaction occurred and the run-specific confirmation,
      target identity, authorization, and evidence are recorded.
- [ ] Not applicable.

Only select the statement that accurately describes this change.
