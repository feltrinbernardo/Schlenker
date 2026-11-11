# 01 — Production logic
Status: functional design, onsite verification pending.

## Startup and operation
- Safe enable and machine homing use filler valve #1 zero reference.
- Normal production: 125Y1 OPEN; 125Y2 OPEN; 125Y3 CLOSED; 125Y4 CLOSED; 125Y5 CLOSED; 125Y6 OPEN when product feeding; 125Y7 CLOSED. Verify actual pneumatic states.
- Tank level target nominal 50%, band 40–60%. “Delta P” in operator discussion means level percentage, NOT differential pressure.
- M102 product pump modulates continuously while bottles are produced; avoid repetitive ON/OFF that creates foam. Confirm VFD setpoint scaling.
- Product inlet 125Y6 OPEN, wait 2 seconds, then start M102 with liquid presence proven; on stopping M102 wait 3 seconds before closing Y6. No dry running.
- During bottle absence, level may rise to 54%, then stop M102 and close Y6 after 3 seconds. Resume feed according to actual level and demand; do not force 45% threshold blindly.
- M103 vacuum pump in production; standby delay-off approximately 3–4 minutes subject to process confirmation.

## Bottle flow
- Two upstream and two downstream accumulation sensors.
- Infeed far absent: reduce speed. Infeed near absent: close inlet gate at minimum speed.
- Every normal gate OPEN/CLOSE occurs at minimum speed; emergency stops are immediate.
- Outfeed far accumulation: slow to minimum, close gate, return to nominal, clear tracked bottles, then exactly THREE additional FULL EMPTY filler revolutions; decelerate and stop in Auto standby, blinking green Auto Start; automatically restart when accumulation clears and safety permits. Restart at minimum speed for gate opening, then nominal.
- Outfeed near accumulation: protective controlled stop without waiting for complete emptying; define safe stop category with controls engineer.
- Fault/emergency/safety trips NEVER auto-restart.

## Runout Production — end of product
- No incoming product: M102 stops on lack of product. Low tank level alarm at 45% during ordinary production.
- Operator selects Runout Production to continue processing remaining tank content, with M102 OFF and production tank-level permissives excluded. Other safety/mechanical protections remain active.
- Operator requests infeed gate close; gate action occurs at minimum speed, then machine can run at nominal speed while remaining bottles exit.
- Operator stops production manually, then selects CIP in HMI Functions.
- Do not confuse this operator runout with automatic outfeed accumulation runout.

## Manual carousel and Omron pendant — operator specification 2026-11-11

This specification supersedes previous manual carousel speed assumptions. It records the operator's requested behavior; it is not evidence of native TIA implementation, safety validation or physical commissioning.

- With the selector in MANUAL, enable the Omron pendant subject to the required operating and safety permissives.
- The central three-position enabling switch in its intermediate position requests carousel VSD RUN, subject to safety permissives. Release or full squeeze revokes RUN; stopping must follow the separately defined safety function.
- Requested manual speed: **3,000 bottles/hour**. With **36 filling valves**, the nominal corresponding carousel speed is `3000 / (36 * 60) = 1.388888... RPM`, rounded to **1.389 RPM**. This is a kinematic conversion, not a measured speed or a safety-approved limit.
- Left button, contacts **7–8: UP**. Right button, contacts **5–6: DOWN**. These labels do not authorize interpreting the buttons as carousel speed increase/decrease or changing their existing equipment assignment without verification.
- In AUTOMATIC, disable all pendant commands; the carousel VSD follows automatic production logic. Pendant commands must not remain latched across the mode transition.
- RUN is an operational command, not a safety stop. Keep the PILZ safety path separate; do not bypass its permissives or change its stop behavior to implement the operational request.

### Implementation and verification still required

- Verify that 3,000 bottles/hour (1.389 carousel RPM) is permitted for protected manual operation against the machine's safety design. Operator confirmation of the requested speed does not establish this safety acceptance.
- Verify the complete mechanical transmission ratio, motor/VSD data and setpoint scaling before assigning any frequency in Hz or percentage reference. No new Hz or percent value is authorized by the RPM calculation alone.
- Verify how the enabling switch's valid intermediate position reaches the operational PLC logic while preserving the independent PILZ safety function; no new I/O address is assigned here.
- Repository source review: `REV12/PLC_Sources/07_FB_JogPendant.scl` gates `JogForwardCmd` with manual mode, safety, pendant connection, `JogPB`, height-mode exclusion and drive readiness. `REV12/PLC_Sources/11_FB_Application.scl` supplies a percentage reference limited using `MainJogMaxFrequencyHz / MainDriveMaxFrequencyHz`. This existing calculation is not proof of a 3,000 bottles/hour setting and must be reconciled with verified mechanical scaling before implementation.
- Confirm native TIA correspondence, UP/DOWN gating, release/full-squeeze behavior and mode transitions during subsequent validation. This document update makes no PLC/HMI/PILZ changes and performs no download or output activation.
