# REV12 SET03 Engineering Change — Settings and Cable Integration

Date: 2026-08-09  
Target: TIA Portal V19, S7-1512C-1 PN, MTP1500 Unified Comfort

## Implemented offline

- Added read-only Settings pages for digital inputs, digital outputs and external-wash diagnostics.
- Added an authorised Timers & Delays page. PLC-side LIMIT validation remains authoritative for every editable value.
- Added PLC-latched Filler and Capper lift confirmations. A lift can run only while the selected lift is confirmed, movement area is clear, Safety OK is true, the machine is stopped and no critical alarm is active.
- Confirmation is cancelled by reset, safety loss, movement, critical alarm, selection change or leaving the lift workflow.
- Added a Production process mimic with tank level, product presence, vacuum, M102, M103 and valves EV210/217/213/212/247. Equipment selection opens read-only diagnostics and never writes a hardware command.
- Removed superseded Production safety-layer objects and corrected mimic geometry. The final full-screen audit passed 25,185 layout checks with zero failures.
- Added engineering device/cable codes to the HMI tag comments without assigning unconfirmed physical addresses.

## PLC-enforced timer limits

| Parameter | Allowed range | Nominal basis |
|---|---:|---:|
| CIP tank-full delay | 1–10 s | 4 s |
| CIP pre-open delay | 0–5 s | 1 s |
| Valve 212 open time | 1–20 s | 7 s |
| CIP vacuum post-run | 1–30 s | 10 s |
| CIP media-loss timeout | 1–10 s | 3–4 s |
| Production vacuum startup inhibit | 1–15 s | 5–6 s |
| Run Out final clearing time | 5–60 s | 20 s |
| External wash OFF delay | 0–10 s | 2–3 s |

## Mandatory hold points

The following remain deliberately `TBC` and were not invented:

- IFM AL1403 port allocation.
- TLS100 interface selection (IO-Link versus 4–20 mA), scaling and final address.
- Cable lengths, routing and final conductor sizing.
- Safety door/E-stop pinouts and Pilz validation.
- Motor/VFD cable sizing for motors without confirmed nameplate and installation data.
- SMC valve-island bit/solenoid allocation, including EV210/217/213/212/247 and the external wash valve.
- The cable schedule lists DS100–111 as 12 devices, while the confirmed machine description states 11 doors. This discrepancy must be resolved before drawings or procurement are released.

## Validation boundary

This change was performed offline. It does not authorise PLC/HMI download, online connection, force, safety validation or physical commissioning. PLCSIM/FAT and point-to-point electrical checks remain required.
