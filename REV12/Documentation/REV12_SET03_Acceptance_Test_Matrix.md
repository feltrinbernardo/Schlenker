# REV12 SET03 Acceptance Test Matrix

| ID | Test | Result | Evidence / remaining action |
|---|---|---|---|
| SET03-PLC-01 | PLC software rebuild | PASS | 0 errors, 0 warnings; `REV12_SET03_PLC_Import_Compile_Report.txt` |
| SET03-HMI-01 | HMI screens/tags compile | PASS | Project saved only after HMI compiler returned zero errors |
| SET03-HMI-02 | Navigation, commands and symbolic fields | PASS | Full Openness audit: 144 navigation, 33 command and 218 field checks |
| SET03-HMI-03 | No overlap or out-of-bound objects | PASS | 25,185 layout checks, zero failures; includes Production page |
| SET03-HMI-04 | No legacy Trends wording | PASS | 530 wording checks, zero failures |
| SET03-LIFT-01 | Filler lift confirmation gating | PASS (static/offline) | PLC logic and HMI workflow implemented; dynamic FAT pending |
| SET03-LIFT-02 | Capper lift confirmation gating | PASS (static/offline) | PLC logic and HMI workflow implemented; dynamic FAT pending |
| SET03-TMR-01 | Timer entry limits enforced in PLC | PASS (static/offline) | LIMIT validation added in FB_Application; boundary FAT pending |
| SET03-IO-01 | Digital I/O Settings pages are read-only | PASS | HMI implementation and audit |
| SET03-MIMIC-01 | Production mimic diagnostics only | PASS | Selectable equipment navigates to diagnostics; no command write |
| SET03-CABLE-01 | Cable/device schedule captured | PASS WITH HOLD POINTS | Unconfirmed interfaces, ports, sizes and lengths remain TBC |
| SET03-FAT-01 | PLCSIM sequence and negative testing | PENDING | Must be executed before download |
| SET03-SITE-01 | Physical I/O, safety and cable validation | PENDING | Requires electrical/safety commissioning authority |

## Required FAT scenarios

1. Attempt each lift without confirmation, with machine moving, with Safety not OK and with a critical alarm; movement must remain inhibited.
2. Confirm only one lift at a time; change selection and leave the workflow; confirmation must clear.
3. Enter values below and above every timer range; PLC values must clamp to the documented limits.
4. Select every Production mimic device; diagnostics navigation must work and no output command may change.
5. Verify all retained production, CIP, run-out, door-access and alarm behavior after the cumulative change.
